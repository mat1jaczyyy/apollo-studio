using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Avalonia.Controls;

using Apollo.Binary;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Windows;

namespace Apollo.Selection {
    public static class Operations {
        static bool InsertCopyable(ISelectParent parent, Copyable paste, int right, out Action undo, out Action redo, out Action dispose) {
            undo = redo = dispose = null;

            List<ISelect> pasted;
            try {
                pasted = paste.Contents;
            } catch (InvalidCastException) {
                return false;
            }
            
            Path<ISelect> path = new Path<ISelect>((ISelect)parent);

            undo = () => {
                ISelectParent chain = (ISelectParent)path.Resolve();

                for (int i = paste.Contents.Count - 1; i >= 0; i--)
                    chain.Remove(right + i + 1);
            };
            
            redo = () => {
                ISelectParent chain = (ISelectParent)path.Resolve();

                for (int i = 0; i < paste.Contents.Count; i++)
                    chain.IInsert(right + i + 1, pasted[i].IClone());

                ISelectParent.GetSelection(chain)?.Select(chain.IChildren[right + 1], true);
            };
            
            dispose = () => {
                foreach (ISelect device in pasted) device.Dispose();
                pasted = null;
            };

            for (int i = 0; i < paste.Contents.Count; i++)
                parent.IInsert(right + i + 1, pasted[i].IClone());
            
            ISelectParent.GetSelection(parent)?.Select(parent.IChildren[right + 1], true);
            
            return true;
        }

        static Copyable CreateCopyable(ISelectParent parent, int left, int right) {
            Copyable copy = new Copyable();
            
            for (int i = left; i <= right; i++)
                copy.Contents.Add(parent.IChildren[i]);
            
            return copy;
        }

        static void DeleteRegion(ISelectParent parent, int left, int right, out Action undo, out Action redo, out Action dispose) {
            List<ISelect> u = (from i in Enumerable.Range(left, right - left + 1) select parent.IChildren[i].IClone()).ToList();

            Path<ISelect> path = new Path<ISelect>((ISelect)parent);

            undo = () => {
                ISelectParent chain = (ISelectParent)path.Resolve();

                for (int i = left; i <= right; i++)
                    chain.IInsert(i, u[i - left].IClone());
            };

            redo = () => {
                ISelectParent chain = (ISelectParent)path.Resolve();

                for (int i = right; i >= left; i--)
                    chain.Remove(i);
            };

            dispose = () => {
               foreach (ISelect device in u) device.Dispose();
               u = null;
            };

            for (int i = right; i >= left; i--)
                parent.Remove(i);
        }

        public static void Copy(ISelectParent parent, int left, int right, bool cut = false) {
            CreateCopyable(parent, left, right).StoreToClipboard();

            if (cut) Delete(parent, left, right);
        }

        public static async void Paste(ISelectParent parent, int right) {
            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && InsertCopyable(parent, paste, right, out Action undo, out Action redo, out Action dispose))
                Program.Project.Undo.Add("Device Pasted", undo, redo, dispose);
        }

        public static async void Replace(ISelectParent parent, int left, int right) {
            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && InsertCopyable(parent, paste, right, out Action undo, out Action redo, out Action dispose)) {
                DeleteRegion(parent, left, right, out Action undo2, out Action redo2, out Action dispose2);

                Path<ISelect> path = new Path<ISelect>((ISelect)parent);

                Program.Project.Undo.Add("Device Replaced",
                    undo2 + undo,
                    redo + redo2 + (() => {
                        ISelectParent chain = (ISelectParent)path.Resolve();

                        ISelectParent.GetSelection(chain)?.Select(chain.IChildren[left + paste.Contents.Count - 1], true);
                    }),
                    dispose2 + dispose
                );
                
                ISelectParent.GetSelection(parent)?.Select(parent.IChildren[left + paste.Contents.Count - 1], true);
            }
        }

        public static void Duplicate(ISelectParent parent, int left, int right) {
            Path<ISelect> path = new Path<ISelect>((ISelect)parent);

            Program.Project.Undo.Add($"Device Duplicated", () => {
                ISelectParent chain = (ISelectParent)path.Resolve();

                for (int i = right - left; i >= 0; i--)
                    chain.Remove(right + i + 1);

            }, () => {
                ISelectParent chain = (ISelectParent)path.Resolve();

                for (int i = 0; i <= right - left; i++)
                    chain.IInsert(right + i + 1, chain.IChildren[left + i].IClone());
            
                ISelectParent.GetSelection(chain)?.Select(chain.IChildren[right + 1], true);
            });

            for (int i = 0; i <= right - left; i++)
                parent.IInsert(right + i + 1, parent.IChildren[left + i].IClone());
            
            ISelectParent.GetSelection(parent)?.Select(parent.IChildren[right + 1], true);
        }

        public static void Delete(ISelectParent parent, int left, int right) {
            DeleteRegion(parent, left, right, out Action undo, out Action redo, out Action dispose);
            Program.Project.Undo.Add($"Device Removed", undo, redo, dispose);
        }

        public static void Group(ISelectParent parent, int left, int right) {
            Chain chain = (Chain)parent;

            Chain init = new Chain();

            for (int i = left; i <= right; i++)
                init.Add(chain[i].Clone());

            Path<Chain> path = new Path<Chain>(chain);

            Program.Project.Undo.Add($"Device Grouped", () => {
                Chain chain = path.Resolve();

                chain.Remove(left);

                for (int i = left; i <= right; i++)
                    chain.Insert(i, init[i - left].Clone());
                
                SelectionManager selection = ISelectParent.GetSelection(chain);
                selection?.Select(chain[left]);
                selection?.Select(chain[right], true);

            }, () => {
                Chain chain = path.Resolve();
                
                for (int i = right; i >= left; i--)
                    chain.Remove(i);
                
                chain.Insert(left, new Group(new List<Chain>() {init.Clone()}) {Expanded = 0});
            
            }, () => {
                init.Dispose();
            });
            
            for (int i = right; i >= left; i--)
                chain.Remove(i);

            chain.Insert(left, new Group(new List<Chain>() {init.Clone()}) {Expanded = 0});
        }

        public static void Ungroup(ISelectParent parent, int index) {
            Chain chain = (Chain)parent;

            if (!(chain[index] is Group group) || group.Count != 1) return;

            Group init = (Group)group.Clone();

            Path<Chain> path = new Path<Chain>(chain);

            Program.Project.Undo.Add($"Device Ungrouped", () => {
                Chain chain = path.Resolve();

                for (int i = index + init[0].Count - 1; i >= index; i--)
                    chain.Remove(i);
                
                chain.Insert(index, init.Clone());

            }, () => {
                Chain chain = path.Resolve();
                
                chain.Remove(index);
            
                for (int i = 0; i < init[0].Count; i++)
                    chain.Insert(index + i, init[0][i].Clone());

                ISelectParent.GetSelection(chain)?.Select(chain[index], true);
                
            }, () => {
                init.Dispose();
            });

            chain.Remove(index);
            
            for (int i = 0; i < init[0].Count; i++)
                chain.Insert(index + i, init[0][i].Clone());

            ISelectParent.GetSelection(parent)?.Select(chain[index], true);
        }
        
        public static void Choke(ISelectParent parent, int left, int right) {
            Chain chain = (Chain)parent;

            Chain init = new Chain();

            for (int i = left; i <= right; i++)
                init.Add(chain[i].Clone());

            Path<Chain> path = new Path<Chain>(chain);

            Program.Project.Undo.Add($"Device Choked", () => {
                Chain chain = path.Resolve();

                chain.Remove(left);

                for (int i = left; i <= right; i++)
                    chain.Insert(i, init[i - left].Clone());
                
                SelectionManager selection = ISelectParent.GetSelection(chain);
                selection?.Select(chain[left]);
                selection?.Select(chain[right], true);

            }, () => {
                Chain chain = path.Resolve();
                
                for (int i = right; i >= left; i--)
                    chain.Remove(i);
                
                chain.Insert(left, new Choke(1, init.Clone()));
            
            }, () => {
                init.Dispose();
            });
            
            for (int i = right; i >= left; i--)
                chain.Remove(i);

            chain.Insert(left, new Choke(1, init.Clone()));
        }

        public static void Unchoke(ISelectParent parent, int index) {
            Chain chain = (Chain)parent;

            if (!(chain[index] is Choke choke))return;

            Choke init = (Choke)choke.Clone();

            Path<Chain> path = new Path<Chain>(chain);

            Program.Project.Undo.Add($"Device Unchoked", () => {
                Chain chain = path.Resolve();

                for (int i = index + init.Chain.Count - 1; i >= index; i--)
                    chain.Remove(i);
                
                chain.Insert(index, init.Clone());

            }, () => {
                Chain chain = path.Resolve();
                
                chain.Remove(index);
            
                for (int i = 0; i < init.Chain.Count; i++)
                    chain.Insert(index + i, init.Chain[i].Clone());

                ISelectParent.GetSelection(chain)?.Select(chain[index], true);
                
            }, () => {
                init.Dispose();
            });

            chain.Remove(index);
            
            for (int i = 0; i < init.Chain.Count; i++)
                chain.Insert(index + i, init.Chain[i].Clone());

            ISelectParent.GetSelection(chain)?.Select(chain[index], true);
        }

        public static void Mute(ISelectParent parent, int left, int right) {
            List<IMutable> items = parent.IChildren.Cast<IMutable>().ToList();

            List<bool> u = (from i in Enumerable.Range(left, right - left + 1) select items[i].Enabled).ToList();
            bool r = !items[left].Enabled;

            Path<ISelect> path = new Path<ISelect>((ISelect)parent);

            Program.Project.Undo.Add($"Device Muted", () => {
                List<IMutable> items = ((ISelectParent)path.Resolve()).IChildren.Cast<IMutable>().ToList();

                for (int i = left; i <= right; i++)
                    items[i].Enabled = u[i - left];

            }, () => {
                List<IMutable> items = ((ISelectParent)path.Resolve()).IChildren.Cast<IMutable>().ToList();

                for (int i = left; i <= right; i++)
                    items[i].Enabled = r;
            });

            for (int i = left; i <= right; i++)
                items[i].Enabled = r;
        }

        public static void Rename(ISelectParent parent, int left, int right) {
            // TODO Implement
        }

        public static async void Export(ISelectParent parent, int left, int right) {
            Window sender = ISelectParent.GetWindow(parent);

            SaveFileDialog sfd = new SaveFileDialog() {
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Extensions = new List<string>() {
                            "apdev"
                        },
                        Name = "Apollo Device Preset"
                    }
                },
                Title = "Export Device Preset"
            };
            
            string result = await sfd.ShowAsync(sender);

            if (result != null) {
                string[] file = result.Split(Path.DirectorySeparatorChar);

                if (Directory.Exists(string.Join("/", file.Take(file.Count() - 1)))) {
                    Copyable copy = CreateCopyable(parent, left, right);

                    try {
                        File.WriteAllBytes(result, Encoder.Encode(copy).ToArray());

                    } catch (UnauthorizedAccessException) {
                        await MessageWindow.Create(
                            $"An error occurred while writing the file.\n\n" +
                            "You may not have sufficient privileges to write to the destination folder, or\n" +
                            "the current file already exists but cannot be overwritten.",
                            null, sender
                        );
                    }
                }
            }
        }
        
        public static async void Import(ISelectParent parent, int right, string path = null) {
            Window sender = ISelectParent.GetWindow(parent);

            if (path == null) {
                OpenFileDialog ofd = new OpenFileDialog() {
                    AllowMultiple = false,
                    Filters = new List<FileDialogFilter>() {
                        new FileDialogFilter() {
                            Extensions = new List<string>() {
                                "apdev"
                            },
                            Name = "Apollo Device Preset"
                        }
                    },
                    Title = "Import Device Preset"
                };

                string[] result = await ofd.ShowAsync(sender);

                if (result.Length > 0) path = result[0];
                else return;
            }
        
            Copyable loaded = await Copyable.DecodeFile(path, sender);
            
            if (loaded != null && InsertCopyable(parent, loaded, right, out Action undo, out Action redo, out Action dispose))
                Program.Project.Undo.Add("Device Imported", undo, redo, dispose);
        }
    }
}
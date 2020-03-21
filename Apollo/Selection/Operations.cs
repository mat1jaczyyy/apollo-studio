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
using Apollo.Undo;
using Apollo.Windows;

namespace Apollo.Selection {
    public static class Operations {
        static bool InsertCopyable(ISelectParent parent, Copyable paste, int right, out Action undo, out Action redo, out Action dispose) {
            undo = redo = dispose = null;

            List<ISelect> pasted;
            try {
                pasted = paste.Contents;
            } catch (InvalidCastException) {  // TODO test this out a lot
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

                chain.Selection?.Select(chain.IChildren[right + 1], true);
            };
            
            dispose = () => {
                foreach (ISelect device in pasted) device.Dispose();
                pasted = null;
            };

            for (int i = 0; i < paste.Contents.Count; i++)
                parent.IInsert(right + i + 1, pasted[i].IClone());
            
            parent.Selection?.Select(parent.IChildren[right + 1], true);
            
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
                Program.Project.Undo.Add($"{parent.ChildString} Pasted", undo, redo, dispose);
        }

        public static async void Replace(ISelectParent parent, int left, int right) {
            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && InsertCopyable(parent, paste, right, out Action undo, out Action redo, out Action dispose)) {
                DeleteRegion(parent, left, right, out Action undo2, out Action redo2, out Action dispose2);

                Path<ISelect> path = new Path<ISelect>((ISelect)parent);

                Program.Project.Undo.Add($"{parent.ChildString} Replaced",
                    undo2 + undo,
                    redo + redo2 + (() => {
                        ISelectParent chain = (ISelectParent)path.Resolve();

                        chain.Selection?.Select(chain.IChildren[left + paste.Contents.Count - 1], true);
                    }),
                    dispose2 + dispose
                );
                
                parent.Selection?.Select(parent.IChildren[left + paste.Contents.Count - 1], true);
            }
        }

        public abstract class SingleParentUndoEntry<T>: PathParentUndoEntry<T> where T: ISelectParent {
            protected override void UndoPath(params T[] items) => Undo(items[0]);
            protected virtual void Undo(T item) {}

            protected override void RedoPath(params T[] items) => Redo(items[0]);
            protected virtual void Redo(T item) {}

            public SingleParentUndoEntry(string desc, T item)
            : base(desc, item) {}
        }

        public class DuplicateUndoEntry: SingleParentUndoEntry<ISelectParent> {
            int left, right;

            protected override void Undo(ISelectParent parent) {
                for (int i = right - left; i >= 0; i--)
                    parent.Remove(right + i + 1);
            }

            protected override void Redo(ISelectParent parent) {
                for (int i = 0; i <= right - left; i++)
                    parent.IInsert(right + i + 1, parent.IChildren[left + i].IClone());
            
                parent.Selection?.Select(parent.IChildren[right + 1], true);
            }
            
            public DuplicateUndoEntry(ISelectParent parent, int left, int right)
            : base($"{parent.ChildString} Duplicated", parent) {
                this.left = left;
                this.right = right;
            }
        }

        public static void Duplicate(ISelectParent parent, int left, int right) {
            Program.Project.Undo.AddAndExecute(new DuplicateUndoEntry(
                parent,
                left,
                right
            ));
        }

        public static void Delete(ISelectParent parent, int left, int right) {
            DeleteRegion(parent, left, right, out Action undo, out Action redo, out Action dispose);
            Program.Project.Undo.Add($"{parent.ChildString} Removed", undo, redo, dispose);
        }

        public abstract class DeviceEncapsulationUndoEntry: SingleParentUndoEntry<Chain> {
            int left, right;
            Chain init;

            protected abstract Device Encapsulate(Chain chain);

            protected override void Undo(Chain chain) {
                chain.Remove(left);

                for (int i = left; i <= right; i++)
                    chain.Insert(i, init[i - left].Clone());
                
                chain.Selection?.Select(chain[left]);
                chain.Selection?.Select(chain[right], true);
            }

            protected override void Redo(Chain chain) {
                for (int i = right; i >= left; i--)
                    chain.Remove(i);
                
                chain.Insert(left, Encapsulate(init.Clone()));
            }

            public override void Dispose() => init.Dispose();
            
            public DeviceEncapsulationUndoEntry(Chain chain, int left, int right, string action)
            : base($"{chain.ChildString} {action}", chain) {
                this.left = left;
                this.right = right;

                init = new Chain();

                for (int i = left; i <= right; i++)
                    init.Add(chain[i].Clone());
            }
        }

        public abstract class DeviceDecapsulationUndoEntry<T>: SingleParentUndoEntry<Chain> where T: Device, IChainParent {
            int index;
            T init;

            protected abstract Chain GetChain(T container);

            protected override void Undo(Chain chain) {
                for (int i = index + GetChain(init).Count - 1; i >= index; i--)
                    chain.Remove(i);
                
                chain.Insert(index, init.Clone());
            }

            protected override void Redo(Chain chain) {
                Chain items = GetChain(init);

                chain.Remove(index);
            
                for (int i = 0; i < items.Count; i++)
                    chain.Insert(index + i, items[i].Clone());

                chain.Selection?.Select(chain[index], true);
            }

            public override void Dispose() => init.Dispose();
            
            public DeviceDecapsulationUndoEntry(Chain chain, int index, string action)
            : base($"{chain.ChildString} {action}", chain) {
                this.index = index;

                init = (T)chain[index].Clone();
            }
        }

        public class GroupUndoEntry: DeviceEncapsulationUndoEntry {
            protected override Device Encapsulate(Chain chain)
                => new Group(new List<Chain>() {chain}) {Expanded = 0};

            public GroupUndoEntry(Chain chain, int left, int right)
            : base(chain, left, right, "Grouped") {}
        }

        public static void Group(ISelectParent parent, int left, int right) {
            if (!(parent is Chain chain)) return;

            Program.Project.Undo.AddAndExecute(new GroupUndoEntry(
                chain,
                left,
                right
            ));
        }

        public class UngroupUndoEntry: DeviceDecapsulationUndoEntry<Group> {
            protected override Chain GetChain(Group group) => group[0];
            
            public UngroupUndoEntry(Chain chain, int index)
            : base(chain, index, "Ungrouped") {}
        }

        public static void Ungroup(ISelectParent parent, int index) {
            if (!(parent is Chain chain)) return;

            if (!(chain[index] is Group group) || group.Count != 1) return;

            Program.Project.Undo.AddAndExecute(new UngroupUndoEntry(
                chain,
                index
            ));
        }
        
        public class ChokeUndoEntry: DeviceEncapsulationUndoEntry {
            protected override Device Encapsulate(Chain chain)
                => new Choke(chain: chain.Clone());

            public ChokeUndoEntry(Chain chain, int left, int right)
            : base(chain, left, right, "Choked") {}
        }

        public static void Choke(ISelectParent parent, int left, int right) {
            if (!(parent is Chain chain)) return;
            
            Program.Project.Undo.AddAndExecute(new ChokeUndoEntry(
                chain,
                left,
                right
            ));
        }

        public class UnchokeUndoEntry: DeviceDecapsulationUndoEntry<Choke> {
            protected override Chain GetChain(Choke choke) => choke.Chain;
            
            public UnchokeUndoEntry(Chain chain, int index)
            : base(chain, index, "Unchoked") {}
        }

        public static void Unchoke(ISelectParent parent, int index) {
            if (!(parent is Chain chain)) return;

            if (!(chain[index] is Choke choke)) return;

            Program.Project.Undo.AddAndExecute(new UnchokeUndoEntry(
                chain,
                index
            ));
        }

        public class MuteUndoEntry: SingleParentUndoEntry<ISelectParent> {
            int left, right;
            List<bool> u;
            bool r;

            List<IMutable> GetMutables(ISelectParent parent) => parent.IChildren.Cast<IMutable>().ToList();

            protected override void Undo(ISelectParent parent) {
                List<IMutable> items = GetMutables(parent);

                for (int i = left; i <= right; i++)
                    items[i].Enabled = u[i - left];
            }

            protected override void Redo(ISelectParent parent) {
                List<IMutable> items = GetMutables(parent);

                for (int i = left; i <= right; i++)
                    items[i].Enabled = r;
            }
            
            public MuteUndoEntry(ISelectParent parent, int left, int right)
            : base($"{parent.ChildString} Muted", parent) {
                this.left = left;
                this.right = right;
                
                List<IMutable> items = GetMutables(parent);

                u = (from i in Enumerable.Range(left, right - left + 1) select items[i].Enabled).ToList();
                r = !items[left].Enabled;
            }
        }

        public static void Mute(ISelectParent parent, int left, int right) {
            if (!(parent.IChildren[left] is IMutable)) return;

            Program.Project.Undo.AddAndExecute(new MuteUndoEntry(
                parent,
                left,
                right
            ));
        }

        public static void Rename(ISelectParent parent, int left, int right) {
            // TODO Implement
        }

        static List<FileDialogFilter> CreateFilters(ISelectParent parent) => new List<FileDialogFilter>() {
            new FileDialogFilter() {
                Extensions = new List<string>() {parent.ChildFileExtension},
                Name = $"Apollo {parent.ChildString} Preset"
            }
        };

        static SaveFileDialog CreateSFD(ISelectParent parent) => new SaveFileDialog() {
            Filters = CreateFilters(parent),
            Title = $"Export {parent.ChildString} Preset"
        };

        static OpenFileDialog CreateOFD(ISelectParent parent) => new OpenFileDialog() {
            AllowMultiple = false,
            Filters = CreateFilters(parent),
            Title = $"Import {parent.ChildString} Preset"
        };

        public static async void Export(ISelectParent parent, int left, int right) {
            Window sender = parent.IWindow;
            
            string result = await CreateSFD(parent).ShowAsync(sender);

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
            Window sender = parent.IWindow;

            if (path == null) {
                string[] result = await CreateOFD(parent).ShowAsync(sender);

                if (result.Length > 0) path = result[0];
                else return;
            }
        
            Copyable loaded = await Copyable.DecodeFile(path, sender);
            
            if (loaded != null && InsertCopyable(parent, loaded, right, out Action undo, out Action redo, out Action dispose))
                Program.Project.Undo.Add($"{parent.ChildString} Imported", undo, redo, dispose);
        }
    }
}
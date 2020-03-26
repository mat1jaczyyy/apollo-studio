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
    static class Operations {
        public abstract class SingleParentUndoEntry<T>: PathParentUndoEntry<T> where T: ISelectParent {
            protected override void UndoPath(params T[] items) => Undo(items[0]);
            protected virtual void Undo(T item) {}

            protected override void RedoPath(params T[] items) => Redo(items[0]);
            protected virtual void Redo(T item) {}

            public SingleParentUndoEntry(string desc, T item)
            : base(desc, item) {}
        }

        public abstract class PatternDrawProtectedUndoEntry<T>: SingleParentUndoEntry<T> where T: ISelectParent {
            void SetDraw(T item, bool value) {
                if (item is Pattern pattern && pattern.Window != null) 
                    if (pattern.Window.Draw = value)
                        pattern.Window.Frame_Select(pattern.Expanded);
            }

            void Action(T item, bool value) {
                SetDraw(item, false);
                
                if (value) base.RedoPath(item);
                else base.UndoPath(item);
                
                SetDraw(item, true);
            }

            protected override void UndoPath(params T[] items) => Action(items[0], false);
            protected override void RedoPath(params T[] items) => Action(items[0], true);

            public PatternDrawProtectedUndoEntry(string desc, T item)
            : base(desc, item) {}
        }

        public class InsertCopyableUndoEntry: PatternDrawProtectedUndoEntry<ISelectParent> {
            int right;
            public List<ISelect> init { get; private set; }

            protected override void Undo(ISelectParent parent) {
                for (int i = init.Count - 1; i >= 0; i--)
                    parent.Remove(right + i + 1);
            }

            protected override void Redo(ISelectParent parent) {
                for (int i = 0; i < init.Count; i++)
                    parent.IInsert(right + i + 1, init[i].IClone());

                parent.Selection?.Select(parent.IChildren[right + 1], true);
            }

            public override void Dispose() {
                foreach (ISelect item in init) item.Dispose();
                init = null;
            }
            
            public InsertCopyableUndoEntry(ISelectParent parent, Copyable copyable, int right, string action)
            : base($"{parent.ChildString} {action}", parent) {
                this.right = right;

                init = copyable.Contents;
            }
        }

        static bool InsertCopyable(ISelectParent parent, Copyable copyable, int right, string action, out InsertCopyableUndoEntry entry) {
            entry = null;

            if (!parent.ChildType.IsAssignableFrom(copyable.Type)) return false;

            entry = new InsertCopyableUndoEntry(parent, copyable, right, action);
            
            return true;
        }

        static Copyable CreateCopyable(ISelectParent parent, int left, int right) {
            Copyable copy = new Copyable();
            
            for (int i = left; i <= right; i++)
                copy.Contents.Add(parent.IChildren[i]);
            
            return copy;
        }

        public static void Copy(ISelectParent parent, int left, int right, bool cut = false) {
            CreateCopyable(parent, left, right).StoreToClipboard();

            if (cut) Delete(parent, left, right);
        }

        public static async void Paste(ISelectParent parent, int right) {
            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && InsertCopyable(parent, paste, right, "Pasted", out InsertCopyableUndoEntry entry))
                Program.Project.Undo.AddAndExecute(entry);
        }
        
        public class DeleteUndoEntry: PatternDrawProtectedUndoEntry<ISelectParent> {
            public int left { get; private set; }
            int right;
            List<ISelect> init;

            protected override void Undo(ISelectParent parent) {
                for (int i = left; i <= right; i++)
                    parent.IInsert(i, init[i - left].IClone());
            }

            protected override void Redo(ISelectParent parent) {
                for (int i = right; i >= left; i--)
                    parent.Remove(i);
            }

            public override void Dispose() {
               foreach (ISelect item in init) item.Dispose();
               init = null;
            }
            
            public DeleteUndoEntry(ISelectParent parent, int left, int right)
            : base($"{parent.ChildString} Removed", parent) {
                this.left = left;
                this.right = right;

                init = (from i in Enumerable.Range(left, right - left + 1) select parent.IChildren[i].IClone()).ToList();
            }
        }

        public static void Delete(ISelectParent parent, int left, int right) {
            if (parent is Pattern pattern && pattern.Count - (right - left + 1) == 0) return;

            Program.Project.Undo.AddAndExecute(new DeleteUndoEntry(parent, left, right));
        }
        
        public class ReplaceUndoEntry: SingleParentUndoEntry<ISelectParent> {
            DeleteUndoEntry delete;
            InsertCopyableUndoEntry insert;

            protected override void Undo(ISelectParent parent) {
                delete.Undo();
                insert.Undo();
            }

            protected override void Redo(ISelectParent parent) {
                insert.Redo();
                delete.Redo();

                parent.Selection?.Select(parent.IChildren[delete.left + insert.init.Count - 1], true);
            }

            public override void Dispose() {
               delete.Dispose();
               insert.Dispose();
            }
            
            public ReplaceUndoEntry(ISelectParent parent, DeleteUndoEntry delete, InsertCopyableUndoEntry insert)
            : base($"{parent.ChildString} Replaced", parent) {
                this.delete = delete;
                this.insert = insert;
            }
        }

        public static async void Replace(ISelectParent parent, int left, int right) {
            Copyable paste = await Copyable.DecodeClipboard();

            if (paste != null && InsertCopyable(parent, paste, right, "", out InsertCopyableUndoEntry insert))
                Program.Project.Undo.AddAndExecute(new ReplaceUndoEntry(
                    parent,
                    new DeleteUndoEntry(parent, left, right),
                    insert
                ));
        }

        public class DuplicateUndoEntry: PatternDrawProtectedUndoEntry<ISelectParent> {
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

        public static void Duplicate(ISelectParent parent, int left, int right)
            => Program.Project.Undo.AddAndExecute(new DuplicateUndoEntry(parent, left, right));

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

            public override void Dispose() {
                init.Dispose();
                init = null;
            }
            
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

            Program.Project.Undo.AddAndExecute(new GroupUndoEntry(chain, left, right));
        }

        public class UngroupUndoEntry: DeviceDecapsulationUndoEntry<Group> {
            protected override Chain GetChain(Group group) => group[0];
            
            public UngroupUndoEntry(Chain chain, int index)
            : base(chain, index, "Ungrouped") {}
        }

        public static void Ungroup(ISelectParent parent, int index) {
            if (!(parent is Chain chain) || !(chain[index] is Group group) || group.Count != 1) return;

            Program.Project.Undo.AddAndExecute(new UngroupUndoEntry(chain, index));
        }
        
        public class ChokeUndoEntry: DeviceEncapsulationUndoEntry {
            protected override Device Encapsulate(Chain chain)
                => new Choke(chain: chain.Clone());

            public ChokeUndoEntry(Chain chain, int left, int right)
            : base(chain, left, right, "Choked") {}
        }

        public static void Choke(ISelectParent parent, int left, int right) {
            if (!(parent is Chain chain)) return;
            
            Program.Project.Undo.AddAndExecute(new ChokeUndoEntry(chain, left, right));
        }

        public class UnchokeUndoEntry: DeviceDecapsulationUndoEntry<Choke> {
            protected override Chain GetChain(Choke choke) => choke.Chain;
            
            public UnchokeUndoEntry(Chain chain, int index)
            : base(chain, index, "Unchoked") {}
        }

        public static void Unchoke(ISelectParent parent, int index) {
            if (!(parent is Chain chain) || !(chain[index] is Choke choke)) return;

            Program.Project.Undo.AddAndExecute(new UnchokeUndoEntry(chain, index));
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

            Program.Project.Undo.AddAndExecute(new MuteUndoEntry(parent, left, right));
        }

        public static void Rename(ISelectParent parent, int left, int right) {
            if (parent.IChildren[left].IInfo is IRenamable renamable)
                renamable.Rename.StartInput(left, right);
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
            if (parent.ChildFileExtension == null) return;

            Window sender = parent.IWindow;
            
            string result = await CreateSFD(parent).ShowAsync(sender);

            if (result != null) {
                string[] file = result.Split(Path.DirectorySeparatorChar);

                if (Directory.Exists(string.Join("/", file.Take(file.Count() - 1)))) {
                    Copyable copy = CreateCopyable(parent, left, right);

                    try {
                        File.WriteAllBytes(result, Encoder.Encode(copy).ToArray());  // TODO move this to Copyable.StoreToFile?

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
            if (parent.ChildFileExtension == null) return;

            Window sender = parent.IWindow;

            if (path == null) {
                string[] result = await CreateOFD(parent).ShowAsync(sender);

                if (result.Length > 0) path = result[0];
                else return;
            }
        
            Copyable loaded = await Copyable.DecodeFile(path, sender);
            
            if (loaded != null && InsertCopyable(parent, loaded, right, "Imported", out InsertCopyableUndoEntry entry))
                Program.Project.Undo.AddAndExecute(entry);
        }
    }
}
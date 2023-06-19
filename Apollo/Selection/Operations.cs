using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Avalonia.Controls;

using Apollo.Binary;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Helpers;
using Apollo.Undo;

namespace Apollo.Selection {
    static class Operations {
        public abstract class PatternDrawProtectedUndoEntry<T>: SinglePathUndoEntry<T> {
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
            
            protected PatternDrawProtectedUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
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
                    parent.IInsert(right + i + 1, init[i].IClone(PurposeType.Active));

                parent.Selection?.Select(parent.IChildren[right + 1], true);
            }

            protected override void OnDispose() {
                foreach (ISelect item in init) item.Dispose();
                init = null;
            }
            
            public InsertCopyableUndoEntry(ISelectParent parent, Copyable copyable, int right, string action)
            : base($"{parent.ChildString} {action}", parent) {
                this.right = right;

                init = copyable.Contents;
            }
            
            InsertCopyableUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {
                right = reader.ReadInt32();

                init = Enumerable.Range(0, reader.ReadInt32()).Select(i => Decoder.DecodeAnything<ISelect>(reader, version, PurposeType.Passive)).ToList();
            }
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                writer.Write(right);
                
                writer.Write(init.Count);
                for (int i = 0; i < init.Count; i++)
                    Encoder.EncodeAnything<ISelect>(writer, init[i]);
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
                    parent.IInsert(i, init[i - left].IClone(PurposeType.Active));
            }

            protected override void Redo(ISelectParent parent) {
                for (int i = right; i >= left; i--)
                    parent.Remove(i);
            }

            protected override void OnDispose() {
               foreach (ISelect item in init) item.Dispose();
               init = null;
            }
            
            public DeleteUndoEntry(ISelectParent parent, int left, int right)
            : base($"{parent.ChildString} Removed", parent) {
                this.left = left;
                this.right = right;

                init = Enumerable.Range(left, right - left + 1).Select(i => parent.IChildren[i].IClone(PurposeType.Passive)).ToList();
            }
            
            DeleteUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {
                left = reader.ReadInt32();
                right = reader.ReadInt32();
                
                init = Enumerable.Range(0, reader.ReadInt32()).Select(i => Decoder.DecodeAnything<ISelect>(reader, version, PurposeType.Passive)).ToList();
            }
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                writer.Write(left);
                writer.Write(right);
                
                writer.Write(init.Count);
                for (int i = 0; i < init.Count; i++)
                    Encoder.EncodeAnything<ISelect>(writer, init[i]);
            }
        }

        public static void Delete(ISelectParent parent, int left, int right) {
            if (parent is Pattern pattern && pattern.Count - (right - left + 1) == 0) return;

            Program.Project.Undo.AddAndExecute(new DeleteUndoEntry(parent, left, right));
        }
        
        public class ReplaceUndoEntry: SinglePathUndoEntry<ISelectParent> {
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

            protected override void OnDispose() {
               delete.Dispose();
               insert.Dispose();
            }
            
            public ReplaceUndoEntry(ISelectParent parent, DeleteUndoEntry delete, InsertCopyableUndoEntry insert)
            : base($"{parent.ChildString} Replaced", parent) {
                this.delete = delete;
                this.insert = insert;
            }
            
            ReplaceUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {
                delete = (DeleteUndoEntry)DecodeEntry(reader, version);
                insert = (InsertCopyableUndoEntry)DecodeEntry(reader, version);
            }
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                delete.Encode(writer);
                insert.Encode(writer);
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
                    parent.IInsert(right + i + 1, parent.IChildren[left + i].IClone(PurposeType.Active));
            
                parent.Selection?.Select(parent.IChildren[right + 1], true);
            }
            
            public DuplicateUndoEntry(ISelectParent parent, int left, int right)
            : base($"{parent.ChildString} Duplicated", parent) {
                this.left = left;
                this.right = right;
            }
            
            DuplicateUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {
                left = reader.ReadInt32();
                right = reader.ReadInt32();
            }
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                writer.Write(left);
                writer.Write(right);
            }
        }

        public static void Duplicate(ISelectParent parent, int left, int right)
            => Program.Project.Undo.AddAndExecute(new DuplicateUndoEntry(parent, left, right));

        public abstract class DeviceEncapsulationUndoEntry: SinglePathUndoEntry<Chain> {
            int left, right;
            Chain init;

            protected abstract Device Encapsulate(Chain chain, Chain parent);

            protected override void Undo(Chain chain) {
                chain.Remove(left);

                for (int i = left; i <= right; i++)
                    chain.Insert(i, init[i - left].Clone(PurposeType.Active));
                
                chain.Selection?.Select(chain[left]);
                chain.Selection?.Select(chain[right], true);
            }

            protected override void Redo(Chain chain) {
                for (int i = right; i >= left; i--)
                    chain.Remove(i);
                
                chain.Insert(left, Encapsulate(init.Clone(PurposeType.Active), chain));
            }

            protected override void OnDispose() {
                init.Dispose();
                init = null;
            }
            
            public DeviceEncapsulationUndoEntry(Chain chain, int left, int right, string action)
            : base($"{chain.ChildString} {action}", chain) {
                this.left = left;
                this.right = right;

                init = new Chain();

                for (int i = left; i <= right; i++)
                    init.Add(chain[i].Clone(PurposeType.Passive));
            }
        
            protected DeviceEncapsulationUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {
                left = reader.ReadInt32();
                right = reader.ReadInt32();
                
                init = Decoder.Decode<Chain>(reader, version, PurposeType.Passive);
            }
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                writer.Write(left);
                writer.Write(right);
                
                Encoder.Encode(writer, init);
            }
        }

        public abstract class DeviceDecapsulationUndoEntry<T>: SinglePathUndoEntry<Chain> where T: Device, IChainParent {
            int index;
            T init;

            protected abstract Chain GetChain(T container);

            protected override void Undo(Chain chain) {
                for (int i = index + GetChain(init).Count - 1; i >= index; i--)
                    chain.Remove(i);
                
                chain.Insert(index, init.Clone(PurposeType.Active));
            }

            protected override void Redo(Chain chain) {
                Chain items = GetChain(init);

                chain.Remove(index);
            
                for (int i = 0; i < items.Count; i++)
                    chain.Insert(index + i, items[i].Clone(PurposeType.Active));

                chain.Selection?.Select(chain[index], true);
            }

            protected override void OnDispose() => init.Dispose();
            
            public DeviceDecapsulationUndoEntry(Chain chain, int index, string action)
            : base($"{chain.ChildString} {action}", chain) {
                this.index = index;

                init = (T)chain[index].Clone(PurposeType.Passive);
            }
        
            protected DeviceDecapsulationUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {
                index = reader.ReadInt32();
                init = (T)Decoder.Decode<Device>(reader, version, PurposeType.Passive);
            }
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                writer.Write(index);
                Encoder.Encode(writer, (Device)init);
            }
        }

        public class GroupUndoEntry: DeviceEncapsulationUndoEntry {
            protected override Device Encapsulate(Chain chain, Chain parent)
                => Device.Create<Group>(PurposeType.Active, parent, new object[] {
                    new List<Chain>() {chain},
                    0,
                    Type.Missing
                });

            public GroupUndoEntry(Chain chain, int left, int right)
            : base(chain, left, right, "Grouped") {}
            
            GroupUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }

        public static void Group(ISelectParent parent, int left, int right) {
            if (!(parent is Chain chain)) return;

            Program.Project.Undo.AddAndExecute(new GroupUndoEntry(chain, left, right));
        }

        public class UngroupUndoEntry: DeviceDecapsulationUndoEntry<Group> {
            protected override Chain GetChain(Group group) => group[0];
            
            public UngroupUndoEntry(Chain chain, int index)
            : base(chain, index, "Ungrouped") {}
            
            UngroupUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }

        public static void Ungroup(ISelectParent parent, int index) {
            if (!(parent is Chain chain) || !(chain[index] is Group group) || group.Count != 1) return;

            Program.Project.Undo.AddAndExecute(new UngroupUndoEntry(chain, index));
        }
        
        public class ChokeUndoEntry: DeviceEncapsulationUndoEntry {
            protected override Device Encapsulate(Chain chain, Chain parent)
                => Device.Create<Choke>(PurposeType.Active, parent, new object[] {
                    Type.Missing,
                    chain
                });

            public ChokeUndoEntry(Chain chain, int left, int right)
            : base(chain, left, right, "Choked") {}
            
            ChokeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }

        public static void Choke(ISelectParent parent, int left, int right) {
            if (!(parent is Chain chain)) return;
            
            Program.Project.Undo.AddAndExecute(new ChokeUndoEntry(chain, left, right));
        }

        public class UnchokeUndoEntry: DeviceDecapsulationUndoEntry<Choke> {
            protected override Chain GetChain(Choke choke) => choke.Chain;
            
            public UnchokeUndoEntry(Chain chain, int index)
            : base(chain, index, "Unchoked") {}
            
            UnchokeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }

        public static void Unchoke(ISelectParent parent, int index) {
            if (!(parent is Chain chain) || !(chain[index] is Choke choke)) return;

            Program.Project.Undo.AddAndExecute(new UnchokeUndoEntry(chain, index));
        }

        public class MuteUndoEntry: SinglePathUndoEntry<ISelectParent> {
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

                u = Enumerable.Range(left, right - left + 1).Select(i => items[i].Enabled).ToList();
                r = !items[left].Enabled;
            }
        
            MuteUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {
                left = reader.ReadInt32();
                right = reader.ReadInt32();
                
                u = Enumerable.Range(0, reader.ReadInt32()).Select(i => reader.ReadBoolean()).ToList();
                
                r = reader.ReadBoolean();
            }
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                writer.Write(left);
                writer.Write(right);
                
                writer.Write(u.Count);
                for (int i = 0; i < u.Count; i++)
                    writer.Write(u[i]);
                
                writer.Write(r);
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
            AllowMultiple = true,
            Filters = CreateFilters(parent),
            Title = $"Import {parent.ChildString} Preset"
        };

        public static async void Export(ISelectParent parent, int left, int right) {
            if (parent.ChildFileExtension == null) return;

            Window sender = parent.IWindow;
            
            string result = await CreateSFD(parent).ShowAsync(sender);

            if (result != null) {
                string[] file = result.Split(Path.DirectorySeparatorChar);

                if (Directory.Exists(string.Join("/", file.Take(file.Count() - 1))))
                    await CreateCopyable(parent, left, right).StoreToFile(result, sender);
            }
        }
        
        public static async void Import(ISelectParent parent, int right, string[] paths = null) {
            if (parent.ChildFileExtension == null) return;

            Window sender = parent.IWindow;

            paths = paths?? await CreateOFD(parent).ShowAsync(sender);
            
            if (!paths.Any()) return;
        
            Copyable loaded = await Copyable.DecodeFile(paths, sender, parent.ChildType);
            
            if (loaded != null && InsertCopyable(parent, loaded, right, "Imported", out InsertCopyableUndoEntry entry))
                Program.Project.Undo.AddAndExecute(entry);
        }
    }
}
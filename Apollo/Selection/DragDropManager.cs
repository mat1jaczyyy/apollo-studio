using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Selection {
    public class DragDropManager {
        public const string FileNames = "Files";
        static readonly Dictionary<string, DataFormat<List<ISelect>>> formats = new();
        public static DataFormat<List<ISelect>> SelectionFormat(string name) {
            if (!formats.TryGetValue(name, out var format))
                formats.Add(name, format = DataFormat.CreateInProcessFormat<List<ISelect>>(name));
            return format;
        }

        static bool Contains(DragEventArgs e, string format) => format == FileNames
            ? e.DataTransfer.Contains(DataFormat.File)
            : e.DataTransfer.Contains(SelectionFormat(format));
        public static bool CanMove(List<ISelect> source, ISelectParent target, int position, bool copy) {
            if (source == null || source.Count == 0 || target == null || position < -1 || position >= target.Count)
                return false;

            if (!(source[0] is Track) && !copy && Track.PathContains((ISelect)target, source)) return false;

            if (!copy && ((source[0] is Frame && source[0].IParent != target && source[0].IParent.Count == source.Count) ||
                ((position == -1)
                    ? target.Count > 0 && source[0] == target.IChildren[0]
                    : source.Contains(target.IChildren[position]) || (source[0].IParent == target && source[0].IParentIndex == position + 1)
                )
            )) return false;

            return true;
        }

        static int AdjustAfter(List<ISelect> source, ISelectParent target, int after, bool copy) {
            if (source == null || source.Count == 0 || CanMove(source, target, after, copy))
                return after;
            if (copy || source[0].IParent != target)
                return after;

            if (after >= 0 && after < target.Count && source.Contains(target.IChildren[after])) {
                int next = source.Max(s => s.IParentIndex.Value) + 1;
                if (next < target.Count && CanMove(source, target, next, copy))
                    return next;
                return after;
            }

            if (source[0].IParentIndex == after + 1) {
                int prev = after - 1;
                if (CanMove(source, target, prev, copy))
                    return prev;
            }

            return after;
        }

        public static bool Move(List<ISelect> source, ISelectParent target, int position, bool copy, out Path<ISelectParent> premove) {
            premove = null;

            if (!CanMove(source, target, position, copy)) return false;

            premove = new Path<ISelectParent>(target);

            ISelect point = (position == -1)? null : target.IChildren[position];

            for (int i = 0; i < source.Count; i++) {
                if (!copy) source[i].IParent.Remove(source[i].IParentIndex.Value, false);

                source[i] = copy? source[i].IClone(PurposeType.Active) : source[i];

                if (source[i] is Pattern pattern)
                    pattern.Window?.Close();

                target.IInsert((point?.IParentIndex.Value?? -1) + i + 1, source[i]);
            }

            SelectionManager selection = target.Selection;
            selection?.Select(source[0]);
            selection?.Select(source.Last(), true);
            
            return true;
        }

        IDroppable Host;
        HashSet<Control> Subscribed = new();

        public delegate bool DropHandler(Control source, ISelectParent parent, ISelect child, int after, string format, DragEventArgs e);
        Dictionary<string, DropHandler> DropHandlers = new();

        static bool DefaultDrop(Control source, ISelectParent parent, ISelect child, int after, string format, DragEventArgs e) {
            List<ISelect> moving = (List<ISelect>)e.DataTransfer.TryGetValue(DragDropManager.SelectionFormat(format));
            if (moving == null || moving.Count == 0) return false;
            ISelectParent source_parent = moving[0].IParent;
            int before = moving[0].IParentIndex.Value - 1;

            bool copy = e.KeyModifiers.HasFlag(App.ControlKey);
            bool result;

            if (result = Move(moving, parent, after, copy, out Path<ISelectParent> premove)) {
                int before_pos = before;
                int after_pos = moving[0].IParentIndex.Value - 1;
                int count = moving.Count;

                if (source_parent == parent && after < before)
                    before_pos += count;

                Program.Project.Undo.Add(new DragDropUndoEntry(source_parent, premove, parent, copy, count, before, after, before_pos, after_pos, format));
            }

            return result;
        }

        static bool FileDrop(Control source, ISelectParent parent, ISelect child, int after, string format, DragEventArgs e) {
            string[] paths = e.DataTransfer.TryGetFiles()?.Select(file => file.TryGetLocalPath()).Where(path => path != null)?.ToArray();

            if (paths != null) Operations.Import(parent, after, paths);

            return true;
        }

        public DragDropManager(IDroppable control) {
            Host = control;

            foreach (KeyValuePair<string, DropHandler> entry in Host.DropHandlers)
                DropHandlers.Add(
                    entry.Key,
                    entry.Value?? ((entry.Key == DragDropManager.FileNames)
                        ? new DropHandler(FileDrop)
                        : DefaultDrop
                    )
                );

            Subscribe((Control)Host);
        }

        public void Subscribe(Control control) {
            if (!Subscribed.Add(control)) return;
            
            control.AddHandler(DragDrop.DragOverEvent, DragOver);
            control.AddHandler(DragDrop.DropEvent, Drop);
        }

        static bool MovedPastThreshold(Point start, Point current) {
            Vector delta = current - start;
            return Math.Abs(delta.X) >= 4 || Math.Abs(delta.Y) >= 4;
        }

        static async Task<bool> WaitForDrag(Control control, PointerPressedEventArgs e) {
            IPointer pointer = e.Pointer;
            Point start = e.GetPosition(control);
            TaskCompletionSource<bool> tcs = new();

            void Moved(object sender, PointerEventArgs ev) {
                if (ev.Pointer == pointer && MovedPastThreshold(start, ev.GetPosition(control)))
                    tcs.TrySetResult(true);
            }

            void Released(object sender, PointerReleasedEventArgs ev) {
                if (ev.Pointer == pointer && ev.InitialPressMouseButton == MouseButton.Left)
                    tcs.TrySetResult(false);
            }

            void CaptureLost(object sender, PointerCaptureLostEventArgs ev) {
                if (ev.Pointer == pointer)
                    tcs.TrySetResult(false);
            }

            void Detached(object sender, VisualTreeAttachmentEventArgs ev) => tcs.TrySetResult(false);

            void KeyDown(object sender, KeyEventArgs ev) {
                if (ev.Key != Key.Escape) return;
                ev.Handled = true;
                tcs.TrySetResult(false);
            }

            var window = TopLevel.GetTopLevel(control);
            pointer.Capture(control);
            control.PointerMoved += Moved;
            control.PointerReleased += Released;
            control.PointerCaptureLost += CaptureLost;
            control.DetachedFromVisualTree += Detached;
            window?.AddHandler(InputElement.KeyDownEvent, KeyDown, RoutingStrategies.Tunnel);

            try {
                return await tcs.Task;
            } finally {
                control.PointerMoved -= Moved;
                control.PointerReleased -= Released;
                control.PointerCaptureLost -= CaptureLost;
                control.DetachedFromVisualTree -= Detached;
                window?.RemoveHandler(InputElement.KeyDownEvent, KeyDown);
                if (pointer.Captured == control)
                    pointer.Capture(null);
            }
        }

        public async void Drag(SelectionManager selection, PointerPressedEventArgs e) {
            if (App.Dragging || selection == null || !(Host is IDraggable drag)) return;

            if (!drag.Selected) drag.Select(e);

            PointerUpdateKind mouseButton = e.GetCurrentPoint((Control)Host).Properties.PointerUpdateKind;

            if (e.ClickCount > 1 || mouseButton != PointerUpdateKind.LeftButtonPressed) {
                if (drag.Selected) drag.Select(e);
                drag.DragFailed(e);
                return;
            }

            if (!await WaitForDrag((Control)Host, e)) {
                if (Host != null) {
                    if (drag.Selected) drag.Select(e);
                    drag.DragFailed(e);
                }
                return;
            }

            using var dragData = new DataTransfer();
            var dragItem = new DataTransferItem();
            dragItem.Set(SelectionFormat(drag.DragFormat), selection.Selection);
            dragData.Add(dragItem);

            if (App.Dragging) return;

            App.Dragging = true;
            DragDropEffects result;
            try {
                result = await DoInProcessDrag((Control)Host, e, dragData);
            } finally {
                App.Dragging = false;
            }

            if (result == DragDropEffects.None && Host != null) {
                if (drag.Selected) drag.Select(e);
                drag.DragFailed(e);
            }
        }

        static Interactive HitTest(Control origin, PointerEventArgs ev) {
            PixelPoint screen = origin.PointToScreen(ev.GetPosition(origin));

            IEnumerable<Window> windows = App.Windows.Where(window => window.IsVisible && window.WindowState != WindowState.Minimized)
                .OrderByDescending(window => window.IsActive);

            foreach (Window window in windows) {
                Point local = window.PointToClient(screen);
                Size size = window.Bounds.Size;
                if (local.X < 0 || local.Y < 0 || local.X > size.Width || local.Y > size.Height)
                    continue;

                if (window.InputHitTest(local) is Interactive hit)
                    return hit;
            }

            return null;
        }

        static readonly Cursor CopyCursor = new(StandardCursorType.DragCopy);
        static readonly Cursor MoveCursor = new(StandardCursorType.DragMove);
        static readonly Cursor NoneCursor = new(StandardCursorType.No);
        static readonly Dictionary<Window, Cursor> OriginalCursors = new();
        static bool DragActive;

        static void SetDragCursor(DragDropEffects effects) {
            if (!DragActive) return;

            Cursor cursor = effects == DragDropEffects.None ? NoneCursor
                : effects.HasFlag(DragDropEffects.Copy) ? CopyCursor : MoveCursor;

            foreach (Window window in App.Windows) {
                OriginalCursors.TryAdd(window, window.Cursor);
                window.Cursor = cursor;
            }
        }

        static void ClearDragCursor() {
            DragActive = false;
            foreach (var entry in OriginalCursors)
                entry.Key.Cursor = entry.Value;
            OriginalCursors.Clear();
        }

        static Interactive DragTarget(Interactive hit, PointerEventArgs ev, out Point local) {
            for (Control control = hit as Control; control != null; control = control.Parent as Control) {
                string name = control.Name;
                if (name == "DropZone" || name == "DropZoneAfter" || name == "DropZoneBefore" ||
                    name == "DropZoneHead" || name == "DropZoneTail" || name == "Contents" || name == "TrackAdd") {
                    local = ev.GetPosition(control);
                    return control;
                }
            }

            local = ev.GetPosition((Visual)hit);
            return hit;
        }

        static DragDropEffects RaiseDrag(RoutedEvent<DragEventArgs> routed, Control origin, PointerEventArgs ev, IDataTransfer data) {
            Interactive hit = HitTest(origin, ev);
            if (hit == null) {
                SetDragCursor(DragDropEffects.None);
                return DragDropEffects.None;
            }

            Interactive target = DragTarget(hit, ev, out Point local);
            DragEventArgs args = new(routed, data, target, local, ev.KeyModifiers) {
                DragEffects = DragDropEffects.Move
            };
            target.RaiseEvent(args);
            DragDropEffects effects = args.Handled? args.DragEffects : DragDropEffects.None;
            if (routed != DragDrop.DropEvent)
                SetDragCursor(effects);
            return effects;
        }

        static async Task<DragDropEffects> DoInProcessDrag(Control origin, PointerEventArgs start, IDataTransfer data) {
            IPointer pointer = start.Pointer;
            TaskCompletionSource<DragDropEffects> tcs = new();
            bool dropping = false;
            DragActive = true;
            DragDropEffects lastEffects = RaiseDrag(DragDrop.DragOverEvent, origin, start, data);

            void Moved(object sender, PointerEventArgs ev) {
                if (ev.Pointer == pointer)
                    lastEffects = RaiseDrag(DragDrop.DragOverEvent, origin, ev, data);
            }

            void Released(object sender, PointerReleasedEventArgs ev) {
                if (ev.Pointer != pointer || ev.InitialPressMouseButton != MouseButton.Left) return;
                // Moving an item removes its old viewer during Drop. That detach/capture
                // loss must not complete the gesture before the drop handler finishes.
                dropping = true;
                try {
                    lastEffects = RaiseDrag(DragDrop.DropEvent, origin, ev, data);
                    tcs.TrySetResult(lastEffects);
                } finally { dropping = false; }
            }

            void CaptureLost(object sender, PointerCaptureLostEventArgs ev) {
                if (ev.Pointer == pointer && !dropping)
                    tcs.TrySetResult(DragDropEffects.None);
            }

            void Detached(object sender, VisualTreeAttachmentEventArgs ev) {
                if (!dropping) tcs.TrySetResult(DragDropEffects.None);
            }

            void KeyDown(object sender, KeyEventArgs ev) {
                if (ev.Key != Key.Escape) return;
                ev.Handled = true;
                tcs.TrySetResult(DragDropEffects.None);
            }

            var window = TopLevel.GetTopLevel(origin);
            pointer.Capture(origin);
            origin.PointerMoved += Moved;
            origin.PointerReleased += Released;
            origin.PointerCaptureLost += CaptureLost;
            origin.DetachedFromVisualTree += Detached;
            window?.AddHandler(InputElement.KeyDownEvent, KeyDown, RoutingStrategies.Tunnel);

            try {
                return await tcs.Task;
            } finally {
                try {
                    origin.PointerMoved -= Moved;
                    origin.PointerReleased -= Released;
                    origin.PointerCaptureLost -= CaptureLost;
                    origin.DetachedFromVisualTree -= Detached;
                    window?.RemoveHandler(InputElement.KeyDownEvent, KeyDown);
                    if (pointer.Captured == origin)
                        pointer.Capture(null);
                } catch {}
                ClearDragCursor();
            }
        }

        bool TryFindDropArea(DragEventArgs e, out Control source) {
            source = (Control)e.Source;

            while (source != null) {
                if (Host.DropAreas.Contains(source.Name))
                    return true;

                if (source == Host || (source is IDroppable && source != Host))
                    break;

                source = source.Parent as Control;
            }

            source = null;
            return false;
        }

        bool TryResolveDrop(DragEventArgs e, out Control source, out int after) {
            after = -1;
            if (!TryFindDropArea(e, out source) || !Host.DropApplies(source, e)) {
                source = null;
                return false;
            }

            after = (Host.Item?.IParentIndex - Convert.ToInt32(Host.DropLeft(source, e)))??
                ((source.Name == "DropZoneAfter")? Host.ItemParent.Count - 1 : -1);
            return true;
        }

        void DragOver(object sender, DragEventArgs e) {
            if (!TryFindDropArea(e, out Control source))
                return;

            e.Handled = true;

            bool applies = Host.DropApplies(source, e);
            int after = (Host.Item?.IParentIndex - Convert.ToInt32(Host.DropLeft(source, e)))??
                ((source.Name == "DropZoneAfter")? Host.ItemParent.Count - 1 : -1);

            if (!applies || !DropHandlers.Keys.Any(format => Contains(e, format))) {
                e.DragEffects = DragDropEffects.None;
                return;
            }

            bool copy = e.KeyModifiers.HasFlag(App.ControlKey);
            bool allowed = false;
            int slot = after;

            foreach (string format in DropHandlers.Keys.Where(i => Contains(e, i))) {
                if (format == FileNames) {
                    allowed = true;
                    break;
                }

                List<ISelect> items = (List<ISelect>)e.DataTransfer.TryGetValue(SelectionFormat(format));
                if (items == null) continue;

                slot = DropHandlers[format] == DefaultDrop
                    ? AdjustAfter(items, Host.ItemParent, after, copy)
                    : after;

                if (DropHandlers[format] != DefaultDrop || CanMove(items, Host.ItemParent, slot, copy)) {
                    allowed = true;
                    break;
                }
            }

            e.DragEffects = !allowed ? DragDropEffects.None : copy || Contains(e, FileNames)
                ? DragDropEffects.Copy : DragDropEffects.Move;
        }

        void Drop(object sender, DragEventArgs e) {
            if (!TryResolveDrop(e, out Control source, out int after))
                return;

            e.Handled = true;

            bool result = false;
            bool copy = e.KeyModifiers.HasFlag(App.ControlKey);

            foreach (string format in DropHandlers.Keys.Where(i => Contains(e, i))) {
                int slot = after;
                if (format != FileNames && DropHandlers[format] == DefaultDrop) {
                    List<ISelect> moving = (List<ISelect>)e.DataTransfer.TryGetValue(SelectionFormat(format));
                    slot = AdjustAfter(moving, Host.ItemParent, after, copy);
                }

                if (result = DropHandlers[format].Invoke(source, Host.ItemParent, Host.Item, slot, format, e))
                    break;
            }
            
            e.DragEffects = !result ? DragDropEffects.None : copy || Contains(e, FileNames)
                ? DragDropEffects.Copy : DragDropEffects.Move;
        }

        public void Dispose() {
            foreach (Control control in Subscribed) {
                control.RemoveHandler(DragDrop.DragOverEvent, DragOver);
                control.RemoveHandler(DragDrop.DropEvent, Drop);
            }

            Subscribed = null;
            Host = null;
        }
        
        public class DragDropUndoEntry: PathUndoEntry<ISelectParent> {
            protected Path<ISelectParent> premove { get; private set; }

            protected virtual ISelectParent ResolvePremove() => premove.Resolve();

            bool copy;
            int count, before, before_pos, after, after_pos;

            protected override void UndoPath(params ISelectParent[] items) {
                if (copy)
                    for (int i = after + count; i > after; i--)
                        items[1].Remove(i);

                else Move(
                    Enumerable.Range(after_pos + 1, count).Select(i => items[1].IChildren[i]).ToList(),
                    items[0],
                    before_pos,
                    false,
                    out _
                );
            }

            protected override void RedoPath(params ISelectParent[] items) => Move(
                Enumerable.Range(before + 1, count).Select(i => items[0].IChildren[i]).ToList(),
                ResolvePremove(),
                after,
                copy,
                out _
            );
            
            public DragDropUndoEntry(ISelectParent sourceparent, Path<ISelectParent> premove, ISelectParent targetparent, bool copy, int count, int before, int after, int before_pos, int after_pos, string format)
            : base($"{format} {(copy? "Copied" : "Moved")}", sourceparent, targetparent) {
                this.premove = premove;
                this.copy = copy;
                this.count = count;
                this.before = before;
                this.after = after;
                this.before_pos = before_pos;
                this.after_pos = after_pos;
            }
        
            protected DragDropUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {
                premove = new Path<ISelectParent>(reader, version);
                copy = reader.ReadBoolean();
                count = reader.ReadInt32();
                before = reader.ReadInt32();
                before_pos = reader.ReadInt32();
                after = reader.ReadInt32();
                after_pos = reader.ReadInt32();
            }
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                premove.Encode(writer);
                writer.Write(copy);
                writer.Write(count);
                writer.Write(before);
                writer.Write(before_pos);
                writer.Write(after);
                writer.Write(after_pos);
            }
        }
    }
}

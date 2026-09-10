#if HEADLESS_AVALONIA
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Helpers;
using Apollo.Selection;
using Apollo.Windows;

namespace Apollo.Tests {
    internal static partial class Scenarios {
        static async Task ExercisePointerGestures(Project project, Window window) {
            var chain = project[0].Chain;
            var paint = Device.Create(typeof(Apollo.Devices.Paint), Apollo.Enums.PurposeType.Active, chain);
            var clear = Device.Create(typeof(Apollo.Devices.Clear), Apollo.Enums.PurposeType.Active, chain);
            chain.Add(paint);
            chain.Add(clear);
            window.Activate();
            window.Focus();
            await Settle();
            var leading = chain.Viewer.Get<Grid>("DropZoneBefore");
            var trailing = chain.Viewer.Get<Grid>("DropZoneAfter");
            using (var transfer = new DataTransfer()) {
                var item = new DataTransferItem();
                item.Set(DragDropManager.SelectionFormat("Device"), new System.Collections.Generic.List<ISelect> { paint });
                transfer.Add(item);
                window.DragDrop(Center(leading, window), RawDragEventType.DragOver, transfer, DragDropEffects.Move);
                window.DragDrop(Center(leading, window), RawDragEventType.Drop, transfer, DragDropEffects.Move);
                Check(ReferenceEquals(chain[0], paint) && chain.Count == 2, "drag-first-item-before-itself-is-noop");
            }

            async Task<Point> StartDrag(Device device) {
                await Task.Delay(550); // Distinguish a new gesture from double-click collapse.
                window.Focus();
                var point = Center(device.Viewer.Get<Grid>("Draggable"), window);
                window.MouseDown(point, MouseButton.Left);
                window.MouseMove(point + new Vector(10, 0), RawInputModifiers.LeftMouseButton);
                await Settle();
                Check(App.Dragging, "pointer-drag-starts-after-threshold");
                return point;
            }

            var originalCursor = new Cursor(StandardCursorType.Cross);
            window.Cursor = originalCursor;
            await StartDrag(paint);
            var end = Center(trailing, window);
            window.MouseMove(end, RawInputModifiers.LeftMouseButton);
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Check(!App.Dragging, "escape-cancels-pointer-drag");
            window.MouseUp(end, MouseButton.Left);
            Check(chain.Count == 2 && ReferenceEquals(chain[0], paint), "cancelled-drag-does-not-reorder");
            Check(ReferenceEquals(window.Cursor, originalCursor), "drag-restores-window-cursor");
            window.Cursor = null;

            await StartDrag(paint);
            end = Center(trailing, window);
            window.MouseMove(end, RawInputModifiers.LeftMouseButton);
            window.MouseUp(end, MouseButton.Left);
            await Settle();
            Check(!App.Dragging && ReferenceEquals(chain[1], paint), "pointer-drag-reorders-and-releases-capture");
            project.Undo.Undo();
            Check(ReferenceEquals(chain[0], paint), "pointer-drag-undo");
            project.Undo.Redo();
            Check(ReferenceEquals(chain[1], paint), "pointer-drag-redo");

            await StartDrag(paint);
            end = Center(trailing, window);
            window.MouseMove(end, RawInputModifiers.LeftMouseButton | RawInputModifiers.Control);
            window.MouseUp(end, MouseButton.Left, RawInputModifiers.Control);
            await Settle();
            Check(chain.Count == 3 && chain[2] is Apollo.Devices.Paint && !ReferenceEquals(chain[2], paint), "pointer-control-drag-copies");
            project.Undo.Undo();
            Check(chain.Count == 2 && ReferenceEquals(chain[1], paint), "pointer-copy-undo");
            project.Undo.Redo();
            Check(chain.Count == 3 && chain[2] is Apollo.Devices.Paint, "pointer-copy-redo");
            var other = new Track(Apollo.Enums.PurposeType.Active);
            project.Insert(1, other);
            TrackWindow.Create(other, window);

            await Settle();
            await StartDrag(paint);
            // Headless PointToScreen ignores window positions. Make the destination
            // wider so its trailing drop zone lies beyond the source's hit region.
            other.Window.Width = 1400;
            other.Window.Activate();
            await Settle();
            var target = other.Chain.Viewer.Get<Grid>("DropZoneAfter");
            end = window.PointToClient(target.PointToScreen(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2)));
            window.MouseMove(end, RawInputModifiers.LeftMouseButton);
            window.MouseUp(end, MouseButton.Left);
            await Settle();
            Check(other.Chain.Count == 1 && ReferenceEquals(other.Chain[0], paint) && chain.Count == 2,
                "pointer-drag-between-track-windows");
            project.Undo.Undo();
            Check(other.Chain.Count == 0 && chain.Count == 3 && ReferenceEquals(chain[1], paint), "cross-window-drag-undo");
            project.Undo.Redo();
            Check(other.Chain.Count == 1 && chain.Count == 2, "cross-window-drag-redo");
            project.Undo.Undo();
            project.Remove(1);
            window.Activate();
            await Settle();

            await StartDrag(paint);
            end = Center(trailing, window);
            chain.Remove(paint.ParentIndex.Value);
            await Settle();
            Check(!App.Dragging, "detached-drag-source-cancels-without-crash");
            window.MouseUp(end, MouseButton.Left);
            Check(chain.Count == 2, "detached-drag-does-not-drop");

            while (chain.Count > 0) chain.Remove(chain.Count - 1);
            await Settle();

            var width = window.Width;
            var position = window.Position;
            foreach (var scale in new[] { 1.0, 1.5 }) {
                window.SetRenderScaling(scale);
                var edge = new Point(window.Bounds.Width - 2, window.Bounds.Height / 2);
                window.MouseDown(edge, MouseButton.Left);
                window.MouseMove(edge + new Vector(60, 0), RawInputModifiers.LeftMouseButton);
                Check(Math.Abs(window.Width - width - 60) < 1, "east-resize-at-scale-" + scale);
                window.MouseUp(edge + new Vector(60, 0), MouseButton.Left);
                window.Width = width;
                await Settle();

                edge = new Point(2, window.Bounds.Height / 2);
                window.MouseDown(edge, MouseButton.Left);
                window.MouseMove(edge + new Vector(40, 0), RawInputModifiers.LeftMouseButton);
                Check(Math.Abs(window.Width - width + 40) < 1 && window.Position.X == position.X + (int)(40 * scale),
                    "west-resize-keeps-opposite-edge-at-scale-" + scale);
                window.MouseUp(edge, MouseButton.Left);
                window.Width = width;
                window.Position = position;
                await Settle();
            }
            window.SetRenderScaling(1);
        }
    }
}
#endif
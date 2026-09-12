#if HEADLESS_AVALONIA
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Components;
using Apollo.Selection;
using Apollo.Helpers;

namespace Apollo.Tests {
    internal static partial class Scenarios {
        static RawInputModifiers ShortcutModifier => App.ControlKey == KeyModifiers.Meta
            ? RawInputModifiers.Meta : RawInputModifiers.Control;

        static void CheckTextKeyRouting(TextBox input, string name) {
            // macOS dispatches text input only when the preceding key was not
            // consumed. KeyTextInput alone bypasses this native-backend condition.
            foreach (var key in new[] { Key.A, Key.Space, Key.OemPlus, Key.OemMinus }) {
                var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key };
                input.RaiseEvent(args);
                Check(!args.Handled && input.IsFocused, name + "-text-key-reaches-native-input-" + key);
            }
        }
        static Point Center(Control control, Window window) =>
            control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window).Value;

        static async Task ExerciseInput(Project project, Window trackWindow) {
            await ExercisePointerGestures(project, trackWindow);
            var window = project.Window;
            var dial = window.Get<Dial>("Macro1");
            var display = dial.Get<TextBlock>("Display");
            var point = Center(display, window);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            var input = dial.Get<TextBox>("Input");
            Check(input.IsFocused, "dial-double-click-edit");
            Check(input.SelectedText == input.Text, "dial-edit-selects-value");
            CheckTextKeyRouting(input, "dial");
            window.KeyTextInput("42");
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            Check(project.GetMacro(1) == 42 && input.Opacity == 0, "dial-enter-commits");

            var originalName = project[0].Name;
            Operations.Rename(project, 0, 0);
            await Settle();
            CheckTextKeyRouting(project[0].Info.Input, "rename");
            window.KeyTextInput("Renamed track");
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            Check(project[0].Name == "Renamed track" && project[0].Info.Input.Opacity == 0, "rename-enter-commits");
            window.Focus();
            window.KeyPress(Key.Z, ShortcutModifier, PhysicalKey.Z, "z");
            window.KeyRelease(Key.Z, ShortcutModifier, PhysicalKey.Z, "z");
            Check(project[0].Name == originalName, "keyboard-undo");

            var chain = project[0].Chain;
            var paint = Device.Create(typeof(Apollo.Devices.Paint), Apollo.Enums.PurposeType.Active, chain);
            chain.Add(paint);
            var clear = Device.Create(typeof(Apollo.Devices.Clear), Apollo.Enums.PurposeType.Active, chain);
            chain.Add(clear);
            await Settle();
            var preset = new Copyable { Contents = new List<ISelect> { paint } };
            preset.StoreToClipboard();
            await Settle();
            var copied = await Copyable.DecodeClipboard();
            Check(copied?.Contents.Single() is Apollo.Devices.Paint, "clipboard-preset-roundtrip");
            ((Device)copied.Contents[0]).Dispose();
            await App.Clipboard.SetTextAsync("not an Apollo preset");
            Check(await Copyable.DecodeClipboard() == null, "invalid-clipboard-ignored");

            using var transfer = new DataTransfer();
            var item = new DataTransferItem();
            item.Set(DragDropManager.SelectionFormat("Device"), new List<ISelect> { paint });
            transfer.Add(item);
            var target = chain.Viewer.Get<Grid>("DropZoneAfter");
            var dropPoint = Center(target, trackWindow);
            trackWindow.DragDrop(dropPoint, RawDragEventType.DragEnter, transfer, DragDropEffects.Move);
            trackWindow.DragDrop(dropPoint, RawDragEventType.Drop, transfer, DragDropEffects.Move);
            Check(ReferenceEquals(chain[0], clear) && ReferenceEquals(chain[1], paint), "device-drag-reorders");
            project.Undo.Undo();
            Check(ReferenceEquals(chain[0], paint), "device-drag-undo");
            project.Undo.Redo();
            Check(ReferenceEquals(chain[1], paint), "device-drag-redo");
            chain.Remove(1);
            chain.Remove(0);
            await Capture(window, "09-edited-project");
            window.SetRenderScaling(1.5);
            Check(window.RenderScaling == 1.5 && window.Bounds.Width == 350, "dpi-change-keeps-logical-layout");
            await Capture(window, "10-project-150-percent");
            window.SetRenderScaling(1);
        }
    }
}
#endif

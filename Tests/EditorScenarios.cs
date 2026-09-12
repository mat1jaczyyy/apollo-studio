using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Apollo.Core;
using Apollo.Components;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Windows;
#if HEADLESS_AVALONIA
using Avalonia.Headless;
#endif

namespace Apollo.Tests {
    internal static partial class Scenarios {
        static async Task ExerciseEditors(Project project, Window trackWindow) {
            var chain = project[0].Chain;
            foreach (var type in typeof(Device).Assembly.GetTypes()
                .Where(t => t.Namespace == "Apollo.Devices" && !t.IsAbstract && typeof(Device).IsAssignableFrom(t))
                .OrderBy(t => t.Name)) {
                var device = Device.Create(type, PurposeType.Active, chain);
                chain.Insert(chain.Count, device);
                await Settle();
                Check(device.Viewer?.SpecificViewer != null, "editor-" + type.Name);
                await Capture(trackWindow, "device-" + type.Name);
                if (device is Pattern pattern) {
                    PatternWindow.Create(pattern, trackWindow);
                    var patternWindow = pattern.Window;
                    PatternWindow.Create(pattern, trackWindow);
                    Check(ReferenceEquals(patternWindow, pattern.Window), "pattern-singleton");
                    await Capture(patternWindow, "06-pattern");
                    patternWindow.Close();
                    Check(pattern.Window == null && project[0].Window != null, "pattern-close-keeps-track");
                }
                var preset = new Apollo.Helpers.Copyable { Contents = new System.Collections.Generic.List<Apollo.Selection.ISelect> { device } };
                var presetPath = System.IO.Path.Combine(output, type.Name + ".apdevice");
                await preset.StoreToFile(presetPath, trackWindow);
                var decoded = await Apollo.Helpers.Copyable.DecodeFile(new[] { presetPath }, trackWindow, typeof(Device));
                Check(decoded?.Contents.Single().GetType() == type, "preset-roundtrip-" + type.Name);
                ((Device)decoded.Contents[0]).Dispose();
                chain.Remove(chain.Count - 1);
            }
            project.Undo.AddAndExecute(new Project.BPMChangedUndoEntry(project.BPM, 180));
            Check(project.BPM == 180, "undoable-bpm-edit");
            project.Undo.Undo();
            Check(project.BPM == 150, "undo-bpm");
            project.Undo.Redo();
            Check(project.BPM == 180, "redo-bpm");
            UndoWindow.Create(trackWindow);
            await Capture(project.Undo.Window, "07-undo");
            project.Undo.Window.Close();
            Check(project.Undo.Window == null, "undo-window-close");
            var author = project.Window.Get<TextBox>("Author");
            typeof(ProjectWindow).GetMethod("BottomCollapse", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(project.Window, null);
            project.Window.Activate();
            await Settle();
            author.Focus();
            Check(author.IsFocused, "author-focus");
            #if HEADLESS_AVALONIA
            CheckTextKeyRouting(author, "author");
            author.SelectAll();
            project.Window.KeyTextInput("Typed author");
            Check(project.Author == "Typed author", "keyboard-text-input");
            var dial = project.Window.Get<Dial>("Macro1");
            var canvas = dial.Get<Canvas>("ArcCanvas");
            var point = canvas.TranslatePoint(new Point(canvas.Bounds.Width / 2, canvas.Bounds.Height / 2), project.Window).Value;
            project.Window.MouseDown(point, Avalonia.Input.MouseButton.Left);
            project.Window.MouseMove(point + new Vector(0, -30));
            project.Window.MouseUp(point + new Vector(0, -30), Avalonia.Input.MouseButton.Left);
            Check(project.GetMacro(1) > 1, "dial-pointer-drag");
            await ExerciseInput(project, trackWindow);
            #endif
        }
        #if HEADLESS_AVALONIA
        static void StartHeadless() {
            App.Args = Array.Empty<string>();
            Program.TimeSpent.Start();
            AppBuilder.Configure<App>().UseSkia().UseHarfBuzz()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
                .StartWithClassicDesktopLifetime(Array.Empty<string>());
            Program.TimeSpent.Stop();
        }
        static void ClickHeadless(Window window, Button button) {
            var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window).Value;
            window.MouseDown(point, Avalonia.Input.MouseButton.Left);
            window.MouseUp(point, Avalonia.Input.MouseButton.Left);
        }
        #endif
    }
}

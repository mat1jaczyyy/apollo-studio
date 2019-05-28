using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

using Apollo.Binary;
using Apollo.Components;
using Apollo.Core;
using Apollo.Elements;
using Apollo.Helpers;
using UndoEntry = Apollo.Helpers.UndoManager.UndoEntry;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class UndoWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void UpdateTopmost(bool value) => Topmost = value;

        Controls Contents;

        public void Contents_Insert(int index, UndoEntry entry) => Contents.Insert(index, new TextBlock() { Text = entry.Description });
        public void Contents_Remove(int index) => Contents.RemoveAt(index);
        
        public UndoWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            UpdateTopmost(Preferences.AlwaysOnTop);
            Preferences.AlwaysOnTopChanged += UpdateTopmost;

            Contents = this.Get<StackPanel>("Contents").Children;

            for (int i = 0; i < Program.Project.Undo.History.Count; i++)
                Contents_Insert(i, Program.Project.Undo.History[i]);
        }
        
        private void Loaded(object sender, EventArgs e) {
            
        }

        private void Unloaded(object sender, EventArgs e) {
            Program.Project.Undo.Window = null;

            Preferences.AlwaysOnTopChanged -= UpdateTopmost;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (Program.Project.HandleKey(this, e) || Program.Project.Undo.HandleKey(e)) {
                this.Focus();
                return;
            }

            if (e.Key == Key.Up) Program.Project.Undo.Undo();
            else if (e.Key == Key.Down) Program.Project.Undo.Redo();
        }

        private void Window_Focus(object sender, PointerPressedEventArgs e) => this.Focus();

        private void MoveWindow(object sender, PointerPressedEventArgs e) => BeginMoveDrag();
        
        private void Minimize() => WindowState = WindowState.Minimized;

        private void ResizeNorth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.North);

        private void ResizeSouth(object sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.South);

        public static void Create(Window owner) {
            if (Program.Project.Undo.Window == null) {
                Program.Project.Undo.Window = new UndoWindow() {Owner = owner};
                Program.Project.Undo.Window.Show();
                Program.Project.Undo.Window.Owner = null;
            } else {
                Program.Project.Undo.Window.WindowState = WindowState.Normal;
                Program.Project.Undo.Window.Activate();
            }
        }
    }
}
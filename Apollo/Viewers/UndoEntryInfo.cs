using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Core;
using UndoEntry = Apollo.Helpers.UndoManager.UndoEntry;

namespace Apollo.Viewers {
    public class UndoEntryInfo: UserControl {
        void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void SelectedEventHandler(int index);
        public event SelectedEventHandler Selected;

        UndoEntry _entry;

        public UndoEntryInfo() => new InvalidOperationException();

        public UndoEntryInfo(UndoEntry entry) {
            InitializeComponent();
            
            _entry = entry;

            this.Get<TextBlock>("Description").Text = _entry.Description;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            Selected = null;
            _entry = null;
        }

        void Click(object sender, PointerPressedEventArgs e) {
            PointerUpdateKind MouseButton = e.GetCurrentPoint(this).Properties.PointerUpdateKind;

            if (MouseButton == PointerUpdateKind.LeftButtonPressed) Selected?.Invoke(Program.Project.Undo.History.IndexOf(_entry));
        }
    }
}

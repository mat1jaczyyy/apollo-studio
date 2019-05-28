using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Core;
using UndoEntry = Apollo.Helpers.UndoManager.UndoEntry;

namespace Apollo.Viewers {
    public class UndoEntryInfo: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void SelectedEventHandler(int index);
        public event SelectedEventHandler Selected;

        UndoEntry _entry;

        public UndoEntryInfo(UndoEntry entry) {
            InitializeComponent();
            
            _entry = entry;

            this.Get<TextBlock>("Description").Text = _entry.Description;
        }

        private void Click(object sender, PointerPressedEventArgs e) => Selected?.Invoke(Program.Project.Undo.History.IndexOf(_entry));
    }
}

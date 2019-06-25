using Avalonia.Input;

using Apollo.Core;

namespace Apollo.Components {
    public class UndoClearButton: ClearButton {
        protected override void Click(PointerReleasedEventArgs e) => Program.Project.Undo.Clear();
    }
}

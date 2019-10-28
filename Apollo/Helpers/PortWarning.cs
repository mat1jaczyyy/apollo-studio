using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Windows;

namespace Apollo.Helpers {
    public class PortWarning {
        PortWarningType State = PortWarningType.None;
        string message = "";

        public PortWarning(string msg) => message = msg;

        public void Set() {
            if (State == PortWarningType.None) {
                State = PortWarningType.Show;

                if (Application.Current != null && App.MainWindow != null) DisplayWarning(null, true);
            }
        }

        public bool DisplayWarning(Window sender, bool dispatcher = false) {
            if (State != PortWarningType.Show) return false;

            State = PortWarningType.Done;

            Action DisplayMessage = async () => {
                await MessageWindow.Create(message, null, sender);

                Launchpad.DisplayWarnings(sender);
            };

            if (dispatcher) Dispatcher.UIThread.Post(DisplayMessage, DispatcherPriority.MinValue);
            else DisplayMessage.Invoke();

            return true;
        }
    }
}
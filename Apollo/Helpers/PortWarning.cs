using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Windows;

namespace Apollo.Helpers {
    public class PortWarning {
        public class Option {
            public readonly string action, url;

            public Option(string button, string location) {
                action = button;
                url = location;
            }
        }

        PortWarningType State = PortWarningType.None;
        string message;
        Option[] options;

        public PortWarning(string msg, params Option[] opts) {
            message = msg;
            options = opts;
        }

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
                string[] actions = options.Select(i => i.action).ToArray();
                int result;

                if ((result = Array.IndexOf(actions, await MessageWindow.Create(message, actions.Append("Ignore").ToArray(), sender))) != -1)
                    App.URL(options[result].url);

                Launchpad.DisplayWarnings(sender);
            };

            if (dispatcher) Dispatcher.UIThread.Post(DisplayMessage, DispatcherPriority.MinValue);
            else DisplayMessage.Invoke();

            return true;
        }
    }
}
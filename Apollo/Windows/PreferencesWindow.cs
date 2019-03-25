using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Newtonsoft.Json;

using Apollo.Core;
using Apollo.Elements;
using Apollo.Viewers;

namespace Apollo.Windows {
    public class PreferencesWindow: Window {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private CheckBox AlwaysOnTop, CenterTrackContents;

        public PreferencesWindow() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif
            
            Icon = new WindowIcon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Apollo.Resources.WindowIcon.png"));

            Program.PreferencesWindow = this;

            AlwaysOnTop = this.Get<CheckBox>("AlwaysOnTop");
            CenterTrackContents = this.Get<CheckBox>("CenterTrackContents");
        }

        private void MoveWindow(object sender, PointerPressedEventArgs e) {
            BeginMoveDrag();
        }

        private void AlwaysOnTop_Changed(object sender, EventArgs e) {
            
        }

        private void CenterTrackContents_Changed(object sender, EventArgs e) {
            
        }
    }
}
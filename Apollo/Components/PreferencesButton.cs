﻿using System;
using System.Collections.Generic;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.Components {
    public class PreferencesButton: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public PreferencesButton() {
            InitializeComponent();
        }

        private void Clicked(object sender, EventArgs e) {
            
        }
    }
}
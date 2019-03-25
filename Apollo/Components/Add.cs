﻿using System;
using System.Collections.Generic;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.Components {
    public class Add: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void DeviceAddedEventHandler(Type device);
        public event DeviceAddedEventHandler DeviceAdded;

        Canvas Icon;

        public Add() {
            InitializeComponent();

            Icon = this.Get<Canvas>("Icon");

            Icon.ContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(ContextMenu_Click));
        }

        private void ContextMenu_Click(object _, EventArgs e) {
            IInteractive sender = ((RoutedEventArgs)e).Source;

            if (sender.GetType() == typeof(MenuItem)) {
                string selected = ((MenuItem)sender).Header.ToString();
                DeviceAdded?.Invoke(Assembly.GetExecutingAssembly().GetType($"Apollo.Devices.{selected}"));
            }
        }

        private void Clicked(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left)
                Icon.ContextMenu.Open(Icon);

            e.Handled = true;
        }
    }
}

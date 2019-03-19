using System;
using System.Collections.Generic;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.Components {
    public class Add: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void DeviceAddedEventHandler(Type device);
        public event DeviceAddedEventHandler DeviceAdded;

        public Add() {
            InitializeComponent();

            Canvas AddCanvas = this.Get<Canvas>("AddCanvas");
            AddCanvas.ContextMenu.AddHandler(MenuItem.ClickEvent, new EventHandler(ContextMenu_Click));
        }

        private void ContextMenu_Click(object _, EventArgs e) {
            IInteractive sender = ((RoutedEventArgs)e).Source;

            if (sender.GetType() == typeof(MenuItem)) {
                string selected = ((MenuItem)sender).Header.ToString();
                DeviceAdded?.Invoke(Assembly.GetExecutingAssembly().GetType($"Apollo.Devices.{selected}"));
            }
        }
    }
}

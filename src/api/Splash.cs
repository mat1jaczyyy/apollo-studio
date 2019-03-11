﻿using System;
using System.Collections.Generic;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;

namespace api {
    public class Splash: Window {
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        public Splash() {
            InitializeComponent();
            #if DEBUG
                this.AttachDevTools();
            #endif

            this.Get<Image>("img").Source = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("api.Resources.SplashImage.png"));
            Icon = new WindowIcon(Assembly.GetExecutingAssembly().GetManifestResourceStream("api.Resources.WindowIcon.png"));
        }

        public void buttonNew_Click(object sender, RoutedEventArgs e) {
            Set.New();
            Close();
        }

        public async void buttonOpen_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog() {
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>() {
                    new FileDialogFilter() {
                        Extensions = new List<string>() {
                            "approj"
                        },
                        Name = "Apollo Project"
                    }
                },
                Title = "Open Project"
            };

            string[] result = await ofd.ShowAsync();
            if (result.Length > 0 && Set.Open(result[0])) {
                Close();
            }
        }
    }
}

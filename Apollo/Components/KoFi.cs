﻿using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Apollo.Core;

namespace Apollo.Components {
    public class KoFi: IconButton {
        public static readonly string URL = "https://ko-fi.com/mat1jaczyyy";

        void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override IBrush Fill {
            get => (IBrush)this.Resources["Brush"];
            set => this.Resources["Brush"] = value;
        }

        public KoFi() {
            InitializeComponent();

            base.MouseLeave(this, null);
        }

        protected override void Click(PointerReleasedEventArgs e) => App.URL(URL);
    }
}

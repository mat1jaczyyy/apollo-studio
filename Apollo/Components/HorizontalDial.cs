using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class HorizontalDial: Dial {
        protected override void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            ArcCanvas = this.Get<Canvas>("ArcCanvas");
            ArcBase = this.Get<Path>("ArcBase");
            Arc = this.Get<Path>("Arc");

            Display = this.Get<TextBlock>("Display");
            TitleText = this.Get<TextBlock>("Title");

            Input = this.Get<TextBox>("Input");
        }

        public override double Maximum {
            get => _max;
            set {
                if (_max != value) {
                    _max = value;

                    Input.Width = ((int)_max).ToString().Length * 25.0 / 3;
                    
                    Value = ToValue(RawValue);
                }
            }
        }

        public HorizontalDial() {}
    }
}

using System;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;

namespace Apollo.Components {
    public class HorizontalDial: Dial {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public override double Maximum {
            get => _max;
            set {
                if (_max != value) {
                    _max = value;

                    Input.Width = (double)((int)_max).ToString(CultureInfo.InvariantCulture).Length * 25 / 3;
                    
                    Value = ToValue(RawValue);
                }
            }
        }

        public HorizontalDial() {
            ArcCanvas = this.Get<Canvas>("ArcCanvas");
            ArcBase = this.Get<Path>("ArcBase");
            Arc = this.Get<Path>("Arc");

            Display = this.Get<TextBlock>("Display");
            TitleText = this.Get<TextBlock>("Title");

            Input = this.Get<TextBox>("Input");
            Input.GetObservable(TextBox.TextProperty).Subscribe(Input_Changed);

            DrawArcBase();
        }
    }
}

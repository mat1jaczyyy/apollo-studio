using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Viewers;

namespace Apollo.DeviceViewers {
    public class MultiViewer: UserControl {
        public static readonly string DeviceIdentifier = "multi";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Multi _multi;
        DeviceViewer _parent;
        Controls _root;
        Controls Contents;

        int? current;

        private void Contents_Insert(int index, Chain chain) {
            ChainInfo viewer = new ChainInfo(chain);
            viewer.ChainAdded += Chain_Insert;
            viewer.ChainRemoved += Chain_Remove;
            viewer.ChainExpanded += Expand;
            Contents.Insert(index + 1, viewer);
        }

        public MultiViewer(Multi multi, DeviceViewer parent) {
            InitializeComponent();

            _multi = multi;

            _parent = parent;
            _parent.Get<Grid>("Contents").Margin = new Thickness(0);
            _parent.Get<Border>("Border").CornerRadius = new CornerRadius(0, 5, 5, 0);

            _root = _parent.Get<StackPanel>("Root").Children;
            _root.Insert(0, new DeviceHead());
            _root.Insert(1, new ChainViewer(_multi.Preprocess) { Background = (IBrush)Application.Current.Styles.FindResource("ThemeControlDarkenBrush") });

            Contents = this.Get<StackPanel>("Contents").Children;
            
            for (int i = 0; i < _multi.Count; i++)
                Contents_Insert(i, _multi[i]);
            
            if (_multi.Count == 0) this.Get<ChainAdd>("ChainAdd").AlwaysShowing = true;
        }

        private void Expand(int? index) {
            if (current != null) {
                _root.RemoveAt(4);
                _root.RemoveAt(3);

                _parent.Get<Border>("Border").CornerRadius = new CornerRadius(0, 5, 5, 0);
                ((ChainInfo)Contents[current.Value + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Normal;

                if (index == current) {
                    current = null;
                    return;
                }
            }

            if (index != null) {
                _root.Insert(3, new ChainViewer(_multi[index.Value]) { Background = (IBrush)Application.Current.Styles.FindResource("ThemeControlDarkenBrush") });
                _root.Insert(4, new DeviceTail());

                _parent.Get<Border>("Border").CornerRadius = new CornerRadius(0);
                ((ChainInfo)Contents[index.Value + 1]).Get<TextBlock>("Name").FontWeight = FontWeight.Bold;
            }
            
            current = index;
        }

        private void Chain_Insert(int index) {
            Chain chain = new Chain();
            if (Preferences.AutoCreatePageFilter) chain.Add(new PageFilter());
            if (Preferences.AutoCreateKeyFilter) chain.Add(new KeyFilter());

            _multi.Insert(index, chain);
            Contents_Insert(index, _multi[index]);

            if (current != null && index <= current) current++;
            Expand(index);

            this.Get<ChainAdd>("ChainAdd").AlwaysShowing = false;
        }

        private void Chain_InsertStart() => Chain_Insert(0);

        private void Chain_Remove(int index) {
            if (current != null) {
                if (index < current) current--;
                else if (index == current) Expand(null);
            }

            Contents.RemoveAt(index + 1);
            _multi.Remove(index);

            if (_multi.Count == 0) this.Get<ChainAdd>("ChainAdd").AlwaysShowing = true;
        }
    }
}

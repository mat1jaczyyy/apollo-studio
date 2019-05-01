using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Elements;

namespace Apollo.Viewers {
    public class ChainInfo: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void ChainInfoEventHandler(int index);
        public event ChainInfoEventHandler ChainAdded;
        public event ChainInfoEventHandler ChainRemoved;

        public delegate void ChainExpandedEventHandler(int? index);
        public event ChainExpandedEventHandler ChainExpanded;

        Chain _chain;

        TextBlock NameText;
        public VerticalAdd ChainAdd;

        private void UpdateText(int index) => NameText.Text = $"Chain {index + 1}";
        
        public ChainInfo(Chain chain) {
            InitializeComponent();
            
            _chain = chain;

            NameText = this.Get<TextBlock>("Name");
            UpdateText(_chain.ParentIndex.Value);
            _chain.ParentIndexChanged += UpdateText;

            ChainAdd = this.Get<VerticalAdd>("ChainAdd");
        }
        
        private void Clicked(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) ChainExpanded?.Invoke(_chain.ParentIndex.Value);
        }

        private void Chain_Add() => ChainAdded?.Invoke(_chain.ParentIndex.Value + 1);
        private void Chain_Remove() => ChainRemoved?.Invoke(_chain.ParentIndex.Value);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Windows;

namespace Apollo.Viewers {
    public class ChainInfo: UserControl {
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        public delegate void ChainInfoEventHandler(int index);
        public event ChainInfoEventHandler ChainAdded;
        public event ChainInfoEventHandler ChainRemoved;

        public delegate void ChainExpandedEventHandler(int? index);
        public event ChainExpandedEventHandler ChainExpanded;

        Chain _chain;

        private void UpdateText(int index) => this.Get<TextBlock>("Name").Text = $"Chain {index + 1}";
        
        public ChainInfo(Chain chain) {
            InitializeComponent();
            
            _chain = chain;
            
            UpdateText(_chain.ParentIndex.Value);
            _chain.ParentIndexChanged += UpdateText;
        }
        
        private void Clicked(object sender, PointerReleasedEventArgs e) {
            if (e.MouseButton == MouseButton.Left) ChainExpanded?.Invoke(_chain.ParentIndex.Value);
        }

        private void Chain_Add() => ChainAdded?.Invoke(_chain.ParentIndex.Value + 1);
        private void Chain_Remove() => ChainRemoved?.Invoke(_chain.ParentIndex.Value);
    }
}

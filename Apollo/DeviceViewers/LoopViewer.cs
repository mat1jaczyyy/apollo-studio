using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class LoopViewer: UserControl {
        public static readonly string DeviceIdentifier = "loop";
        
        Loop _loop;
        
        Dial Rate, Gate, Repeats;
        CheckBox Hold;
        
        void InitializeComponent(){
            AvaloniaXamlLoader.Load(this);
            
            Rate = this.Get<Dial>("Rate");
            Gate = this.Get<Dial>("Gate");
            Repeats = this.Get<Dial>("Repeats");
            
            Hold = this.Get<CheckBox>("Hold");
        }
        
        public LoopViewer() => new InvalidOperationException();

        public LoopViewer(Loop loop) {
            InitializeComponent();
            
            _loop = loop;
            
            Rate.UsingSteps = _loop.Rate.Mode;
            Rate.Length = _loop.Rate.Length;
            Rate.RawValue = _loop.Rate.Free;
            
            Gate.RawValue = _loop.Gate * 100;
            
            Repeats.RawValue = _loop.Repeats;
            Repeats.Enabled = !_loop.Hold;
            
            Hold.IsChecked = _loop.Hold;
        }
        
        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _loop = null;

        void Rate_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_loop);

                Program.Project.Undo.Add($"Loop Rate Changed to {r}{Rate.Unit}", () => {
                    Track.TraversePath<Loop>(path).Rate.Free = u;
                }, () => {
                    Track.TraversePath<Loop>(path).Rate.Free = r;
                });
            }

            _loop.Rate.Free = (int)value;
        }
        
        public void SetRateValue(int value) => Rate.RawValue = value;
        
        void Rate_StepChanged(int value, int? old) {
            if (old != null && old != value) {
                int u = old.Value;
                int r = value;
                List<int> path = Track.GetPath(_loop);

                Program.Project.Undo.Add($"Loop Rate Changed to {Length.Steps[r]}", () => {
                    Track.TraversePath<Loop>(path).Rate.Length.Step = u;
                }, () => {
                    Track.TraversePath<Loop>(path).Rate.Length.Step = r;
                });
            }
        }
        
        public void SetRateStep(Length rate) => Rate.Length = rate;
        
        void Rate_ModeChanged(bool value, bool? old) {
            if (old != null && old != value) {
                bool u = old.Value;
                bool r = value;
                List<int> path = Track.GetPath(_loop);

                Program.Project.Undo.Add($"Loop Rate Switched to {(r? "Steps" : "Free")}", () => {
                    Track.TraversePath<Loop>(path).Rate.Mode = u;
                }, () => {
                    Track.TraversePath<Loop>(path).Rate.Mode = r;
                });
            }

            _loop.Rate.Mode = value;
        }
        
        void Hold_Changed(object sender, RoutedEventArgs e) {
            bool value = Hold.IsChecked.Value;

            if (_loop.Hold != value) {
                bool u = _loop.Hold;
                bool r = value;
                List<int> path = Track.GetPath(_loop);

                Program.Project.Undo.Add($"Loop Hold Changed to {(r? "Enabled" : "Disabled")}", () => {
                    Track.TraversePath<Loop>(path).Hold = u;
                }, () => {
                    Track.TraversePath<Loop>(path).Hold = r;
                });

                _loop.Hold = value;
            }
        }
        
        public void SetHold(bool hold){
            Hold.IsChecked = hold;
            Repeats.Enabled = !hold;
        }
        
        public void SetMode(bool mode) => Rate.UsingSteps = mode;
        
        void Gate_Changed(Dial sender, double value, double? old){
            if (old != null && old != value) {
                double u = old.Value / 100;
                double r = value / 100;
                List<int> path = Track.GetPath(_loop);

                Program.Project.Undo.Add($"Loop Gate Changed to {value}{Gate.Unit}", () => {
                    Track.TraversePath<Loop>(path).Gate = u;
                }, () => {
                    Track.TraversePath<Loop>(path).Gate = r;
                });
            }

            _loop.Gate = value / 100;
        }
        
        public void SetGate(double gate) => Gate.RawValue = gate * 100;
        
        void Repeats_Changed(Dial sender, double value, double? old){
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_loop);

                Program.Project.Undo.Add($"Loop Repeats Changed to {r}{Repeats.Unit}", () => {
                    Track.TraversePath<Loop>(path).Repeats = u;
                }, () => {
                    Track.TraversePath<Loop>(path).Repeats = r;
                });
            }

            _loop.Repeats = (int)value;
        }
        public void SetRepeats(int repeats) => Repeats.RawValue = repeats;
    
        
    
    }   
}
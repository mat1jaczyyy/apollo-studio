using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;
using Avalonia.Interactivity;

namespace Apollo.DeviceViewers {
    public class LoopViewer: UserControl {
        public static readonly string DeviceIdentifier = "loop";
        
        Loop _loop;
        
        Dial Duration, Gate, Repeats;
        CheckBox Hold;
        
        void InitializeComponent(){
            AvaloniaXamlLoader.Load(this);
            
            Duration = this.Get<Dial>("Duration");
            Gate = this.Get<Dial>("Gate");
            Repeats = this.Get<Dial>("Repeats");
            
            Hold = this.Get<CheckBox>("Hold");
        }
        
        public LoopViewer() => new InvalidOperationException();

        public LoopViewer(Loop loop) {
            InitializeComponent();
            
            _loop = loop;
            
            Duration.UsingSteps = _loop.Duration.Mode;
            Duration.Length = _loop.Duration.Length;
            Duration.RawValue = _loop.Duration.Free;
            
            Gate.RawValue = _loop.Gate * 100;
            
            Repeats.RawValue = _loop.Repeats;
        }
        
        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _loop = null;

        void Duration_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_loop);

                Program.Project.Undo.Add($"Loop Duration Changed to {r}{Duration.Unit}", () => {
                    Track.TraversePath<Loop>(path).Duration.Free = u;
                }, () => {
                    Track.TraversePath<Loop>(path).Duration.Free = r;
                });
            }

            _loop.Duration.Free = (int)value;
        }
        
        public void SetDurationValue(int value) => Duration.RawValue = value;
        
        void Duration_StepChanged(int value, int? old) {
            if (old != null && old != value) {
                int u = old.Value;
                int r = value;
                List<int> path = Track.GetPath(_loop);

                Program.Project.Undo.Add($"Loop Duration Changed to {Length.Steps[r]}", () => {
                    Track.TraversePath<Loop>(path).Duration.Length.Step = u;
                }, () => {
                    Track.TraversePath<Loop>(path).Duration.Length.Step = r;
                });
            }
        }
        
        public void SetDurationStep(Length duration) => Duration.Length = duration;
        
        void Duration_ModeChanged(bool value, bool? old) {
            if (old != null && old != value) {
                bool u = old.Value;
                bool r = value;
                List<int> path = Track.GetPath(_loop);

                Program.Project.Undo.Add($"Loop Duration Switched to {(r? "Steps" : "Free")}", () => {
                    Track.TraversePath<Loop>(path).Duration.Mode = u;
                }, () => {
                    Track.TraversePath<Loop>(path).Duration.Mode = r;
                });
            }

            _loop.Duration.Mode = value;
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
        
        public void SetMode(bool mode) => Duration.UsingSteps = mode;
        
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
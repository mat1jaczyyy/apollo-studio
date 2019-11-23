using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.Components {
    public class CopyOffset: UserControl {
        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
            
            _viewer = this.Get<MoveDial>("Offset");
            
            AngleDial = this.Get<Dial>("OffsetAngle");
        }

        public delegate void OffsetEventHandler(int index);
        public event OffsetEventHandler OffsetAdded;
        public event OffsetEventHandler OffsetRemoved;
        
        Offset _offset;
        Copy _copy;
        MoveDial _viewer;
        public Dial AngleDial;

        public CopyOffset() => throw new InvalidOperationException();

        public CopyOffset(Offset offset, Copy copy) {
            InitializeComponent();

            _offset = offset;
            _copy = copy;

            _viewer.X = _offset.X;
            _viewer.Y = _offset.Y;
            _viewer.Changed += Offset_Changed;
            
            AngleDial.RawValue = offset.Angle/Math.PI*180;
            
            AngleDial.Enabled = (copy.CopyMode == Enums.CopyType.Interpolate);
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            OffsetAdded = null;
            OffsetRemoved = null;

            _offset = null;
            _copy = null;
            _viewer = null;
        }

        void Offset_Add() => OffsetAdded?.Invoke(_copy.Offsets.IndexOf(_offset) + 1);

        void Offset_Remove() => OffsetRemoved?.Invoke(_copy.Offsets.IndexOf(_offset));

        void Offset_Changed(int x, int y, int? old_x, int? old_y) {
            _offset.X = x;
            _offset.Y = y;

            if (old_x != null && old_y != null) {
                int ux = old_x.Value;
                int uy = old_y.Value;
                int rx = x;
                int ry = y;
                int index = _copy.Offsets.IndexOf(_offset);

                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Offset {index + 1} Changed to {rx},{ry}", () => {
                    Copy copy = ((Copy)Track.TraversePath(path));
                    copy.Offsets[index].X = ux;
                    copy.Offsets[index].Y = uy;

                }, () => {
                    Copy copy = ((Copy)Track.TraversePath(path));
                    copy.Offsets[index].X = rx;
                    copy.Offsets[index].Y = ry;
                });
            }
        }

        public void SetOffset(int x, int y) {
            _viewer.X = x;
            _viewer.Y = y;
        }
    
        public void SetAngle(double angle){
            AngleDial.RawValue = angle;
        }
        
        public void Angle_Changed(Dial sender, double angle, double? old){
            int index = _copy.Offsets.IndexOf(_offset);
            
            if(old != null && old.Value != angle){
                List<int> path = Track.GetPath(_copy);
                
                double a = angle;
                double o = old.Value;

                Program.Project.Undo.Add($"Copy Angle {index + 1} Changed to {angle}°", () => {
                    Copy copy = ((Copy)Track.TraversePath(path));
                    copy.Offsets[index].Angle = old.Value/180*Math.PI;
                    SetAngle(o);

                }, () => {
                    Copy copy = ((Copy)Track.TraversePath(path));
                    copy.Offsets[index].Angle = angle/180*Math.PI;
                    SetAngle(a);
                });
            }
            
            _copy.Offsets[index].Angle = angle/180*Math.PI;
        }
    }
}

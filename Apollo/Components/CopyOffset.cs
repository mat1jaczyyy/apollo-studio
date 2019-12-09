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
            
            Angle = this.Get<Dial>("Angle");
        }

        public delegate void OffsetEventHandler(int index);
        public event OffsetEventHandler OffsetAdded;
        public event OffsetEventHandler OffsetRemoved;
        
        Offset _offset;
        Copy _copy;
        MoveDial _viewer;
        Dial Angle;

        public bool AngleEnabled {
            get => Angle.Enabled;
            set => Angle.Enabled = value;
        }

        public CopyOffset() => throw new InvalidOperationException();

        public CopyOffset(Offset offset, int angle, Copy copy) {
            InitializeComponent();

            _offset = offset;
            _copy = copy;

            _viewer.Update(_offset);
            _viewer.Changed += Offset_Changed;
            _viewer.AbsoluteChanged += Offset_AbsoluteChanged;
            _viewer.Switched += Offset_Switched;
            
            Angle.RawValue = angle;
            Angle.Enabled = (copy.CopyMode == Enums.CopyType.Interpolate);
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) {
            OffsetAdded = null;
            OffsetRemoved = null;

            _viewer.Changed -= Offset_Changed;
            _viewer.AbsoluteChanged -= Offset_AbsoluteChanged;
            _viewer.Switched -= Offset_Switched;

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

                Program.Project.Undo.Add($"Copy Offset {index + 1} Relative Changed to {rx},{ry}", () => {
                    Copy copy = Track.TraversePath<Copy>(path);
                    copy.Offsets[index].X = ux;
                    copy.Offsets[index].Y = uy;

                }, () => {
                    Copy copy = Track.TraversePath<Copy>(path);
                    copy.Offsets[index].X = rx;
                    copy.Offsets[index].Y = ry;
                });
            }
        }

        void Offset_AbsoluteChanged(int x, int y, int? old_x, int? old_y) {
            _offset.AbsoluteX = x;
            _offset.AbsoluteY = y;

            if (old_x != null && old_y != null) {
                int ux = old_x.Value;
                int uy = old_y.Value;
                int rx = x;
                int ry = y;
                int index = _copy.Offsets.IndexOf(_offset);

                List<int> path = Track.GetPath(_copy);

                Program.Project.Undo.Add($"Copy Offset {index + 1} Absolute Changed to {rx},{ry}", () => {
                    Copy copy = Track.TraversePath<Copy>(path);
                    copy.Offsets[index].AbsoluteX = ux;
                    copy.Offsets[index].AbsoluteY = uy;

                }, () => {
                    Copy copy = Track.TraversePath<Copy>(path);
                    copy.Offsets[index].AbsoluteX = rx;
                    copy.Offsets[index].AbsoluteY = ry;
                });
            }
        }

        void Offset_Switched() {
            bool u = _offset.IsAbsolute;
            bool r = !_offset.IsAbsolute;
            int index = _copy.Offsets.IndexOf(_offset);

            List<int> path = Track.GetPath(_copy);

            Program.Project.Undo.Add($"Copy Offset {index + 1} Switched to {(r? "Absolute" : "Relative")}", () => {
                Copy copy = Track.TraversePath<Copy>(path);
                copy.Offsets[index].IsAbsolute = u;

            }, () => {
                Copy copy = Track.TraversePath<Copy>(path);
                copy.Offsets[index].IsAbsolute = r;
            });

            _offset.IsAbsolute = !_offset.IsAbsolute;
        }

        public void SetOffset(Offset offset) => _viewer.Update(offset);
        
        public void Angle_Changed(Dial sender, double angle, double? old){
            int index = _copy.Offsets.IndexOf(_offset);
            
            if(old != null && old.Value != angle){
                List<int> path = Track.GetPath(_copy);
                
                int u = (int)old.Value;
                int r = (int)angle;

                Program.Project.Undo.Add($"Copy Angle {index + 1} Changed to {angle}{Angle.Unit}", () => {
                    Copy copy = Track.TraversePath<Copy>(path);
                    copy.SetAngle(index, u);

                }, () => {
                    Copy copy = Track.TraversePath<Copy>(path);
                    copy.SetAngle(index, r);
                });
            }
            
            _copy.SetAngle(index, (int)angle);
        }
    
        public void SetAngle(double angle) => Angle.RawValue = angle;
    }
}

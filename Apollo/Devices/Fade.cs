using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Apollo.Binary;
using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Elements.Purpose;
using Apollo.Enums;
using Apollo.Rendering;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class Fade: Device, IInitializable {
        public class FadeInfo {
            public Color Color;
            public double Time;
            public bool IsHold;

            public FadeInfo(Color color, double time, bool isHold = false) {
                Color = color;
                Time = time;
                IsHold = isHold;
            }

            public FadeInfo WithTime(double time) => new FadeInfo(Color, time, IsHold);
        }

        List<Color> _colors = new();
        List<double> _positions = new();
        List<FadeType> _types = new();
        List<FadeInfo> fade;

        public Color GetColor(int index) => _colors[index];
        public void SetColor(int index, Color color) {
            if (_colors[index] != color) {
                _colors[index] = color;
                Generate();

                if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetColor(index, _colors[index]);
            }
        }

        public double GetPosition(int index) => _positions[index];
        public void SetPosition(int index, double position) {
            if (_positions[index] != position) {
                _positions[index] = position;
                Generate();
                
                if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetPosition(index, _positions[index]);
            }
        }
        
        public FadeType GetFadeType(int index) => _types[index];
        public void SetFadeType(int index, FadeType type) {
            if (_types[index] != type) {
                _types[index] = type;
                Generate();
            }
        }

        static Dictionary<FadeType, Func<double, double>> TimeEasing = new() { 
            {FadeType.Fast, proportion => Math.Pow(proportion, 2)},
            {FadeType.Slow, proportion => 1 - Math.Pow(1 - proportion, 2)},
            {FadeType.Sharp, proportion => (proportion < 0.5)
                ? Math.Pow(proportion - 0.5, 2) * -2 + 0.5
                : Math.Pow(proportion - 0.5, 2) * 2 + 0.5
            },
            {FadeType.Smooth, proportion => (proportion < 0.5)
                ? Math.Pow(proportion, 2) * 2
                : Math.Pow(proportion - 1, 2) * -2 + 1
            }
        };

        double EaseTime(FadeType type, double start, double end, double val) {
            if (type == FadeType.Linear) return val;
            if (type == FadeType.Hold) return (start != val)? end - 0.1 : start;
            if (type == FadeType.Release) return start;

            double duration = end - start;
            return start + duration * TimeEasing[type].Invoke((val - start) / duration);
        }
    
        Time _time;
        public Time Time {
            get => _time;
            set {
                if (_time != null) {
                    _time.FreeChanged -= FreeChanged;
                    _time.ModeChanged -= ModeChanged;
                    _time.StepChanged -= StepChanged;
                }

                _time = value;

                if (_time != null) {
                    _time.Minimum = 1;
                    _time.Maximum = 30000;

                    _time.FreeChanged += FreeChanged;
                    _time.ModeChanged += ModeChanged;
                    _time.StepChanged += StepChanged;
                }
            }
        }

        void FreeChanged(int value) {
            Generate();
            if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetDurationValue(value);
        }

        void ModeChanged(bool value) {
            Generate();
            if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetMode(value);
        }

        void StepChanged(Length value) {
            Generate();
            if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetDurationStep(value);
        }

        double _gate;
        public double Gate {
            get => _gate;
            set {
                if (0.01 <= value && value <= 4) {
                    _gate = value;
                    Generate();
                    
                    if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetGate(Gate);
                }
            }
        }

        FadePlaybackType _mode;
        public FadePlaybackType PlayMode {
            get => _mode;
            set {
                _mode = value;

                if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).SetPlaybackMode(PlayMode);
            }
        }

        public delegate void GeneratedEventHandler(List<FadeInfo> points);
        public event GeneratedEventHandler Generated;

        public void Generate() => Generate(Preferences.FPSLimit);

        void Generate(int fps) {
            double frameTime = 1000.0 / fps;

            if (_colors.Count < 2 || _positions.Count < 2) return;
            if (_types.Count < _colors.Count - 1) return;

            List<Color> _steps = new();
            List<int> _counts = new();
            List<int> _cutoffs = new() {0};

            for (int i = 0; i < _colors.Count - 1; i++) {
                int max = new [] {
                    Math.Abs(_colors[i].Red - _colors[i + 1].Red),
                    Math.Abs(_colors[i].Green - _colors[i + 1].Green),
                    Math.Abs(_colors[i].Blue - _colors[i + 1].Blue),
                    1
                }.Max();

                if(_types[i] == FadeType.Hold) {
                    _steps.Add(_colors[i]);
                    _counts.Add(1);
                    _cutoffs.Add(1 + _cutoffs.Last());

                } else {
                    for (double k = 0; k < max; k++) {
                        double factor = k / max;
                        _steps.Add(new Color(
                            (byte)(_colors[i].Red + (_colors[i + 1].Red - _colors[i].Red) * factor),
                            (byte)(_colors[i].Green + (_colors[i + 1].Green - _colors[i].Green) * factor),
                            (byte)(_colors[i].Blue + (_colors[i + 1].Blue - _colors[i].Blue) * factor)
                        ));
                    }

                    _counts.Add(max);
                    _cutoffs.Add(max + _cutoffs.Last());
                }
            }

            _steps.Add(_colors.Last());

            if (_steps.Last().Lit) {
                _cutoffs[_cutoffs.Count - 1]++;
                _counts[_counts.Count - 1]++;
            
                _steps.Add(new Color(0));
                _counts.Add(1);
                _cutoffs.Add(1 + _cutoffs.Last());
            }

            List<FadeInfo> fullFade = new() {
                new FadeInfo(_steps[0], 0, _types[0] == FadeType.Hold)
            };

            int j = 0;
            for (int i = 1; i < _steps.Count; i++) {
                if (_cutoffs[j + 1] == i) j++;
                
                if (j < _colors.Count - 1) {
                    double prevTime = (j != 0)? _positions[j] * _time * _gate : 0;
                    double currTime = (_positions[j] + (_positions[j + 1] - _positions[j]) * (i - _cutoffs[j]) / _counts[j]) * _time * _gate;
                    double nextTime = _positions[j + 1] * _time * _gate;
                    
                    double time = EaseTime(_types[j], prevTime, nextTime, currTime);
                    
                    fullFade.Add(new FadeInfo(_steps[i], time, _types[j] == FadeType.Hold));
                }
            }
            
            if (fade != null) fade.Clear();
            else fade = new List<FadeInfo>();

            fade.Add(fullFade.First());

            for (int i = 1; i < fullFade.Count; i++) {
                double cutoff = fade.Last().Time + frameTime;

                if (cutoff < fullFade[i].Time)
                    fade.Add(fullFade[i]);
                    
                else if (fade.Last().Time + 2 * frameTime <= ((i < fullFade.Count - 1)? fullFade[i + 1].Time : _time * _gate))
                    fade.Add(fullFade[i].WithTime(cutoff));

                else if (i == fullFade.Count - 1 && fullFade[i].Color.Lit)
                    fade.Add(fullFade[i]);
            }

            fade.Add(new FadeInfo(_steps.Last(), _time * _gate));
            
            Generated?.Invoke(fullFade);
        }

        public int Count => _colors.Count;

        protected override object[] CloneParameters(PurposeType purpose)
            => new object[] { _time.Clone(), _gate, PlayMode, _colors.Select(i => i.Clone()).ToList(), _positions.ToList(), _types.ToList(), Type.Missing };

        public void Insert(int index, Color color, double position, FadeType type) {
            _colors.Insert(index, color);
            _positions.Insert(index, position);
            _types.Insert(index, type);

            if (Viewer?.SpecificViewer != null) {
                FadeViewer SpecificViewer = ((FadeViewer)Viewer.SpecificViewer);
                SpecificViewer.Contents_Insert(index, _colors[index]);

                SpecificViewer.Expand(index);
            }

            Generate();
        }

        public void Remove(int index) {
            _colors.RemoveAt(index);
            _positions.RemoveAt(index);
            if (index < _types.Count) _types.RemoveAt(index);

            if (Viewer?.SpecificViewer != null) ((FadeViewer)Viewer.SpecificViewer).Contents_Remove(index);

            Generate();
        }

        public int? Expanded;

        public Fade(Time time = null, double gate = 1, FadePlaybackType playmode = FadePlaybackType.Mono, List<Color> colors = null, List<double> positions = null, List<FadeType> types = null, int? expanded = null) : base("fade") {
            Time = time?? new Time();
            Gate = gate;
            PlayMode = playmode;

            _colors = colors?? new List<Color>() {new Color(), new Color(0)};
            _positions = positions?? new List<double>() {0, 1};
            _types = types?? new List<FadeType>() {FadeType.Linear};
            Expanded = expanded;

            (this as IInitializable).Initialize();
        }

        public void Initialized() {
            if (Purpose != PurposeType.Active && Purpose != PurposeType.Unrelated) return;

            Generate();

            Preferences.FPSLimitChanged += Generate;

            if (Purpose != PurposeType.Active) return;

            if (Program.Project != null)
                Program.Project.BPMChanged += Generate;
        }

        ConcurrentDictionary<Signal, Signal> buffer = new();

        public override void MIDIProcess(List<Signal> n)
            => InvokeExit(n.SelectMany(i => {
                Signal k = i.With(color: new Color(0));
                Signal v = i.Clone();

                Signal p = buffer.TryGetValue(k, out p)? p : null;

                if (!(PlayMode == FadePlaybackType.Mono && !i.Color.Lit && buffer.ContainsKey(k) && (buffer[k]?.Color.Lit?? false)))
                    buffer[k] = v;

                if (i.Color.Lit) {
                    double start = Heaven.Time;
                    int index = 0;
                    
                    void Next() {
                        if (!object.ReferenceEquals(buffer[k], v)) {
                            if (PlayMode == FadePlaybackType.Mono && !buffer[k].Color.Lit)
                                v = buffer[k];
                            
                            else return;
                        }

                        if (++index == fade.Count - 1 && PlayMode == FadePlaybackType.Loop)
                            index = 0;
                        
                        if (index < fade.Count) {
                            if (index < fade.Count - 1) Schedule(Next, start += fade[index + 1].Time - fade[index].Time);

                            InvokeExit(new List<Signal>() {i.With(color: fade[index].Color.Clone())});
                        }
                    };

                    if (fade == null)
                        throw new Exception("Fade was never initialized");

                    Schedule(Next, start += fade[1].Time - fade[0].Time);

                    return new [] {i.With(color: fade[0].Color.Clone())};
                
                } else if (PlayMode == FadePlaybackType.Loop && !(p is null)) {
                    buffer[k] = null;

                    return new [] {k};
                }

                return Enumerable.Empty<Signal>();
            }).ToList());

        protected override void Stopped() => buffer.Clear();

        public override void Dispose() {
            if (Disposed) return;

            Generated = null;
            Preferences.FPSLimitChanged -= Generate;

            if (Purpose == PurposeType.Active && Program.Project != null)
                Program.Project.BPMChanged -= Generate;

            Time.Dispose();
            base.Dispose();
        }

        public class ThumbInsertUndoEntry: PathUndoEntry<Fade> {
            int index;
            Color thumbColor;
            double pos;
            FadeType type;
            
            protected override void UndoPath(params Fade[] items) => items[0].Remove(index);
            protected override void RedoPath(params Fade[] items) => items[0].Insert(index, thumbColor, pos, type);
            
            public ThumbInsertUndoEntry(Fade fade, int index, Color thumbColor, double pos, FadeType type)
            : base($"Fade Color {index + 1} Inserted", fade) {
                this.index = index;
                this.thumbColor = thumbColor;
                this.pos = pos;
                this.type = type;
            }
            
            ThumbInsertUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {
                index = reader.ReadInt32();
                thumbColor = Decoder.Decode<Color>(reader, version, PurposeType.Passive);
                pos = reader.ReadDouble();
                type = (FadeType)reader.ReadInt32();
            }
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                writer.Write(index);
                Encoder.Encode(writer, thumbColor);
                writer.Write(pos);
                writer.Write((int)type);
            }
        }
        
        public class ThumbRemoveUndoEntry: PathUndoEntry<Fade> {
            int index;
            Color uc;
            double up;
            FadeType ut;
            
            protected override void UndoPath(params Fade[] items) => items[0].Insert(index, uc, up, ut);
            protected override void RedoPath(params Fade[] items) => items[0].Remove(index);
            
            public ThumbRemoveUndoEntry(Fade fade, int index)
            : base($"Fade Color {index + 1} Removed", fade) {
                this.index = index;
                
                uc = fade.GetColor(index);
                up = fade.GetPosition(index);
                ut = fade.GetFadeType(index);
            }
            
            ThumbRemoveUndoEntry(BinaryReader reader, int version): base(reader, version){
                index = reader.ReadInt32();
                uc = Decoder.Decode<Color>(reader, version, PurposeType.Passive);
                up = reader.ReadDouble();
                ut = (FadeType)reader.ReadInt32();
            }
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                writer.Write(index);
                Encoder.Encode(writer, uc);
                writer.Write(up);
                writer.Write((int)ut);
            }
        }
        
        public class ThumbTypeUndoEntry: SimpleIndexPathUndoEntry<Fade, FadeType> {
            protected override void Action(Fade item, int index, FadeType element) => item.SetFadeType(index, element);
            
            public ThumbTypeUndoEntry(Fade fade, int index, FadeType r)
            : base($"Fade Type {index + 1} Changed to {r.ToString()}", fade, index, fade.GetFadeType(index), r) {}
            
            ThumbTypeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class ThumbMoveUndoEntry: SimpleIndexPathUndoEntry<Fade, double> {
            protected override void Action(Fade item, int index, double element) => item.SetPosition(index, element);
            
            public ThumbMoveUndoEntry(Fade fade, int index, double u, double r)
            : base($"Fade Color {index + 1} Moved", fade, index, u, r) {}
            
            ThumbMoveUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class ColorUndoEntry: SimpleIndexPathUndoEntry<Fade, Color> {
            protected override void Action(Fade item, int index, Color element) => item.SetColor(index, element.Clone());
            
            public ColorUndoEntry(Fade fade, int index, Color u, Color r)
            : base($"Fade Color {index + 1} Changed to {r.ToHex()}", fade, index, u, r) {}
            
            ColorUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class DurationUndoEntry: SimplePathUndoEntry<Fade, int> {
            protected override void Action(Fade item, int element) => item.Time.Free = element;
            
            public DurationUndoEntry(Fade fade, int u, int r)
            : base($"Fade Duration Changed to {r}ms", fade, u, r) {}
            
            DurationUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class DurationModeUndoEntry: SimplePathUndoEntry<Fade, bool> {
            protected override void Action(Fade item, bool element) => item.Time.Mode = element;
            
            public DurationModeUndoEntry(Fade fade, bool u, bool r)
            : base($"Fade Duration Switched to {(r? "Steps" : "Free")}", fade, u, r) {}
            
            DurationModeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class DurationStepUndoEntry: SimplePathUndoEntry<Fade, int> {
            protected override void Action(Fade item, int element) => item.Time.Length.Step = element;
            
            public DurationStepUndoEntry(Fade fade, int u, int r)
            : base($"Fade Duration Changed to {Length.Steps[r]}", fade, u, r) {}
            
            DurationStepUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class GateUndoEntry: SimplePathUndoEntry<Fade, double> {
            protected override void Action(Fade item, double element) => item.Gate = element;
            
            public GateUndoEntry(Fade fade, double u, double r)
            : base($"Fade Gate Changed to {r}%", fade, u / 100, r / 100) {}
            
            GateUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class PlaybackModeUndoEntry: SimplePathUndoEntry<Fade, FadePlaybackType> {
            protected override void Action(Fade item, FadePlaybackType element) => item.PlayMode = element;
            
            public PlaybackModeUndoEntry(Fade fade, FadePlaybackType u, FadePlaybackType r)
            : base($"Fade Playback Mode Changed to {r.ToString()}", fade, u, r) {}
            
            PlaybackModeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class ReverseUndoEntry: SymmetricPathUndoEntry<Fade> {
            protected override void Action(Fade item) {
                List<Color> colors = Enumerable.Range(0, item.Count).Select(i => item.GetColor(i)).ToList();
                List<double> positions = Enumerable.Range(0, item.Count).Select(i => 1 - item.GetPosition(i)).ToList();
                
                for (int i = 0; i < item.Count; i++) {
                    item.SetColor(i, colors[item.Count - i - 1]);
                    item.SetPosition(i, positions[item.Count - i - 1]);
                }
                
                List<FadeType> fadetypes = Enumerable.Range(0, item.Count - 1).Select(i => item.GetFadeType(i)).ToList();
                
                for (int i = 0; i < item.Count - 1; i++)
                    item.SetFadeType(i, fadetypes[item.Count - i - 2].Opposite());

                int? expanded = item.Count - item.Expanded - 1;
                if (expanded != item.Expanded && item.Viewer?.SpecificViewer != null) ((FadeViewer)item.Viewer.SpecificViewer).Expand(expanded);
            }
            
            public ReverseUndoEntry(Fade fade)
            : base("Fade Reversed", fade) {}
            
            ReverseUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class EqualizeUndoEntry: SimplePathUndoEntry<Fade, double[]> {
            protected override void Action(Fade item, double[] element) {
                for (int i = 1; i < item.Count - 1; i++)
                    item.SetPosition(i, element[i - 1]);
            }
            
            public EqualizeUndoEntry(Fade fade)
            : base("Fade Equalized", fade,
                Enumerable.Range(1, fade.Count - 2).Select(i => fade.GetPosition(i)).ToArray(),
                Enumerable.Range(1, fade.Count - 2).Select(i => (double)i / (fade.Count - 1)).ToArray()
            ) {}
            
            EqualizeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }

        public abstract class CutUndoEntry: SinglePathUndoEntry<Fade> {
            List<Color> colors;
            List<double> positions;
            List<FadeType> fadetypes;
            int index;

            protected override void Undo(Fade item) {
                while (item.Count > 0)
                    item.Remove(item.Count - 1);
                
                for (int i = 0; i < colors.Count; i++)
                    item.Insert(i, colors[i], positions[i], i < fadetypes.Count? fadetypes[i] : FadeType.Linear);
            }
            
            protected override void Redo(Fade item) => Redo(item, index);
            protected abstract void Redo(Fade item, int index);
            
            public CutUndoEntry(Fade fade, int index, string action)
            : base($"Fade {action} Here Applied To Color {index + 1}", fade) {
                this.index = index;
                
                colors = Enumerable.Range(0, fade.Count).Select(i => fade.GetColor(i)).ToList();
                positions = Enumerable.Range(0, fade.Count).Select(i => fade.GetPosition(i)).ToList();
                fadetypes = Enumerable.Range(0, fade.Count - 1).Select(i => fade.GetFadeType(i)).ToList();
            }
        
            protected CutUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {
                colors = Enumerable.Range(0, reader.ReadInt32()).Select(i => Decoder.Decode<Color>(reader, version, PurposeType.Passive)).ToList();
                positions = Enumerable.Range(0, reader.ReadInt32()).Select(i => reader.ReadDouble()).ToList();
                fadetypes = Enumerable.Range(0, reader.ReadInt32()).Select(i => (FadeType)reader.ReadInt32()).ToList();
                index = reader.ReadInt32();
            }
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                writer.Write(colors.Count);
                for (int i = 0; i < colors.Count; i++)
                    Encoder.Encode(writer, colors[i]);
                
                writer.Write(positions.Count);
                for (int i = 0; i < positions.Count; i++)
                     writer.Write(positions[i]);
                
                writer.Write(fadetypes.Count);
                for (int i = 0; i < fadetypes.Count; i++)
                    writer.Write((int)fadetypes[i]);
                
                writer.Write(index);
            }
        }

        public class StartHereUndoEntry: CutUndoEntry {
            protected override void Redo(Fade item, int index) {
                for (int i = index - 1; i >= 0; i--)
                    item.Remove(i);
                
                for (int i = item.Count - 1; i >= 0; i--)
                    item.SetPosition(i, (item.GetPosition(i) - item.GetPosition(0)) / (1 - item.GetPosition(0)));
            }
            
            public StartHereUndoEntry(Fade fade, int index)
            : base(fade, index, "Start") {}
            
            StartHereUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }

        public class EndHereUndoEntry: CutUndoEntry {
            protected override void Redo(Fade item, int index) {
                for (int i = item.Count - 1; i > index; i--)
                    item.Remove(i);
                
                for (int i = 0; i < item.Count; i++)
                    item.SetPosition(i, item.GetPosition(i) / item.GetPosition(index));
            }
            
            public EndHereUndoEntry(Fade fade, int index)
            : base(fade, index, "End") {}
            
            EndHereUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}
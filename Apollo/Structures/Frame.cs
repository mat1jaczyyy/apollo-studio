using System.Collections.Generic;
using System.Linq;

using Apollo.Components;
using Apollo.Devices;
using Apollo.Interfaces;

namespace Apollo.Structures {
    public class Frame: ISelect {
        Color[] _screen;
        public Color[] Screen {
            get => _screen;
            set {
                if (_screen == null || !_screen.SequenceEqual(value)) {
                    _screen = value;

                    Info?.Viewer?.Draw();
                    
                    Parent?.Window?.SetGrid(ParentIndex.Value, this);
                }
            }
        }

        public ISelectViewer IInfo {
            get => Info;
        }

        public ISelectParent IParent {
            get => Parent;
        }

        public int? IParentIndex {
            get => ParentIndex;
        }

        public FrameDisplay Info;
        public Pattern Parent;
        public int? ParentIndex;

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
                    _time.Minimum = 10;
                    _time.Maximum = 30000;

                    _time.FreeChanged += FreeChanged;
                    _time.ModeChanged += ModeChanged;
                    _time.StepChanged += StepChanged;

                    FreeChanged(_time.Free);
                    ModeChanged(_time.Mode);
                    StepChanged(_time.Length);
                }
            }
        }

        void FreeChanged(int value) {
            Parent?.Window?.SetDurationValue(ParentIndex.Value, Time.Free);
            if (Info != null) Info.Viewer.Time.Text = ToString();
        }

        void ModeChanged(bool value) {
            Parent?.Window?.SetDurationMode(ParentIndex.Value, Time.Mode);
            if (Info != null) Info.Viewer.Time.Text = ToString();
        }

        void StepChanged(Length value) {
            Parent?.Window?.SetDurationStep(ParentIndex.Value, Time.Length);
            if (Info != null) Info.Viewer.Time.Text = ToString();
        }

        public Frame Clone() => new Frame(Time.Clone(), (from i in Screen select i.Clone()).ToArray());

        public Frame(Time time = null, Color[] screen = null) {
            if (screen == null || screen.Length != 101) {
                screen = new Color[101];
                for (int i = 0; i < 101; i++) screen[i] = new Color(0);
            }

            Time = time?? new Time();
            Screen = screen;
        }

        public void Invert() => Screen = Screen.SkipLast(1).Reverse().Concat(Screen.TakeLast(1)).ToArray();

        public static bool Move(List<Frame> source, Pattern target, int position, bool copy = false) => (position == -1)
            ? Move(source, target, copy)
            : Move(source, target[position], copy);

        public static bool Move(List<Frame> source, Frame target, bool copy = false) {
            if (!copy && ((source[0].Parent != target.Parent && source[0].Parent.Count == source.Count) ||
                source.Contains(target) || (source[0].Parent == target.Parent && source[0].ParentIndex == target.ParentIndex + 1)))
                return false;
            
            List<Frame> moved = new List<Frame>();

            for (int i = 0; i < source.Count; i++) {
                if (!copy) source[i].Parent.Remove(source[i].ParentIndex.Value);

                moved.Add(copy? source[i].Clone() : source[i]);

                target.Parent.Insert(target.ParentIndex.Value + i + 1, moved.Last());
            }

            target.Parent.Window.Selection.Select(moved.First());
            target.Parent.Window.Selection.Select(moved.Last(), true);

            target.Parent.Window.Frame_Select(moved.Last().ParentIndex.Value);
            
            return true;
        }

        public static bool Move(List<Frame> source, Pattern target, bool copy = false) {
            if (!copy && ((source[0].Parent != target && source[0].Parent.Count == source.Count) || source[0] == target[0]))
                return false;
            
            List<Frame> moved = new List<Frame>();

            for (int i = 0; i < source.Count; i++) {
                if (!copy) source[i].Parent.Remove(source[i].ParentIndex.Value);

                moved.Add(copy? source[i].Clone() : source[i]);

                target.Insert(i, moved.Last());
            }

            target.Window.Selection.Select(moved.First());
            target.Window.Selection.Select(moved.Last(), true);

            target.Window.Frame_Select(moved.Last().ParentIndex.Value);
            
            return true;
        }

        public override string ToString() => (Parent.Infinite && ParentIndex.Value == Parent.Count - 1)? "Infinite" : Time.ToString();

        public void Dispose() {
            Time.Dispose();
            Info = null;
            Parent = null;
            ParentIndex = null;
        }
    }
}
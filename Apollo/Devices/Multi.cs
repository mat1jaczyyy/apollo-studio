using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Enums;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    //+ Heaven compatible
    public class Multi: Group {
        public delegate void MultiResetHandler();
        public static event MultiResetHandler Reset;

        public static void InvokeReset() => Reset?.Invoke();

        public Chain Preprocess;

        MultiType _mode;
        public MultiType Mode {
            get => _mode;
            set {
                _mode = value;

                if (SpecificViewer != null) ((MultiViewer)SpecificViewer).SetMode(Mode);
            }
        }

        int current = -1;
        ConcurrentDictionary<Signal, List<int>> buffer = new ConcurrentDictionary<Signal, List<int>>();

        Random RNG = new Random();

        protected override void Reroute() {
            if (Preprocess != null) {
                Preprocess.Parent = this;
                Preprocess.MIDIExit = PreprocessExit;
            }

            base.Reroute();
        }

        public override Device Clone() => new Multi(Preprocess.Clone(), Chains.Select(i => i.Clone()).ToList(), Expanded, Mode) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        void HandleReset() => current = -1;

        public Multi(Chain preprocess = null, List<Chain> init = null, int? expanded = null, MultiType mode = MultiType.Forward): base(init, expanded, "multi") {
            Preprocess = preprocess?? new Chain();

            Mode = mode;
            
            Reset += HandleReset;

            Reroute();
        }

        public override IEnumerable<Signal> MIDIProcess(IEnumerable<Signal> n) {
            /*Signal m = n.Clone();
            n.Color = new Color();

            if (!buffer.ContainsKey(n)) {
                if (!m.Color.Lit) return;

                List<int> target = new List<int>();

                if (Mode == MultiType.Forward) {
                    if (++current >= Chains.Count) current = 0;
                
                } else if (Mode == MultiType.Backward) {
                    if (--current < 0) current = Chains.Count - 1;
                
                } else if (Mode == MultiType.Random || current == -1)
                    current = RNG.Next(Chains.Count);
                
                else if (Mode == MultiType.RandomPlus) {
                    int old = current;
                    if (Chains.Count <= 1) current = 0;
                    else if ((current = RNG.Next(Chains.Count - 1)) >= old) current++;
                    
                } else if (Mode == MultiType.Key) {
                    for (int index = 0; index < Chains.Count; index++)
                        if (Chains[index].SecretMultiFilter[m.Index]) target.Add(index);
                }

                if (Mode != MultiType.Key) target.Add(current);

                m.MultiTarget.Push(buffer[n] = target);

            } else {
                m.MultiTarget.Push(buffer[n]);
                if (!m.Color.Lit) buffer.Remove(n, out _);
            }

            Preprocess.MIDIEnter(m);*/

            // TODO Implement
            return n;
        }

        void PreprocessExit(Signal n) {
            /*if (n is StopSignal) return;

            List<int> target = n.MultiTarget.Pop();
            
            if (Chains.Count == 0) InvokeExit(n);
            else {
                foreach (int i in target)
                Chains[i].MIDIEnter(n.Clone());
            }*/
        }
        
        protected override void Stop() {
            base.Stop();
            Preprocess.MIDIEnter(new StopSignal());
        }

        public override void Dispose() {
            if (Disposed) return;

            Reset -= HandleReset;

            Preprocess.Dispose();
            foreach (Chain chain in Chains) chain.Dispose();
            base.Dispose();
        }
        
        public class ModeUndoEntry: EnumSimplePathUndoEntry<Multi, MultiType> {
            protected override void Action(Multi item, MultiType element) => item.Mode = element;
            
            public ModeUndoEntry(Multi multi, MultiType u, MultiType r, IEnumerable source)
            : base("Multi Direction", multi, u, r, source) {}
            
            ModeUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
        
        public class FilterChangedUndoEntry: SimpleIndexPathUndoEntry<Multi, bool[]> {
            protected override void Action(Multi item, int index, bool[] element) => item[index].SecretMultiFilter = element.ToArray();
            
            public FilterChangedUndoEntry(Multi multi, int index, bool[] u)
            : base($"Multi Chain {index + 1} Filter Changed", multi, index, u.ToArray(), multi[index].SecretMultiFilter.ToArray()) {}
            
            FilterChangedUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }
    }
}
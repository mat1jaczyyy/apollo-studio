using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Elements.Launchpads;
using Apollo.Elements.Purpose;
using Apollo.Enums;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class Output: Device, IInitializable {
        int _target;
        public int Target {
            get => _target;
            set {
                if (_target != value) {
                    Program.Project.Tracks[_target].ParentIndexChanged -= IndexChanged;
                    Program.Project.Tracks[_target].Disposing -= IndexRemoved;
                    
                    _target = value;
                    Program.Project.Tracks[_target].ParentIndexChanged += IndexChanged;
                    Program.Project.Tracks[_target].Disposing += IndexRemoved;
                    
                    if (Viewer?.SpecificViewer != null) ((OutputViewer)Viewer.SpecificViewer).SetTarget(Target);
                }
            }
        }

        void IndexChanged(int value) {
            if (Track.Get(this)?.IsDisposing != false) return;

            _target = value;
            if (Viewer?.SpecificViewer != null) ((OutputViewer)Viewer.SpecificViewer).SetTarget(Target);
        }

        void IndexRemoved() {
            Track owner = Track.Get(this);

            if (Program.Project.IsDisposing || owner?.IsDisposing != false || owner?.ParentIndex == null) return;

            bool redoing = false;

            foreach (StackFrame call in new StackTrace().GetFrames()) {
                MethodBase method = call.GetMethod();
                if (redoing = method.DeclaringType == typeof(UndoManager) && method.Name == "Select") break;
            }

            if (!redoing)
                Program.Project.Undo.History.Last().AddPost(new IndexRemovedFix(this, Target));

            Target = owner.ParentIndex.Value;
            if (Viewer?.SpecificViewer != null) ((OutputViewer)Viewer.SpecificViewer).SetTarget(Target);
        }

        public Launchpad Launchpad => Program.Project.Tracks[Target].Launchpad;

        protected override object[] CloneParameters(PurposeType purpose)
            => new object[] { Target };

        public Output(int target = -1): base("output") {
            if (target < 0) {
                if (Parent == null)
                    throw new Exception("Output device was created fresh without a parent to help find target Track");

                target = Track.Get(this).ParentIndex.Value;
            }
            _target = target;

            if (Program.Project?.TrackOperation == true) Program.Project.TrackOperationFinished += (this as IInitializable).Initialize;
            else (this as IInitializable).Initialize();
        }

        public void Initialized() {
            if (Purpose != PurposeType.Active) return;

            Program.Project.Tracks[_target].ParentIndexChanged += IndexChanged;
            Program.Project.Tracks[_target].Disposing += IndexRemoved;
        }

        public override void MIDIProcess(List<Signal> n) {
            n.ForEach(i => i.Source = Launchpad);
            InvokeExit(n);
        }

        public override void Dispose() {
            if (Disposed) return;

            if (Purpose == PurposeType.Active) {
                if (Program.Project.Tracks.ElementAtOrDefault(_target) is Track track) {
                    track.ParentIndexChanged -= IndexChanged;
                    track.Disposing -= IndexRemoved;
                }
            }

            base.Dispose();
        }
        
        public class TargetUndoEntry: SimplePathUndoEntry<Output, int> {
            protected override void Action(Output item, int element) => item.Target = element;
            
            public TargetUndoEntry(Output output, int u, int r)
            : base($"Output Target Changed to {r}", output, u - 1, r - 1) {}
            
            TargetUndoEntry(BinaryReader reader, int version)
            : base(reader, version) {}
        }

        public class IndexRemovedFix: PathUndoEntry<Output> {
            int target;

            protected override void UndoPath(params Output[] items) => items[0].Target = target;

            protected override void OnRedo() {}

            public IndexRemovedFix(Output output, int target)
            : base("Output Index Removed Fix", output) => this.target = target;
            
            IndexRemovedFix(BinaryReader reader, int version)
            : base(reader, version) => target = reader.ReadInt32();
            
            public override void Encode(BinaryWriter writer) {
                base.Encode(writer);
                
                writer.Write(target);
            }
        }
    }
}
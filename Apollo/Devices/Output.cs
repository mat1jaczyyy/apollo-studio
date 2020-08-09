using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Structures;
using Apollo.Undo;

namespace Apollo.Devices {
    public class Output: Device {
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
            if (Program.Project.IsDisposing || Track.Get(this)?.IsDisposing != false) return;

            bool redoing = false;

            foreach (StackFrame call in new StackTrace().GetFrames()) {
                MethodBase method = call.GetMethod();
                if (redoing = method.DeclaringType == typeof(UndoManager) && method.Name == "Select") break;
            }

            if (!redoing)
                Program.Project.Undo.History.Last().AddPost(new IndexRemovedFix(this, Target));

            Target = Track.Get(this).ParentIndex.Value;
            if (Viewer?.SpecificViewer != null) ((OutputViewer)Viewer.SpecificViewer).SetTarget(Target);
        }

        public Launchpad Launchpad => Program.Project.Tracks[Target].Launchpad;

        public override Device Clone() => new Output(Target) {
            Collapsed = Collapsed,
            Enabled = Enabled
        };

        public Output(int target = -1): base("output") {
            if (target < 0) target = Track.Get(this).ParentIndex.Value;
            _target = target;

            if (Program.Project == null) Program.ProjectLoaded += Initialize;
            else if (Program.Project.TrackOperation) Program.Project.TrackOperationFinished += Initialize;
            else Initialize();
        }

        void Initialize() {
            Program.Project.Tracks[_target].ParentIndexChanged += IndexChanged;
            Program.Project.Tracks[_target].Disposing += IndexRemoved;
        }

        public override void MIDIProcess(List<Signal> n) {
            n.ForEach(i => i.Source = Launchpad);
            InvokeExit(n);
        }

        public override void Dispose() {
            if (Disposed) return;

            Program.Project.Tracks[_target].ParentIndexChanged -= IndexChanged;
            Program.Project.Tracks[_target].Disposing -= IndexRemoved;

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
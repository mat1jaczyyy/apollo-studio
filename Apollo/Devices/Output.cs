using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Apollo.Core;
using Apollo.DeviceViewers;
using Apollo.Elements;
using Apollo.Selection;
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

            if (!redoing) {
                int u = Target;
                Path<Output> path = new Path<Output>(this);

                //TODO Program.Project.Undo.History[Program.Project.Undo.History.Count - 1].Undo += () => path.Resolve().Target = u;
            }

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

        public override void MIDIProcess(Signal n) {
            n.Source = Launchpad;
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
            : base($"Output Target Changed to {r}", output, u, r) {}
        }
    }
}
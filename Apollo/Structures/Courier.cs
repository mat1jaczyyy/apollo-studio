using System;
using System.Timers;

namespace Apollo.Structures {
    public class Courier {
        Timer Timer = new Timer();
        Action<Courier> Handler;
        bool Expecting;

        public Courier(double time, Action<Courier> handler = null, bool start = true, bool repeat = false) {
            Timer.AutoReset = repeat;

            Timer.Interval = time;
            Handler = handler;

            Timer.Elapsed += Tick;

            if (start) {
                Expecting = true;
                Timer.Start();
            }
        }

        public void Restart() {
            Cancel();

            Expecting = true;
            Timer.Start();
        }

        protected virtual void Fire() => Handler?.Invoke(this);

        void Tick(object sender, EventArgs e) {
            if (!Expecting) return;

            Expecting = Timer?.AutoReset?? false;
            Fire();
        }

        public void Cancel() {
            Expecting = false;
            Timer?.Stop();
        }

        public virtual void Dispose() {
            Handler = null;

            Cancel();

            Timer?.Dispose();
            Timer = null;
        }
    }

    public class Courier<T>: Courier {
        public T Info { get; private set; }
        Action<Courier<T>, T> Handler;

        public Courier(double time, T info, Action<Courier<T>, T> handler, bool start = true, bool repeat = false)
        : base(time, start: start, repeat: repeat) {
            Info = info;
            Handler = handler;
        }

        protected override void Fire() {
            T info = Info;

            if (info != null) Handler?.Invoke(this, info);
        }

        public void Restart(T info) {
            Info = info;
            Restart();
        }

        public override void Dispose() {
            Handler = null;

            base.Dispose();
        }
    }
}
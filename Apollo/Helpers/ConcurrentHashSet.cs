using System.Collections.Generic;

namespace Apollo.Helpers {
    public class ConcurrentHashSet<T>: HashSet<T> {
        object locker = new object();

        public new bool Add(T item) {
            lock (locker)
                return base.Add(item);
        }

        public new bool Remove(T item) {
            lock (locker)
                return base.Remove(item);
        }

        public new void Clear() {
            lock (locker)
                base.Clear();
        }
    }
}
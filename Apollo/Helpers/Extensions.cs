using System.Collections.Concurrent;
using System.Linq;

namespace Apollo.Helpers {
    public static class Extensions {
        public static bool ContainsKeyReference<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key) {
            foreach (TKey dictKey in dictionary.Keys) 
                if (ReferenceEquals(dictKey, key)) return true;
            return false;
        }
    }
}
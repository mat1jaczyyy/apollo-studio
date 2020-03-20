using System.Collections.Generic;
using System.Linq;

using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;

namespace Apollo.Selection {
    public class Path<T> where T: ISelect {
        List<int> path = new List<int>();

        TRet Next<TRet>(ISelectParent current, int index) => (TRet)(
            (path[index] == -1)
                ? (ISelect)((Multi)current).Preprocess
                : (current.IChildren[path[index]] is Choke choke && (index != 0 || typeof(T) != typeof(Choke)))
                    ? (ISelect)choke.Chain
                    : current.IChildren[path[index]]
        );

        public T Resolve(int skip = 0) {
            ISelectParent item = Program.Project[path.Last()].Chain;

            if (path.Count - skip == 1) return (T)item;

            for (int i = path.Count - skip - 2; i > 0; i--)
                item = Next<ISelectParent>(item, i);

            return Next<T>(item, 0);
        }

        public Path(T item) {
            ISelect child = (ISelect)item;

            while (true) {
                if (item is Chain chain && (chain.Parent is Choke || chain.IRoot))
                    child = (ISelect)chain.Parent;

                path.Add(child.IParentIndex?? -1);

                if (child is Track) break;

                child = (ISelect)child.IParent;
            }
        }
    }
}
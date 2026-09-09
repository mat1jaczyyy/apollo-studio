using System;

namespace Apollo {
    internal static class ObservableExtensions {
        public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> changed)
            => source.Subscribe(new Avalonia.Reactive.AnonymousObserver<T>(changed));
    }
}

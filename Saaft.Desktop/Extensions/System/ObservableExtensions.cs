﻿using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Collections;

namespace System
{
    public static class ObservableExtensions
    {
        public static ReactiveCollection<T> ToReactiveCollection<T>(this IObservable<ReactiveCollectionAction<T>> actions)
            => new(actions);

        public static ReactiveReadOnlyValue<T?> ToReactiveReadOnlyValue<T>(this IObservable<T?> source)
            => ReactiveReadOnlyValue.Create(source);

        public static ReactiveReadOnlyValue<T> ToReactiveReadOnlyValue<T>(
                this    IObservable<T>  source,
                        T               initialValue)
            => ReactiveReadOnlyValue.Create(source, initialValue);
    }
}

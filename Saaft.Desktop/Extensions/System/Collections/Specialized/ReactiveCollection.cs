using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Collections;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace System.Collections.Specialized
{
    public class ReactiveCollection<T>
        : ReadOnlyCollection<T>,
            INotifyCollectionChanged
    {
        public ReactiveCollection(IObservable<ReactiveCollectionAction<T>> actions)
                : base(new List<T>())
            => _collectionChanged = actions
                .ObserveOn(DispatcherScheduler.Current)
                .Select(PerformAction)
                .Share()
                .ToEventPattern();

        event NotifyCollectionChangedEventHandler? INotifyCollectionChanged.CollectionChanged
        {
            add     => _collectionChanged.OnNext += value;
            remove  => _collectionChanged.OnNext -= value;
        }

        private EventPattern<object?, NotifyCollectionChangedEventArgs> PerformAction(ReactiveCollectionAction<T> action)
        {
            switch(action)
            {
                case ReactiveCollectionClearAction<T>:
                    Items.Clear();
                    return new(
                        sender: this,
                        e:      new(NotifyCollectionChangedAction.Reset));

                case ReactiveCollectionInsertAction<T> insertAction:
                    Items.Insert(insertAction.Index, insertAction.Item);
                    return new(
                        sender: this,
                        e:      new(NotifyCollectionChangedAction.Add, insertAction.Item, insertAction.Index));

                case ReactiveCollectionMoveAction<T> moveAction:
                    var movedItem = Items[moveAction.OldIndex];
                    Items.RemoveAt(moveAction.OldIndex);
                    Items.Insert(moveAction.NewIndex, movedItem);
                    return new(
                        sender: this,
                        e:      new(NotifyCollectionChangedAction.Move, movedItem, moveAction.OldIndex, moveAction.NewIndex));

                case ReactiveCollectionRemoveAction<T> removeAction:
                    var removedItem = Items[removeAction.Index];
                    Items.RemoveAt(removeAction.Index);
                    return new(
                        sender: this,
                        e:      new(NotifyCollectionChangedAction.Remove, removedItem, removeAction.Index));

                case ReactiveCollectionResetAction<T> resetAction:
                    Items.Clear();
                    foreach(var item in resetAction.Items)
                        Items.Add(item);
                    return new(
                        sender: this,
                        e:      new(NotifyCollectionChangedAction.Reset));
            }
            
            throw new InvalidOperationException($"Unsupported ReactiveCollectionAction type {action.GetType().Name}");
        }

        private readonly ICollectionChangedEventSource _collectionChanged;
    }
}

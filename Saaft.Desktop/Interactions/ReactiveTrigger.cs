using System;
using System.Windows;

using Microsoft.Xaml.Behaviors;

namespace Saaft.Desktop.Interactions
{
    public class ReactiveTrigger
        : TriggerBase<DependencyObject>
    {
        public object? Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
        public static readonly DependencyProperty SourceProperty
            = DependencyProperty.Register(
                nameof(Source),
                typeof(object),
                typeof(ReactiveTrigger),
                new PropertyMetadata()
                {
                    PropertyChangedCallback = (sender, e) => ((ReactiveTrigger)sender).OnSourceChanged(e)
                });

        private void OnSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            TryClearSubscription();

            AttachSubscription((dynamic)e.NewValue);
        }

        private void AttachSubscription<T>(IObservable<T> source)
            => _subscription = source.Subscribe(
                onCompleted:    TryClearSubscription,
                onError:        _ => TryClearSubscription(),
                onNext:         parameter => InvokeActions(parameter));

        #pragma warning disable CA1822 // Mark members as static
        // Fallback method for dynamic invocation
        private void AttachSubscription(object? _) { }
        #pragma warning restore CA1822 // Mark members as static

        private void TryClearSubscription()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private IDisposable? _subscription;
    }
}

using System.Windows.Input;
using System.Windows;

using Microsoft.Xaml.Behaviors;

namespace Saaft.Desktop.Interactions
{
    public class ClickAndDragBehavior
        : Behavior<UIElement>
    {
        public string? DataFormat
        {
            get => _dataFormat;
            set => _dataFormat = value;
        }

        public object? DataValue
        {
            get => GetValue(DataValueProperty);
            set => SetValue(DataValueProperty, value);
        }
        public static readonly DependencyProperty DataValueProperty
            = DependencyProperty.Register(
                nameof(DataValue),
                typeof(object),
                typeof(ClickAndDragBehavior));

        public DragDropEffects Effects
        {
            get => (DragDropEffects)GetValue(EffectsProperty);
            set => SetValue(EffectsProperty, value);
        }
        public static readonly DependencyProperty EffectsProperty
            = DependencyProperty.Register(
                nameof(Effects),
                typeof(DragDropEffects),
                typeof(ClickAndDragBehavior));

        protected override void OnAttached()
        {
            AssociatedObject.MouseLeave += OnMouseLeave;
            AssociatedObject.MouseMove  += OnMouseMove;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseLeave -= OnMouseLeave;
            AssociatedObject.MouseMove  -= OnMouseMove;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
            => _readyToStartDrag = false;

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                _readyToStartDrag = true;
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!_readyToStartDrag
                        || (_dataFormat is not string dataFormat)
                        || (DataValue is not object dataValue))
                    return;

                var dataObject = new DataObject();
                dataObject.SetData(dataFormat, dataValue);

                DragDrop.DoDragDrop(AssociatedObject, dataObject, Effects);

                _readyToStartDrag = false;
            }
        }

        private string? _dataFormat;
        private bool    _readyToStartDrag;
    }
}

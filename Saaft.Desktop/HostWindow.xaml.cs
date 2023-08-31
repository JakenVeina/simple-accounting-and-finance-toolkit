using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

using Saaft.Desktop.Prompts;

namespace Saaft.Desktop
{
    public partial class HostWindow
    {
        public HostWindow()
        {
            InitializeComponent();

            DataContextChanged += (sender, e) => 
            {
                _hostedModelSubscription?.Dispose();
                _hostedModelSubscription = null;

                if (e.NewValue is IHostedModel hostedModel)
                    Observable.FromEventPattern<CancelEventHandler, CancelEventArgs>(
                            addHandler:     handler => Closing += handler,
                            removeHandler:  handler => Closing -= handler)
                        .TakeUntil(hostedModel.Closed)
                        .Do(pattern =>
                        {
                            pattern.EventArgs.Cancel = true;
                            Dispatcher.BeginInvoke(() => hostedModel.OnCloseRequested.OnNext(Unit.Default));
                        })
                        .Finally(() => Close())
                        .Subscribe();
            };
        }

        private void OnCloseCanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = true;

        private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (DataContext is IHostedModel hostedModel)
                hostedModel.OnCloseRequested.OnNext(Unit.Default);
            else
                Close();
        }

        private void OnHostCanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = e.Parameter is null or IHostedModel;

        private void OnHostExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is DecisionPromptModel decisionPrompt)
            {
                var result = MessageBox.Show(
                    owner:          this,
                    messageBoxText: decisionPrompt.Message,
                    caption:        decisionPrompt.Title.Value,
                    button:         MessageBoxButton.YesNoCancel,
                    icon:           MessageBoxImage.Warning);

                switch(result)
                {
                    case MessageBoxResult.Yes:
                        decisionPrompt.PublishResult(true);
                        break;
                    
                    case MessageBoxResult.No:
                        decisionPrompt.PublishResult(false);
                        break;

                    default:
                        decisionPrompt.Cancel();
                        break;
                }
            }
            else if (e.Parameter is OpenFilePromptModel openFilePrompt)
            {
                var dialog = new OpenFileDialog()
                {
                    CheckFileExists = true,
                    CheckPathExists = true,
                    FileName        = openFilePrompt.InitialFilePath,
                    Filter          = openFilePrompt.Filter,
                    Title           = openFilePrompt.Title.Value
                };

                if (dialog.ShowDialog() != true)
                    openFilePrompt.Cancel();

                openFilePrompt.PublishResult(dialog.FileName!);
            }
            else if (e.Parameter is SaveFilePromptModel saveFilePrompt)
            {
                var dialog = new SaveFileDialog()
                {
                    CheckPathExists = true,
                    FileName        = saveFilePrompt.InitialFilePath,
                    Filter          = saveFilePrompt.Filter,
                    Title           = saveFilePrompt.Title.Value
                };

                if ((dialog.ShowDialog() == true) && (dialog.FileName is string fileName))
                    saveFilePrompt.PublishResult(fileName);
                else
                    saveFilePrompt.Cancel();
            }
            else if (e.Parameter is IHostedModel hostedModel)
            {
                var host = new HostWindow()
                {
                    DataContext     = hostedModel,
                    SizeToContent   = SizeToContent.WidthAndHeight
                };

                host.ShowDialog();
            }
        }

        private IDisposable? _hostedModelSubscription;
    }
}

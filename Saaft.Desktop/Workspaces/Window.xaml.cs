using System;
using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

namespace Saaft.Desktop.Workspaces
{
    public partial class Window
    {
        public Window()
            => InitializeComponent();

        private void OnCloseCanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = true;

        private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e)
            => Close();

        private void OnLaunchCanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = true;

        private void OnLaunchExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is not Func<ModelBase> workspaceFactory)
                return;

            var workspace = workspaceFactory.Invoke();
            try
            {
                new Window()
                    {
                        DataContext     = workspace,
                        SizeToContent   = SizeToContent.WidthAndHeight
                    }
                    .ShowDialog();
            }
            finally
            {
                if (workspace is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        private void OnPromptCanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = true;

        private void OnPromptExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is DecisionPromptModel decisionPrompt)
            {
                var result = MessageBox.Show(
                    owner:          this,
                    messageBoxText: decisionPrompt.Message,
                    caption:        decisionPrompt.Title,
                    button:         MessageBoxButton.YesNoCancel,
                    icon:           MessageBoxImage.Warning);

                switch(result)
                {
                    case MessageBoxResult.Yes:
                        decisionPrompt.SetResult(true);
                        break;
                    
                    case MessageBoxResult.No:
                        decisionPrompt.SetResult(false);
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
                    Filter          = openFilePrompt.Filter
                };

                if (dialog.ShowDialog() != true)
                    openFilePrompt.Cancel();

                openFilePrompt.SetResult(dialog.FileName!);
            }
            else if (e.Parameter is SaveFilePromptModel saveFilePrompt)
            {
                var dialog = new SaveFileDialog()
                {
                    CheckPathExists = true,
                    FileName        = saveFilePrompt.InitialFilePath,
                    Filter          = saveFilePrompt.Filter
                };

                if (dialog.ShowDialog() != true)
                    saveFilePrompt.Cancel();

                saveFilePrompt.SetResult(dialog.FileName!);
            }
        }
    }
}

using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

using Saaft.Desktop.Prompts;

namespace Saaft.Desktop
{
    public partial class HostWindow
    {
        public HostWindow()
            => InitializeComponent();

        private void OnCloseCanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = true;

        private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e)
            => Close();

        private void OnHostCanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = e.Parameter is null or HostedModelBase;

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
                    Filter          = openFilePrompt.Filter,
                    Title           = openFilePrompt.Title.Value
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
                    Filter          = saveFilePrompt.Filter,
                    Title           = saveFilePrompt.Title.Value
                };

                if (dialog.ShowDialog() != true)
                    saveFilePrompt.Cancel();

                saveFilePrompt.SetResult(dialog.FileName!);
            }
            else if (e.Parameter is HostedModelBase hostedModel)
            {
                try
                {
                    new HostWindow()
                        {
                            DataContext     = hostedModel,
                            SizeToContent   = SizeToContent.WidthAndHeight
                        }
                        .ShowDialog();
                }
                finally
                {
                    if (hostedModel is not ModelBase)
                        hostedModel.Dispose();
                }
            }
        }
    }
}

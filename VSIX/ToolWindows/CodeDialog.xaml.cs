using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace AltaSoft.Storm.ToolWindows;

public partial class CodeDialog : DialogWindow
{
    public CodeDialog(string generatedCode)
    {
        InitializeComponent();
        codeTextBox.Text = generatedCode;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(codeTextBox.Text);
        MessageBox.Show("Copied to Clipboard!");
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        codeTextBox.Focus();
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace VerifyPro.Views;

public partial class ConfigFileView : Window
{
    public ConfigFileView()
    {
        InitializeComponent();
    }
    private async void OnSelectFileClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择配置文件",
            Filters =
            {
                new FileDialogFilter { Name = "JSON 文件", Extensions = { "json" } },
                new FileDialogFilter { Name = "所有文件", Extensions = { "*" } }
            },
            AllowMultiple = false
        };

        var result = await dialog.ShowAsync(this);
        if (result != null && result.Length > 0)
        {
            SelectedFileNameTextBox.Text = result[0];
        }
    }

    private void Close(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
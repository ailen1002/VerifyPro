using Avalonia.Controls;
using Avalonia.Interactivity;
using VerifyPro.ViewModels;

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
        if (result is not { Length: > 0 }) return;
        var filePath = result[0];
        // 访问 ViewModel 并调用逻辑
        if (DataContext is not ConfigFileViewModel vm) return;
        vm.SelectedFilePath = filePath;
    }

    private void Close(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConfigFileViewModel vm)
        {
            vm.ConfirmAndNavigate();
        }
        
        this.Close();
    }
}
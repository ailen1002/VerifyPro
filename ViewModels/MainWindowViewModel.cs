namespace VerifyPro.ViewModels;
using ReactiveUI;
using System.Reactive;
using System;

public class MainWindowViewModel : ReactiveObject
{
    public ReactiveCommand<string, Unit> NavigateCommand { get; }

    public MainWindowViewModel()
    {
        NavigateCommand = ReactiveCommand.Create<string>(NavigateTo);
    }

    private void NavigateTo(string page)
    {
        Console.WriteLine($"导航到页面: {page}");
        // 这里可以添加切换页面的逻辑，例如在 MainWindow 中替换 Content
    }
}
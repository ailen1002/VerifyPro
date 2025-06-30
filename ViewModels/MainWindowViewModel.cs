using System;
using System.Reactive;
using ReactiveUI;
using VerifyPro.Utils;


namespace VerifyPro.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    public ReactiveCommand<string, Unit> NavigateCommand { get; } = ReactiveCommand.Create<string>(NavigateTo);

    private static void NavigateTo(string page)
    {
        Console.WriteLine($"导航到页面: {page}");
        var windowToOpen = WindowFactory.Create(page);
        windowToOpen.Show();
    }
}
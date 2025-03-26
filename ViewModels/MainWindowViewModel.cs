using Avalonia.Controls;
using System;
using System.Reactive;
using ReactiveUI;
using VerifyPro.Views;

namespace VerifyPro.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    public ReactiveCommand<string, Unit> NavigateCommand { get; }

    public MainWindowViewModel()
    {
        NavigateCommand = ReactiveCommand.Create<string>(NavigateTo);
    }
    public void NavigateTo(string page)
    {
        Console.WriteLine($"导航到页面: {page}");

        Window windowToOpen = page switch
        {
            "CommConfig" => new CommConfigView(),
        };

        windowToOpen?.Show();
    }
}
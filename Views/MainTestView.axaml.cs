using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using ReactiveUI;
using VerifyPro.ViewModels;

namespace VerifyPro.Views;

public partial class MainTestView : Window
{
    public MainTestView()
    {
        InitializeComponent();
        
        this.WhenAnyValue(x => x.DataContext)!
            .OfType<MainTestViewModel>()
            .Select(vm => vm.WhenAnyValue(x => x.DetectLog))
            .Switch()
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                DetectLogScrollViewer?.ScrollToEnd();
            });
    }
}
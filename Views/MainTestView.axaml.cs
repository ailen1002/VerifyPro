using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VerifyPro.ViewModels;

namespace VerifyPro.Views;

public partial class MainTestView : Window
{
    public MainTestView()
    {
        InitializeComponent();
        
        DataContext = new MainTestViewModel();
    }
}
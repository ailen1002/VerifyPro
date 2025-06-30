using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace VerifyPro.Views;

public partial class CommConfigView : Window
{
    public CommConfigView()
    {
        InitializeComponent();
    }
    
    private void Close(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
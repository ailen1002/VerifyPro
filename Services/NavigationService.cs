using Avalonia.Controls;
using VerifyPro.Interfaces;
using VerifyPro.Utils;


namespace VerifyPro.Services;

public class NavigationService : INavigationService
{
    public void Navigate(string pageKey)
    {
        var window = WindowFactory.Create(pageKey);
        window.Show();
    }

    public void ShowDialog(string pageKey, Window owner)
    {
        var window = WindowFactory.Create(pageKey);
        window.ShowDialog(owner);
    }
}
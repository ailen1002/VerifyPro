namespace VerifyPro.Interfaces;

public interface INavigationService
{
    void Navigate(string pageKey);
    void ShowDialog(string pageKey, Avalonia.Controls.Window owner);
}
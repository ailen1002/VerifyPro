using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using VerifyPro.Services;
using VerifyPro.ViewModels;
using VerifyPro.Views;

namespace VerifyPro;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();

        ConfigureServices(serviceCollection);

        Services = serviceCollection.BuildServiceProvider();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // ע����񣨵���/˲̬��
        services.AddSingleton<DetectionService>();
        services.AddSingleton<ExportService>();

        // ע�Ṳ�� ViewModel��������
        services.AddSingleton<ConfigFileViewModel>();

        // ע�� MainTestViewModel��ע���������������
        services.AddTransient<MainTestViewModel>();
    }
}
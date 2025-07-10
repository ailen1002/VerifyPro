using System;
using Avalonia;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using VerifyPro.Interfaces;
using VerifyPro.Services;
using VerifyPro.Utils;
using VerifyPro.ViewModels;

namespace VerifyPro;

public class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services  = new ServiceCollection();

        // ע����ͼģ��
        services.AddSingleton<ConfigFileViewModel>();
        services.AddSingleton<MainTestViewModel>();
        //���ķ���
        services.AddSingleton<DeviceCommManager>();
        services.AddSingleton<ExportService>();
        // ע�����
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<DetectionService>();
        services.AddSingleton<DetectionStateService>();

        Services = services.BuildServiceProvider();
        
        // ������һ������
        var nav = Services.GetRequiredService<INavigationService>();
        nav.Navigate("ConfigFile");

        base.OnFrameworkInitializationCompleted();
    }
}
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

        // 注册视图模型
        services.AddSingleton<ConfigFileViewModel>();
        services.AddSingleton<MainTestViewModel>();
        //核心服务
        services.AddSingleton<DeviceCommManager>();
        services.AddSingleton<ExportService>();
        // 注册服务
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<DetectionService>();
        services.AddSingleton<DetectionStateService>();

        Services = services.BuildServiceProvider();
        
        // 启动第一个窗口
        var nav = Services.GetRequiredService<INavigationService>();
        nav.Navigate("ConfigFile");

        base.OnFrameworkInitializationCompleted();
    }
}
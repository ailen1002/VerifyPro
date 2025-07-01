using Avalonia.Controls;
using VerifyPro.Views;
using VerifyPro.ViewModels;
using VerifyPro.Services;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace VerifyPro.Utils;

public static  class WindowFactory
{
    public static Window Create(string page)
    {
        return page switch
        {
            "CommConfig" => new CommConfigView(),
            "TestControllerType" => new TestControllerTypeView(),
            "ConfigFile" => new ConfigFileView
            {
                DataContext = App.Services.GetRequiredService<ConfigFileViewModel>()
            },
            "SelfCheck" => new SelfCheckView(),
            "Calibration" => new CalibrationView(),
            "Finish" => CreateMainTestView(),
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };
    }

    private static Window CreateMainTestView()
    {
        var vm = App.Services.GetRequiredService<MainTestViewModel>();
        return new MainTestView
        {
            DataContext = vm
        };
    }
}
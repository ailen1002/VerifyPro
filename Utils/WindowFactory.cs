using Avalonia.Controls;
using VerifyPro.Views;
using VerifyPro.ViewModels;
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
            "Finish" => new MainTestView
            {
                DataContext = App.Services.GetRequiredService<MainTestViewModel>()
            },
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };
    }
}
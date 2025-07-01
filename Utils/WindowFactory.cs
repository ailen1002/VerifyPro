using Avalonia.Controls;
using VerifyPro.Views;
using VerifyPro.ViewModels;
using VerifyPro.Services;
using System;

namespace VerifyPro.Utils;

public static  class WindowFactory
{
    public static Window Create(string page)
    {
        return page switch
        {
            "CommConfig" => new CommConfigView(),
            "TestControllerType" => new TestControllerTypeView(),
            "ConfigFile" => new ConfigFileView(),
            "SelfCheck" => new SelfCheckView(),
            "Calibration" => new CalibrationView(),
            "Finish" => CreateMainTestView(),
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };
    }

    private static Window CreateMainTestView()
    {
        var detectionService = new DetectionService();
        var exportService = new ExportService();
        var configVm = App.SharedConfigViewModel;
   
        var viewModel = new MainTestViewModel(detectionService,exportService,configVm);
        return new MainTestView
        {
            DataContext = viewModel
        };
    }
}
using System;
using System.Linq;
using ReactiveUI;

namespace VerifyPro.ViewModels;

public class MainTestViewModel : ReactiveObject
{   
    private string _detectLog = "初始内容";
    public string DetectLog
    {
        get => _detectLog;
        set => this.RaiseAndSetIfChanged(ref _detectLog, value);
    }

    public MainTestViewModel()
    {
        // 模拟添加内容
        DetectLog = string.Join(Environment.NewLine, Enumerable.Range(1, 100).Select(i => $"第{i}行内容"));
    }
}
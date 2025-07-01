using System;
using System.IO;
using System.Text.Json;
using ReactiveUI;
using VerifyPro.Interfaces;
using VerifyPro.Models;

namespace VerifyPro.ViewModels;

public class ConfigFileViewModel : ReactiveObject
{
    private readonly INavigationService _navigationService;
    public ConfigFileViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }
    
    private string? _selectedFilePath;
    public string? SelectedFilePath
    {
        get => _selectedFilePath;
        set => this.RaiseAndSetIfChanged(ref _selectedFilePath, value);
    }

    private Config? _config;
    public Config? Config
    {
        get => _config;
        set => this.RaiseAndSetIfChanged(ref _config, value);
    }

    public void LoadConfigFromFile(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var content = File.ReadAllText(path);
            Config = JsonSerializer.Deserialize<Config>(content);
            if (Config != null) Console.WriteLine($"加载内容: {Config.Modelname}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载配置失败: {ex.Message}");
        }
    }
    
    public void ConfirmAndNavigate()
    {
        if (string.IsNullOrEmpty(SelectedFilePath)) return;
        LoadConfigFromFile(SelectedFilePath);
        _navigationService.Navigate("Finish");
    }
}
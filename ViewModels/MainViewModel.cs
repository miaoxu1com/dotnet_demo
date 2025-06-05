using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using XmindToExcelConverter.Models;
using XmindToExcelConverter.Services;
using ClosedXML.Excel;
using Avalonia.Platform.Storage;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace XmindToExcelConverter.ViewModels;

public class MainViewModel : ViewModelBase
{
    private AppConfig _config = null!;
    private string _xmindFilePath = string.Empty;
    private Dictionary<string, string> _fieldValues;
    private string _selectedFilePath = string.Empty;

    private string _outFilePath = string.Empty;

    public MainViewModel()
    {
        LoadConfig();
        _fieldValues = new Dictionary<string, string>();
        
        // Initialize commands
        SelectXmindFileCommand = new RelayCommand(SelectXmindFile);
        GenerateExcelCommand = new RelayCommand(async () => await GenerateExcelAsync());
    }

    private async void LoadConfig()
    {
        var configService = new ConfigService("config.yaml");
        Config = await configService.LoadConfigAsync();
        
        // Initialize field values with empty strings
        foreach (var field in Config.Fields)
        {
            FieldValues[field.Name] = string.Empty;
        }
    }

    public AppConfig Config
    {
        get => _config;
        set => SetProperty(ref _config, value);
    }

    public Dictionary<string, string> FieldValues
    {
        get => _fieldValues;
        set => SetProperty(ref _fieldValues, value);
    }

    public string XmindFilePath
    {
        get => _xmindFilePath;
        set => SetProperty(ref _xmindFilePath, value);
    }

    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set => SetProperty(ref _selectedFilePath, value);
    }
    public string OutFilePath
    {
        get => _outFilePath;
        set => SetProperty(ref _outFilePath, value);
    }
    public IRelayCommand SelectXmindFileCommand { get; }
    public IRelayCommand GenerateExcelCommand { get; }

    private async void SelectXmindFile()
    {
        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (desktop?.MainWindow == null) return;

        var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择思维导图文件",
            AllowMultiple = false,
            FileTypeFilter = new[] 
            { 
                new FilePickerFileType("XMind Files")
                {
                    Patterns = new[] { "*.xmind" }
                }
            }
        });

        if (files.Count > 0)
        {
            SelectedFilePath = files[0].Path.LocalPath;
        }
    }

    private async Task GenerateExcelAsync()
    {
        try
        {
            // Check if template file exists
            if (!File.Exists("template.xlsx"))
            {
                await ShowMessageBox("错误", "模板文件未找到！请确保template.xlsx存在于应用程序目录中.");
                return;
            }

            // Check if XMind file is selected
            if (string.IsNullOrEmpty(SelectedFilePath))
            {
                await ShowMessageBox("提示", "请先选择XMind文件！");
                return;
            }
            
            // Load the template workbook
            using var workbook = new XLWorkbook("template.xlsx");
            var worksheet = workbook.Worksheet(1);
            
            // Fill in the header row with field labels
            int col = 1;
            foreach (var field in Config.Fields)
            {
                worksheet.Cell(1, col).Value = field.Label;
                col++;
            }
            
            // Fill in the data from field values
            col = 1;
            foreach (var field in Config.Fields)
            {
                worksheet.Cell(2, col).Value = FieldValues[field.Name] ?? string.Empty;
                col++;
            }
            
            // Generate output file path
            string directory = Path.GetDirectoryName(SelectedFilePath) ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(SelectedFilePath);
            OutFilePath = Path.Combine(directory, $"{fileName}.xlsx");
            
            // Save the output file
            workbook.SaveAs(OutFilePath);
            
            // Show completion message
            //await ShowMessageBox("成功", $"Excel文件生成成功！保存路径：{_outFilePath}");
        }
        catch (Exception ex)
        {
            await ShowMessageBox("错误", $"生成Excel文件时发生错误：{ex.Message}");
        }
    }

    private async Task ShowMessageBox(string title, string message)
    {
        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (desktop?.MainWindow == null) return;

        var dialog = new Window
        {
            Title = title,
            Content = new TextBlock { Text = message },
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        await dialog.ShowDialog(desktop.MainWindow);
    }
}
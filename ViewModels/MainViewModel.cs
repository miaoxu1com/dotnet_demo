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
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks;

namespace XmindToExcelConverter.ViewModels;

public class MainViewModel : ViewModelBase
{
    private AppConfig _config = null!;
    private string _xmindFilePath = string.Empty;
    private Dictionary<string, string> _fieldValues;
    private string _selectedFilePath = string.Empty;
    private string _outFilePath = string.Empty;
    private readonly XMindService _xmindService;

    public MainViewModel()
    {
        // 设置控制台输出编码为 UTF-8
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        LoadConfig();
        _fieldValues = new Dictionary<string, string>();
        _xmindService = new XMindService();
        
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
            FieldValues[field.Label] = field.Name;
            field.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FieldConfig.Name) && s is FieldConfig fieldConfig)
                {
                    FieldValues[fieldConfig.Label] = fieldConfig.Name;
                }
            };
            Console.WriteLine($"初始化字段: {field.Label}");
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
            if (string.IsNullOrEmpty(SelectedFilePath))
            {
                await ShowMessageBox("提示", "请先选择XMind文件！");
                return;
            }
            
            // Parse XMind file
            var testCases = await _xmindService.ParseXMindFileAsync(SelectedFilePath, FieldValues);
            
            // 使用并行处理生成Excel
            await Task.Run(() => GenerateExcelInParallel(testCases));
            
            await ShowMessageBox("成功", $"Excel文件生成成功！保存路径：{OutFilePath}");
        }
        catch (Exception ex)
        {
            await ShowMessageBox("错误", $"生成Excel文件时发生错误：{ex.Message}");
        }
    }

    private void GenerateExcelInParallel(List<TestCase> testCases)
    {
        // 创建Excel工作簿
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");
        
        // 写入表头
        for (int i = 0; i < Config.Templates.Count; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = Config.Templates[i];
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        
        // 准备数据
        var data = new object[testCases.Count, Config.Templates.Count];
        
        // 并行处理数据准备
        Parallel.For(0, testCases.Count, i =>
        {
            var testCase = testCases[i];
            for (int j = 0; j < Config.Templates.Count; j++)
            {
                string template = Config.Templates[j];
                data[i, j] = template switch
                {
                    "测试用例名称" => testCase.Title,
                    "执行步骤" => testCase.Steps,
                    "预期结果" => testCase.ExpectedResult,
                    _ => testCase.AdditionalFields.GetValueOrDefault(template, string.Empty)
                };
            }
        });
        
        // 写入数据
        for (int i = 0; i < testCases.Count; i++)
        {
            for (int j = 0; j < Config.Templates.Count; j++)
            {
                worksheet.Cell(i + 2, j + 1).Value = data[i, j];
            }
        }
        
        // 设置列宽
        for (int i = 1; i <= Config.Templates.Count; i++)
        {
            worksheet.Column(i).Width = 20;
        }
        
        // 生成输出文件路径
        string directory = Path.GetDirectoryName(SelectedFilePath) ?? string.Empty;
        string fileName = Path.GetFileNameWithoutExtension(SelectedFilePath);
        OutFilePath = Path.Combine(directory, $"{fileName}.xlsx");
        
        // 保存文件
        workbook.SaveAs(OutFilePath);
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
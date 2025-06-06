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
            // Check if XMind file is selected
            if (string.IsNullOrEmpty(SelectedFilePath))
            {
                await ShowMessageBox("提示", "请先选择XMind文件！");
                return;
            }
            
            // 打印当前字段值
            Console.WriteLine("当前字段值:");
            foreach (var field in FieldValues)
            {
                Console.WriteLine($"字段: {field.Key}, 值: {field.Value}");
            }
            
            // Parse XMind file
            var testCases = await _xmindService.ParseXMindFileAsync(SelectedFilePath, FieldValues);
            
            // Create a new workbook
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sheet1");
            
            // Fill in the header row with templates from config
            int col = 1;
            foreach (var template in Config.Templates)
            {
                worksheet.Cell(1, col).Value = template;
                Console.WriteLine($"添加表头: {template}");
                col++;
            }
            
            // Fill in the test cases
            int row = 2;
            foreach (var testCase in testCases)
            {
                col = 1;
                foreach (var template in Config.Templates)
                {
                    string value;
                    if (template == "测试用例名称")
                    {
                        value = testCase.Title;
                    }
                    else if (template == "执行步骤")
                    {
                        value = testCase.Steps;
                    }
                    else if (template == "预期结果")
                    {
                        value = testCase.ExpectedResult;
                    }
                    else
                    {
                        // 对于其他列，使用模板名称作为key来获取对应的值
                        value = testCase.AdditionalFields.GetValueOrDefault(template, string.Empty);
                        Console.WriteLine($"尝试获取字段 {template} 的值: {value}");
                    }
                    worksheet.Cell(row, col).Value = value;
                    col++;
                }
                row++;
            }
            
            // Generate output file path
            string directory = Path.GetDirectoryName(SelectedFilePath) ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(SelectedFilePath);
            OutFilePath = Path.Combine(directory, $"{fileName}.xlsx");
            
            // Save the output file
            workbook.SaveAs(OutFilePath);
            
            await ShowMessageBox("成功", $"Excel文件生成成功！保存路径：{OutFilePath}");
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
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
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text;

namespace XmindToExcelConverter.ViewModels;

public class MainViewModel : ViewModelBase
{
    private AppConfig? _config;
    private string _xmindFilePath = string.Empty;
    private Lazy<Dictionary<string, string>> _fieldValues;
    private string _selectedFilePath = string.Empty;
    private string _outFilePath = string.Empty;
    private readonly Lazy<XMindService> _xmindService;
    private readonly Lazy<ConfigService> _configService;
    private readonly SemaphoreSlim _configLoadSemaphore = new SemaphoreSlim(1, 1);

    public MainViewModel()
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        _fieldValues = new Lazy<Dictionary<string, string>>(() => new Dictionary<string, string>());
        _xmindService = new Lazy<XMindService>(() => new XMindService());
        _configService = new Lazy<ConfigService>(() => new ConfigService("config.yaml"));
        
        // 立即加载配置
        _ = LoadConfigAsync();
        
        SelectXmindFileCommand = new RelayCommand(SelectXmindFile);
        GenerateExcelCommand = new RelayCommand(async () => await GenerateExcelAsync());
    }

    private async Task<AppConfig> LoadConfigAsync()
    {
        if (_config != null)
        {
            return _config;
        }

        await _configLoadSemaphore.WaitAsync();
        try
        {
            if (_config == null)
            {
                _config = await _configService.Value.LoadConfigAsync();
                OnPropertyChanged(nameof(Config));
                
                foreach (var field in _config.Fields)
                {
                    _fieldValues.Value[field.Label] = field.Name;
                    field.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(FieldConfig.Name) && s is FieldConfig fieldConfig)
                        {
                            _fieldValues.Value[fieldConfig.Label] = fieldConfig.Name;
                        }
                    };
                }
            }
            return _config;
        }
        finally
        {
            _configLoadSemaphore.Release();
        }
    }

    public AppConfig Config
    {
        get => _config ?? new AppConfig();
        set
        {
            if (SetProperty(ref _config, value))
            {
                OnPropertyChanged(nameof(Config));
            }
        }
    }

    public Dictionary<string, string> FieldValues => _fieldValues.Value;

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
            
            // 确保配置已加载
            await LoadConfigAsync();
            
            // Parse XMind file
            var testCases = await _xmindService.Value.ParseXMindFileAsync(SelectedFilePath, FieldValues);
            
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
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("测试用例");
        
        // 设置表头
        var headers = new List<string> { "标题", "预期结果", "执行步骤" };
        headers.AddRange(Config.Fields.Select(f => f.Label));
        
        for (int i = 0; i < headers.Count; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }
        
        // 使用并行处理填充数据
        var rowData = new ConcurrentBag<(int Row, object[] Data)>();
        Parallel.ForEach(testCases, (testCase, state, index) =>
        {
            var data = new List<object>
            {
                testCase.Title,
                testCase.ExpectedResult,
                testCase.Steps
            };
            
            foreach (var field in Config.Fields)
            {
                data.Add(testCase.AdditionalFields.GetValueOrDefault(field.Label, string.Empty));
            }
            
            rowData.Add(((int)index + 2, data.ToArray()));
        });
        
        // 按行号排序并填充数据
        foreach (var (row, data) in rowData.OrderBy(x => x.Row))
        {
            for (int i = 0; i < data.Length; i++)
            {
                worksheet.Cell(row, i + 1).Value = data[i];
            }
        }
        
        // 设置列宽
        worksheet.Columns().AdjustToContents();
        
        // 保存文件
        var directory = Path.GetDirectoryName(SelectedFilePath);
        var fileName = Path.GetFileNameWithoutExtension(SelectedFilePath);
        OutFilePath = Path.Combine(directory!, $"{fileName}.xlsx");
        
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
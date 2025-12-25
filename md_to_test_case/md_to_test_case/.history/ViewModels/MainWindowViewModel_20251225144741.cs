using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using md_to_test_case.Services;
using md_to_test_case.Models;

namespace md_to_test_case.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IMarkdownService _markdownService;
        private readonly IXmindService _xmindService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string displayText = "拖拽上传文件";

        [ObservableProperty]
        private bool isProcessing = false;

        public MainWindowViewModel()
        {
            _markdownService = new MarkdownService();
            _xmindService = new XmindService();
            _dialogService = new DialogService();
        }

        [RelayCommand]
        private async Task HandleFileDrop(string[] files)
        {
            if (files == null || files.Length == 0)
                return;

            var filePath = files[0];

            try
            {
                IsProcessing = true;
                DisplayText = "处理中...";

                // 验证文件格式
                if (!_markdownService.IsValidMarkdownFile(filePath))
                {
                    await _dialogService.ShowErrorAsync("文件格式错误", "请选择有效的Markdown文件（.md或.markdown）");
                    return;
                }

                // 读取并解析Markdown内容
                var content = await File.ReadAllTextAsync(filePath);
                var testCases = _markdownService.ParseMarkdownToTestCases(content);

                if (testCases == null || !testCases.Any())
                {
                    await _dialogService.ShowErrorAsync("解析失败", "未能从Markdown文件中解析出有效的测试用例");
                    return;
                }

                // 测试：打印解析结果
                TestPrintParsedData(testCases);

                // 生成XMind文件
                var outputPath = Path.ChangeExtension(filePath, ".xmind");
                await _xmindService.GenerateXmindFileAsync(testCases, outputPath);

                await _dialogService.ShowSuccessAsync("转换成功", $"XMind文件已生成：{outputPath}");
                DisplayText = "转换完成";
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("处理失败", $"处理文件时发生错误：{ex.Message}");
            }
            finally
            {
                IsProcessing = false;
                DisplayText = "拖拽上传文件";
            }
        }

        private void TestPrintParsedData(List<TestCase> testCases)
        {
            try
            {
                Console.WriteLine($"\n========== 解析结果测试 ==========");
                Console.WriteLine($"根节点数量: {testCases.Count}\n");

                if (testCases.Count > 0)
                {
                    PrintTestCase(testCases[0], 0);
                }

                Console.WriteLine($"\n========== 测试完成 ==========\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试打印错误: {ex.Message}");
            }
        }

        private void PrintTestCase(TestCase testCase, int indent)
        {
            try
            {
                var prefix = new string(' ', indent * 2);
                var nodeType = testCase.IsRootNode ? "[根节点]" : "";
                Console.WriteLine($"{prefix}[{testCase.Level}] {testCase.Title} {nodeType}");

                if (testCase.Steps.Count > 0)
                {
                    Console.WriteLine($"{prefix}  步骤 ({testCase.Steps.Count}项):");
                    foreach (var step in testCase.Steps)
                    {
                        Console.WriteLine($"{prefix}    - {step}");
                    }
                }

                if (testCase.ExpectedResults.Count > 0)
                {
                    Console.WriteLine($"{prefix}  预期结果 ({testCase.ExpectedResults.Count}项):");
                    foreach (var result in testCase.ExpectedResults)
                    {
                        Console.WriteLine($"{prefix}    - {result}");
                    }
                }

                if (testCase.Children.Count > 0)
                {
                    Console.WriteLine($"{prefix}  子节点 ({testCase.Children.Count}个)");
                    foreach (var child in testCase.Children)
                    {
                        PrintTestCase(child, indent + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"打印节点错误: {ex.Message}");
            }
        }
    }
}
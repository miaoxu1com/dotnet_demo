using System;
using System.Collections.Generic;
using System.IO;
using md_to_test_case.Models;
using md_to_test_case.Services;

namespace md_to_test_case
{
    public class TestParser
    {
        public static void Test()
        {
            try
            {
                var service = new MarkdownService();
                var filePath = @"D:\Projects\vscode_wpf\md_to_test_case\md_to_test_case\TestCase\展业端AI审批官集成优化-主辅调合影任务测试用例.md";

                if (!service.IsValidMarkdownFile(filePath))
                {
                    Console.WriteLine("无效的Markdown文件");
                    return;
                }

                var content = File.ReadAllText(filePath);
                var testCases = service.ParseMarkdownToTestCases(content);

                Console.WriteLine($"解析完成，共生成 {testCases.Count} 个根节点\n");

                if (testCases.Count > 0)
                {
                    PrintTestCase(testCases[0], 0);
                }

                Console.WriteLine("\n按任意键退出...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ReadKey();
            }
        }

        private static void PrintTestCase(TestCase testCase, int indent)
        {
            var prefix = new string(' ', indent * 2);
            Console.WriteLine($"{prefix}[{testCase.Level}] {testCase.Title}");

            if (testCase.Steps.Count > 0)
            {
                Console.WriteLine($"{prefix}  步骤:");
                foreach (var step in testCase.Steps)
                {
                    Console.WriteLine($"{prefix}    - {step}");
                }
            }

            if (testCase.ExpectedResults.Count > 0)
            {
                Console.WriteLine($"{prefix}  预期结果:");
                foreach (var result in testCase.ExpectedResults)
                {
                    Console.WriteLine($"{prefix}    - {result}");
                }
            }

            foreach (var child in testCase.Children)
            {
                PrintTestCase(child, indent + 1);
            }
        }
    }
}
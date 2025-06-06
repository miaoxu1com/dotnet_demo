using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XmindToExcelConverter.Models;

namespace XmindToExcelConverter.Services;

public class XMindService
{
    public XMindService()
    {
        // 设置控制台输出编码为 UTF-8
        Console.OutputEncoding = System.Text.Encoding.UTF8;
    }

    public async Task<List<TestCase>> ParseXMindFileAsync(string filePath, Dictionary<string, string> fieldValues)
    {
        var testCases = new List<TestCase>();
        
        // 打印传入的字段值
        Console.WriteLine("XMindService 接收到的字段值:");
        foreach (var field in fieldValues)
        {
            Console.WriteLine($"字段: {field.Key}, 值: {field.Value}");
        }
        
        using var archive = ZipFile.OpenRead(filePath);
        var contentEntry = archive.Entries.FirstOrDefault(e => e.FullName == "content.json");
        
        if (contentEntry == null)
        {
            throw new Exception("无法找到 content.json 文件");
        }
        
        using var stream = contentEntry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        
        var options = new JsonDocumentOptions
        {
            MaxDepth = 128
        };
        
        var jsonDoc = JsonDocument.Parse(content, options);
        var root = jsonDoc.RootElement;
        
        // 获取第一个 sheet
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            throw new Exception("无法找到有效的思维导图表单");
        }
        
        var sheet = root[0];
        var rootTopic = sheet.GetProperty("rootTopic");
        
        // 获取根节点标题
        string rootTitle = rootTopic.TryGetProperty("title", out var rootTitleElement) 
            ? rootTitleElement.GetString() ?? string.Empty 
            : string.Empty;
            
        // 处理所有分支
        ProcessBranches(rootTopic, rootTitle, testCases, fieldValues);
        
        return testCases;
    }
    
    private void ProcessBranches(JsonElement topic, string currentPath, List<TestCase> testCases, Dictionary<string, string> fieldValues)
    {
        // 获取当前节点标题
        string currentTitle = topic.TryGetProperty("title", out var titleElement) 
            ? titleElement.GetString() ?? string.Empty 
            : string.Empty;
            
        // 构建当前路径
        string path = string.IsNullOrEmpty(currentPath) ? currentTitle : $"{currentPath}_{currentTitle}";
        
        // 检查是否有子节点
        if (topic.TryGetProperty("children", out var children) && 
            children.TryGetProperty("attached", out var attached) &&
            attached.ValueKind == JsonValueKind.Array)
        {
            // 如果有子节点，继续递归处理
            foreach (var child in attached.EnumerateArray())
            {
                ProcessBranches(child, path, testCases, fieldValues);
            }
        }
        else
        {
            // 如果没有子节点，说明是叶子节点，创建测试用例
            var testCase = new TestCase
            {
                Title = path,
                ExpectedResult = currentTitle, // 最后一个节点作为预期结果
                Steps = path // 完整路径作为步骤
            };
            
            // 添加用户输入的字段值
            foreach (var field in fieldValues)
            {
                if (!string.IsNullOrEmpty(field.Value)) // 只添加有值的字段
                {
                    testCase.AdditionalFields[field.Key] = field.Value;
                    Console.WriteLine($"添加字段到测试用例: {field.Key} = {field.Value}");
                }
            }
            
            testCases.Add(testCase);
        }
    }
} 
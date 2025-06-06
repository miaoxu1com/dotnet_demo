using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using XmindToExcelConverter.Models;

namespace XmindToExcelConverter.Services;

public class XMindService
{
    private readonly ObjectPool<JsonDocument> _jsonDocumentPool;
    private const int MaxPoolSize = 10;

    public XMindService()
    {
        // 设置控制台输出编码为 UTF-8
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        _jsonDocumentPool = new ObjectPool<JsonDocument>(MaxPoolSize);
    }

    public async Task<List<TestCase>> ParseXMindFileAsync(string filePath, Dictionary<string, string> fieldValues)
    {
        var testCases = new ConcurrentBag<TestCase>();
        
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
            MaxDepth = 128,
            AllowTrailingCommas = true
        };
        
        using var jsonDoc = JsonDocument.Parse(content, options);
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
            
        // 使用并行处理处理分支
        await Task.Run(() => ProcessBranchesParallel(rootTopic, rootTitle, testCases, fieldValues));
        
        return testCases.ToList();
    }
    
    private void ProcessBranchesParallel(JsonElement topic, string currentPath, ConcurrentBag<TestCase> testCases, Dictionary<string, string> fieldValues)
    {
        var tasks = new List<Task>();
        
        void ProcessBranch(JsonElement branch, string path)
        {
            string currentTitle = branch.TryGetProperty("title", out var titleElement) 
                ? titleElement.GetString() ?? string.Empty 
                : string.Empty;
            Console.WriteLine($"打印标题：{currentTitle}");
            
            // 修改路径构建逻辑
            string newPath;
            if (string.IsNullOrEmpty(path))
            {
                // 如果是根节点，直接使用当前标题
                newPath = currentTitle;
            }
            else if (path == currentTitle)
            {
                // 如果路径等于当前标题，说明是第一个子节点，直接使用当前标题
                newPath = currentTitle;
            }
            else
            {
                // 其他情况，正常拼接路径
                newPath = $"{path}_{currentTitle}";
            }
            
            if (branch.TryGetProperty("children", out var children) && 
                children.TryGetProperty("attached", out var attached) &&
                attached.ValueKind == JsonValueKind.Array)
            {
                var childTasks = new List<Task>();
                foreach (var child in attached.EnumerateArray())
                {
                    childTasks.Add(Task.Run(() => ProcessBranch(child, newPath)));
                }
                Task.WaitAll(childTasks.ToArray());
            }
            else
            {
                // 获取父节点路径（不包含当前节点）
                string steps = path;
                
                var testCase = new TestCase
                {
                    Title = newPath,
                    ExpectedResult = currentTitle,
                    Steps = steps // 使用父节点路径作为执行步骤
                };
                
                foreach (var field in fieldValues)
                {
                    if (!string.IsNullOrEmpty(field.Value))
                    {
                        testCase.AdditionalFields[field.Key] = field.Value;
                    }
                }
                
                testCases.Add(testCase);
            }
        }
        
        ProcessBranch(topic, currentPath);
    }
}

public class ObjectPool<T> where T : IDisposable
{
    private readonly ConcurrentBag<T> _objects;
    private readonly Func<T> _objectGenerator;
    private readonly int _maxSize;

    public ObjectPool(int maxSize, Func<T> objectGenerator = null)
    {
        _objects = new ConcurrentBag<T>();
        _objectGenerator = objectGenerator;
        _maxSize = maxSize;
    }

    public T Get()
    {
        return _objects.TryTake(out T item) ? item : _objectGenerator();
    }

    public void Return(T item)
    {
        if (_objects.Count < _maxSize)
        {
            _objects.Add(item);
        }
        else
        {
            item.Dispose();
        }
    }
} 
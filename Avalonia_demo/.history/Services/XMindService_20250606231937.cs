using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.Runtime.Caching;
using XmindToExcelConverter.Models;

namespace XmindToExcelConverter.Services;

public class XMindService
{
    private readonly ObjectPool<JsonDocument> _jsonDocumentPool;
    private readonly MemoryCache _fileCache;
    private const int MaxPoolSize = 10;
    private const int CacheExpirationMinutes = 5;

    public XMindService()
    {
        // 设置控制台输出编码为 UTF-8
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        _jsonDocumentPool = new ObjectPool<JsonDocument>(MaxPoolSize, () => JsonDocument.Parse("{}"));
        _fileCache = new MemoryCache("XMindFileCache");
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
        
        string content = await GetFileContentAsync(filePath);
        
        using var jsonDoc = _jsonDocumentPool.Get();
        try
        {
            var options = new JsonDocumentOptions
            {
                MaxDepth = 128,
                AllowTrailingCommas = true
            };
            
            var root = JsonDocument.Parse(content, options).RootElement;
            
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
        finally
        {
            _jsonDocumentPool.Return(jsonDoc);
        }
    }
    
    private async Task<string> GetFileContentAsync(string filePath)
    {
        string cacheKey = $"file_{filePath}";
        if (_fileCache.Contains(cacheKey))
        {
            return (string)_fileCache.Get(cacheKey);
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
        
        var cachePolicy = new CacheItemPolicy
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(CacheExpirationMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };
        
        _fileCache.Set(cacheKey, content, cachePolicy);
        
        return content;
    }
    
    private void ProcessBranchesParallel(JsonElement topic, string currentPath, ConcurrentBag<TestCase> testCases, Dictionary<string, string> fieldValues)
    {
        var tasks = new List<Task>();
        
        void ProcessBranch(JsonElement branch, string path)
        {
            if (!branch.TryGetProperty("title", out var titleElement))
                return;
                
            string currentTitle = titleElement.GetString() ?? string.Empty;
            string newPath = string.IsNullOrEmpty(path) ? currentTitle : $"{path} > {currentTitle}";
            
            if (branch.TryGetProperty("children", out var childrenElement) && 
                childrenElement.TryGetProperty("attached", out var attachedElement))
            {
                var attachedArray = attachedElement.EnumerateArray();
                foreach (var child in attachedArray)
                {
                    ProcessBranch(child, newPath);
                }
            }
            else
            {
                string steps = path;
                
                var testCase = new TestCase
                {
                    Title = newPath,
                    ExpectedResult = currentTitle,
                    Steps = steps
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
    private readonly SemaphoreSlim _semaphore;

    public ObjectPool(int maxSize, Func<T> objectGenerator)
    {
        _objects = new ConcurrentBag<T>();
        _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
        _maxSize = maxSize;
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public async Task<T> GetAsync()
    {
        if (_objects.TryTake(out T item))
        {
            return item;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (_objects.Count < _maxSize)
            {
                var newItem = _objectGenerator();
                return newItem;
            }
            return _objectGenerator();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Return(T item)
    {
        if (item == null) return;

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
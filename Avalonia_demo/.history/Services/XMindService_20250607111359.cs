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
using Serilog;
using Serilog.Events;
using XmindToExcelConverter.Models;

namespace XmindToExcelConverter.Services;

public class XMindService : IXMindService
{
    private readonly ObjectPool<JsonDocument> _jsonDocumentPool;
    private readonly MemoryCache _fileCache;
    private readonly ILogger _logger;
    private readonly XMindServiceOptions _options;

    public XMindService(XMindServiceOptions? options = null)
    {
        _options = options ?? new XMindServiceOptions();
        _logger = CreateLogger();
        _jsonDocumentPool = new ObjectPool<JsonDocument>(_options.MaxPoolSize, () => JsonDocument.Parse("{}"));
        _fileCache = new MemoryCache("XMindFileCache");
    }

    private ILogger CreateLogger()
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug();

        if (_options.EnableConsoleLogging)
        {
            loggerConfig.WriteTo.Console();
        }

        if (_options.EnableFileLogging)
        {
            loggerConfig.WriteTo.File(
                _options.LogFilePath,
                rollingInterval: Serilog.RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
        }

        return loggerConfig.CreateLogger();
    }

    public async Task<List<TestCase>> ParseXMindFileAsync(string filePath, Dictionary<string, string> fieldValues)
    {
        var testCases = new ConcurrentBag<TestCase>();
        
        _logger.Information("开始解析XMind文件: {FilePath}", filePath);
        _logger.Debug("接收到的字段值: {@FieldValues}", fieldValues);
        
        try
        {
            string content = await GetFileContentAsync(filePath);
            using var jsonDoc = await _jsonDocumentPool.GetAsync();
            
            var root = ParseJsonContent(content);
            var rootTopic = GetRootTopic(root);
            string rootTitle = GetRootTitle(rootTopic);
                
            _logger.Information("开始处理思维导图，根节点标题: {RootTitle}", rootTitle);
            
            await Task.Run(() => ProcessBranchesParallel(rootTopic, string.Empty, testCases, fieldValues));
            
            _logger.Information("思维导图处理完成，共生成 {Count} 个测试用例", testCases.Count);
            return testCases.ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "处理XMind文件时发生错误");
            throw;
        }
    }

    private JsonElement ParseJsonContent(string content)
    {
        var options = new JsonDocumentOptions
        {
            MaxDepth = 128,
            AllowTrailingCommas = true
        };
        
        var root = JsonDocument.Parse(content, options).RootElement;
        
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            _logger.Error("无法找到有效的思维导图表单");
            throw new Exception("无法找到有效的思维导图表单");
        }

        return root;
    }

    private JsonElement GetRootTopic(JsonElement root)
    {
        var sheet = root[0];
        return sheet.GetProperty("rootTopic");
    }

    private string GetRootTitle(JsonElement rootTopic)
    {
        return rootTopic.TryGetProperty("title", out var rootTitleElement) 
            ? rootTitleElement.GetString() ?? string.Empty 
            : string.Empty;
    }
    
    private async Task<string> GetFileContentAsync(string filePath)
    {
        string cacheKey = $"file_{filePath}";
        if (_fileCache.Contains(cacheKey))
        {
            _logger.Debug("从缓存中获取文件内容: {FilePath}", filePath);
            return (string)_fileCache.Get(cacheKey);
        }

        _logger.Debug("从文件系统读取内容: {FilePath}", filePath);
        using var archive = ZipFile.OpenRead(filePath);
        var contentEntry = archive.Entries.FirstOrDefault(e => e.FullName == "content.json");
        
        if (contentEntry == null)
        {
            _logger.Error("无法找到content.json文件: {FilePath}", filePath);
            throw new Exception("无法找到 content.json 文件");
        }
        
        using var stream = contentEntry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        
        var cachePolicy = new CacheItemPolicy
        {
            AbsoluteExpiration = DateTimeOffset.MaxValue,
            SlidingExpiration = TimeSpan.FromMinutes(_options.CacheExpirationMinutes)
        };
        
        _fileCache.Set(cacheKey, content, cachePolicy);
        _logger.Debug("文件内容已缓存: {FilePath}", filePath);
        
        return content;
    }
    
    private void ProcessBranchesParallel(JsonElement topic, string currentPath, ConcurrentBag<TestCase> testCases, Dictionary<string, string> fieldValues)
    {
        void ProcessBranch(JsonElement branch, string path)
        {
            if (!branch.TryGetProperty("title", out var titleElement))
            {
                _logger.Warning("分支缺少title属性");
                return;
            }
                
            string currentTitle = titleElement.GetString() ?? string.Empty;
            string newPath = string.IsNullOrEmpty(path) ? currentTitle : $"{path}_{currentTitle}";
            
            _logger.Debug("处理分支: {Path}", newPath);
            
            if (HasChildren(branch))
            {
                ProcessChildren(branch, newPath, testCases, fieldValues);
            }
            else
            {
                AddTestCase(newPath, currentTitle, path, fieldValues, testCases);
            }
        }
        
        ProcessBranch(topic, currentPath);
    }

    private bool HasChildren(JsonElement branch)
    {
        return branch.TryGetProperty("children", out var childrenElement) && 
               childrenElement.TryGetProperty("attached", out _);
    }

    private void ProcessChildren(JsonElement branch, string newPath, ConcurrentBag<TestCase> testCases, Dictionary<string, string> fieldValues)
    {
        var childrenElement = branch.GetProperty("children");
        var attachedElement = childrenElement.GetProperty("attached");
        var attachedArray = attachedElement.EnumerateArray();
        
        foreach (var child in attachedArray)
        {
            ProcessBranch(child, newPath);
        }
    }

    private void AddTestCase(string newPath, string currentTitle, string path, Dictionary<string, string> fieldValues, ConcurrentBag<TestCase> testCases)
    {
        var testCase = new TestCase
        {
            Title = newPath,
            ExpectedResult = currentTitle,
            Steps = path
        };
        
        foreach (var field in fieldValues)
        {
            if (!string.IsNullOrEmpty(field.Value))
            {
                testCase.AdditionalFields[field.Key] = field.Value;
            }
        }
        
        testCases.Add(testCase);
        _logger.Debug("添加测试用例: {Title}", newPath);
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
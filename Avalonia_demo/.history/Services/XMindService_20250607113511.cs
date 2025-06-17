using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using XmindToExcelConverter.Models;

namespace XmindToExcelConverter.Services;

public class XMindService : IXMindService
{
    private readonly Lazy<ObjectPool<JsonDocument>> _jsonDocumentPool;
    private readonly MemoryCache _fileCache;
    private readonly Lazy<ILogger> _logger;
    private readonly XMindServiceOptions _options;
    private MemoryMappedFile? _mappedFile;
    private MemoryMappedViewAccessor? _viewAccessor;

    public XMindService(XMindServiceOptions? options = null)
    {
        _options = options ?? new XMindServiceOptions();
        _logger = new Lazy<ILogger>(CreateLogger);
        _jsonDocumentPool = new Lazy<ObjectPool<JsonDocument>>(() => 
            new ObjectPool<JsonDocument>(_options.MaxPoolSize, () => JsonDocument.Parse("{}")));
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
        var testCases = new Lazy<ConcurrentBag<TestCase>>(() => new ConcurrentBag<TestCase>());
        
        _logger.Value.Information("开始解析XMind文件: {FilePath}", filePath);
        _logger.Value.Debug("接收到的字段值: {@FieldValues}", fieldValues);
        
        try
        {
            string content = await GetFileContentAsync(filePath);
            using var jsonDoc = await _jsonDocumentPool.Value.GetAsync();
            
            var root = ParseJsonContent(content);
            var rootTopic = GetRootTopic(root);
            string rootTitle = GetRootTitle(rootTopic);
                
            _logger.Value.Information("开始处理思维导图，根节点标题: {RootTitle}", rootTitle);
            
            var processingTask = new Lazy<Task>(() => Task.Run(() => 
                ProcessBranchesParallel(rootTopic, string.Empty, testCases.Value, fieldValues)));
            
            await processingTask.Value;
            
            _logger.Value.Information("思维导图处理完成，共生成 {Count} 个测试用例", testCases.Value.Count);
            return testCases.Value.ToList();
        }
        catch (Exception ex)
        {
            _logger.Value.Error(ex, "处理XMind文件时发生错误");
            throw;
        }
        finally
        {
            DisposeMappedFile();
        }
    }

    private void DisposeMappedFile()
    {
        _viewAccessor?.Dispose();
        _mappedFile?.Dispose();
        _viewAccessor = null;
        _mappedFile = null;
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
            _logger.Value.Error("无法找到有效的思维导图表单");
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
            _logger.Value.Debug("从缓存中获取文件内容: {FilePath}", filePath);
            return (string)_fileCache.Get(cacheKey);
        }

        _logger.Value.Debug("从文件系统读取内容: {FilePath}", filePath);
        
        try
        {
            // 使用 FileShare.Read 允许其他进程同时读取文件
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
            var contentEntry = archive.Entries.FirstOrDefault(e => e.FullName == "content.json");
            
            if (contentEntry == null)
            {
                _logger.Value.Error("无法找到content.json文件: {FilePath}", filePath);
                throw new Exception("无法找到 content.json 文件");
            }

            // 直接读取文件内容，不使用内存映射
            using var stream = contentEntry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            
            var cachePolicy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.MaxValue,
                SlidingExpiration = TimeSpan.FromMinutes(_options.CacheExpirationMinutes)
            };
            
            _fileCache.Set(cacheKey, content, cachePolicy);
            _logger.Value.Debug("文件内容已缓存: {FilePath}", filePath);
            
            return content;
        }
        catch (IOException ex)
        {
            _logger.Value.Error(ex, "文件被其他进程占用: {FilePath}", filePath);
            throw new IOException($"文件 {filePath} 正在被其他程序使用，请关闭后重试。", ex);
        }
        catch (Exception ex)
        {
            _logger.Value.Error(ex, "读取文件内容时发生错误: {FilePath}", filePath);
            throw;
        }
    }
    
    private void ProcessBranchesParallel(JsonElement topic, string currentPath, ConcurrentBag<TestCase> testCases, Dictionary<string, string> fieldValues)
    {
        ProcessBranch(topic, currentPath, testCases, fieldValues);
    }

    private void ProcessBranch(JsonElement branch, string path, ConcurrentBag<TestCase> testCases, Dictionary<string, string> fieldValues)
    {
        if (!branch.TryGetProperty("title", out var titleElement))
        {
            _logger.Value.Warning("分支缺少title属性");
            return;
        }
            
        string currentTitle = titleElement.GetString() ?? string.Empty;
        string newPath = string.IsNullOrEmpty(path) ? currentTitle : $"{path}_{currentTitle}";
        
        _logger.Value.Debug("处理分支: {Path}", newPath);
        
        if (HasChildren(branch))
        {
            ProcessChildren(branch, newPath, testCases, fieldValues);
        }
        else
        {
            AddTestCase(newPath, currentTitle, path, fieldValues, testCases);
        }
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
            ProcessBranch(child, newPath, testCases, fieldValues);
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
        _logger.Value.Debug("添加测试用例: {Title}", newPath);
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
namespace XmindToExcelConverter.Services;

public class XMindServiceOptions
{
    /// <summary>
    /// 对象池最大大小
    /// </summary>
    public int MaxPoolSize { get; set; } = 10;

    /// <summary>
    /// 缓存过期时间（分钟）
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// 日志文件路径
    /// </summary>
    public string LogFilePath { get; set; } = "logs/xmind-service-.log";

    /// <summary>
    /// 是否启用控制台日志
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = true;

    /// <summary>
    /// 是否启用文件日志
    /// </summary>
    public bool EnableFileLogging { get; set; } = true;

    /// <summary>
    /// 日志级别
    /// </summary>
    public string LogLevel { get; set; } = "Debug";
} 
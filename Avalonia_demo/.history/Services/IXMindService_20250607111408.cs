using System.Collections.Generic;
using System.Threading.Tasks;
using XmindToExcelConverter.Models;

namespace XmindToExcelConverter.Services;

public interface IXMindService
{
    /// <summary>
    /// 解析XMind文件并生成测试用例列表
    /// </summary>
    /// <param name="filePath">XMind文件路径</param>
    /// <param name="fieldValues">字段值映射</param>
    /// <returns>测试用例列表</returns>
    Task<List<TestCase>> ParseXMindFileAsync(string filePath, Dictionary<string, string> fieldValues);
} 
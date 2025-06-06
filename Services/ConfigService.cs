using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using XmindToExcelConverter.Models;

namespace XmindToExcelConverter.Services;

public class ConfigService
{
    private readonly string _configPath;

    public ConfigService(string configPath)
    {
        _configPath = configPath;
    }

    public async Task<AppConfig> LoadConfigAsync()
    {
        var yaml = await File.ReadAllTextAsync(_configPath, Encoding.UTF8);
        
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
            
        try
        {
            return deserializer.Deserialize<AppConfig>(yaml);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Deserialization error: {ex}");
            throw;
        }
    }
}
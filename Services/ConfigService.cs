using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
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
        var yaml = await File.ReadAllTextAsync(_configPath);
        var deserializer = new DeserializerBuilder().Build();
        return deserializer.Deserialize<AppConfig>(yaml);
    }
}
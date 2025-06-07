using System.Collections.Generic;

namespace XmindToExcelConverter.Models;

public class TestCase
{
    public string Title { get; set; } = string.Empty;
    public string Steps { get; set; } = string.Empty;
    public string ExpectedResult { get; set; } = string.Empty;
    public Dictionary<string, string> AdditionalFields { get; set; } = new();
} 
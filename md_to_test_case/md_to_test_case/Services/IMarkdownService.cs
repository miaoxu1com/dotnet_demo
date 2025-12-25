using System.Collections.Generic;
using md_to_test_case.Models;

namespace md_to_test_case.Services
{
    public interface IMarkdownService
    {
        bool IsValidMarkdownFile(string filePath);
        List<TestCase> ParseMarkdownToTestCases(string content);
    }
}
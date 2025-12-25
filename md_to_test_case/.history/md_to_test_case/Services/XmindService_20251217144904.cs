using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using md_to_test_case.Models;
using XMindAPI;
using XMindAPI.Models;

namespace md_to_test_case.Services
{
    public class XmindService : IXmindService
    {
        public async Task GenerateXmindFileAsync(List<TestCase> testCases, string outputPath)
        {
            // 这里是一个简化的实现，生成XML格式的思维导图
            // 实际项目中可以使用专门的XMind库
            var xml = GenerateXmindXml(testCases);
            await File.WriteAllTextAsync(outputPath, xml, Encoding.UTF8);
        }

        private string GenerateXmindXml(List<TestCase> testCases)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<xmap-content>");
            sb.AppendLine("  <sheet>");
            sb.AppendLine("    <topic>");
            sb.AppendLine("      <title>测试用例</title>");
            
            foreach (var testCase in testCases)
            {
                GenerateTestCaseXml(sb, testCase, 3);
            }
            
            sb.AppendLine("    </topic>");
            sb.AppendLine("  </sheet>");
            sb.AppendLine("</xmap-content>");
            
            return sb.ToString();
        }

        private void GenerateTestCaseXml(StringBuilder sb, TestCase testCase, int indent)
        {
            var indentStr = new string(' ', indent * 2);
            
            sb.AppendLine($"{indentStr}<topic>");
            sb.AppendLine($"{indentStr}  <title>{EscapeXml(testCase.Title)}</title>");
            
            // 添加步骤节点
            if (testCase.Steps.Count > 0)
            {
                sb.AppendLine($"{indentStr}  <topic>");
                sb.AppendLine($"{indentStr}    <title>步骤</title>");
                foreach (var step in testCase.Steps)
                {
                    sb.AppendLine($"{indentStr}    <topic>");
                    sb.AppendLine($"{indentStr}      <title>{EscapeXml(step)}</title>");
                    sb.AppendLine($"{indentStr}    </topic>");
                }
                sb.AppendLine($"{indentStr}  </topic>");
            }
            
            // 添加预期结果节点
            if (testCase.ExpectedResults.Count > 0)
            {
                sb.AppendLine($"{indentStr}  <topic>");
                sb.AppendLine($"{indentStr}    <title>预期结果</title>");
                foreach (var result in testCase.ExpectedResults)
                {
                    sb.AppendLine($"{indentStr}    <topic>");
                    sb.AppendLine($"{indentStr}      <title>{EscapeXml(result)}</title>");
                    sb.AppendLine($"{indentStr}    </topic>");
                }
                sb.AppendLine($"{indentStr}  </topic>");
            }
            
            // 递归处理子节点
            foreach (var child in testCase.Children)
            {
                GenerateTestCaseXml(sb, child, indent + 1);
            }
            
            sb.AppendLine($"{indentStr}</topic>");
        }

        private string EscapeXml(string text)
        {
            return text.Replace("&", "&amp;")
                      .Replace("<", "&lt;")
                      .Replace(">", "&gt;")
                      .Replace("\"", "&quot;")
                      .Replace("'", "&apos;");
        }
    }
}
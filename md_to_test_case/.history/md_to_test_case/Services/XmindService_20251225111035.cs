using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using md_to_test_case.Models;
using XMindAPI;
using XMindAPI.Core;
using XMindAPI.Core.Element;



namespace md_to_test_case.Services
{
    public class XmindService : IXmindService
    {
        public async Task GenerateXmindFileAsync(List<TestCase> testCases, string outputPath)
        {
            // 使用xmindcsharp库生成标准XMind文件
            var workbook = new XMindConfiguration()
                .WithFileWriter(Path.GetDirectoryName(outputPath), zip: true)
                .CreateWorkBook(Path.GetFileName(outputPath));
                
            var sheet = workbook.CreateSheet();
            var rootTopic = sheet.GetRootTopic();
            rootTopic.SetTitle("测试用例");
                
            // 递归添加测试用例节点
            foreach (var testCase in testCases)
            {
                AddTestCaseToTopic(rootTopic, testCase);
            }
                
            workbook.AddSheet(sheet, 0);
            workbook.Save();
                    
            await Task.CompletedTask; // 为了满足async方法的要求
        }
        
        private void AddTestCaseToTopic(ITopic parentTopic, TestCase testCase)
        {
            var topic = parentTopic.AddTopic();
            topic.SetTitle(testCase.Title);
        
            // 添加步骤节点
            if (testCase.Steps.Count > 0)
            {
                var stepsTopic = topic.AddTopic();
                stepsTopic.SetTitle("步骤");
                foreach (var step in testCase.Steps)
                {
                    var stepTopic = stepsTopic.AddTopic();
                    stepTopic.SetTitle(step);
                }
            }
        
            // 添加预期结果节点
            if (testCase.ExpectedResults.Count > 0)
            {
                var resultsTopic = topic.AddTopic();
                resultsTopic.SetTitle("预期结果");
                foreach (var result in testCase.ExpectedResults)
                {
                    var resultTopic = resultsTopic.AddTopic();
                    resultTopic.SetTitle(result);
                }
            }
        
            // 递归处理子节点
            foreach (var child in testCase.Children)
            {
                AddTestCaseToTopic(topic, child);
            }
        }
    }
}
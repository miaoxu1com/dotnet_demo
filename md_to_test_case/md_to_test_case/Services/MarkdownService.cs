using System;
using System.Collections.Generic;
using System.IO;
using md_to_test_case.Models;

namespace md_to_test_case.Services
{
    public class MarkdownService : IMarkdownService
    {
        public bool IsValidMarkdownFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".md" || extension == ".markdown";
        }

        public List<TestCase> ParseMarkdownToTestCases(string content)
        {
            var testCases = new List<TestCase>();
            var lines = content.Split('\n', StringSplitOptions.None);

            TestCase rootNode = null;           // 一级标题：根节点
            TestCase currentLevel2Node = null;   // 二级标题：用例集合
            TestCase currentLevel3Node = null;   // 三级标题：用例名称
            TestCase currentStepsNode = null;    // 步骤节点
            TestCase currentResultsNode = null;  // 预期结果节点

            string currentSection = null;        // 当前处理的区域："steps" 或 "results"
            var currentListItems = new List<string>(); // 当前收集的列表项

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // 先检测"步骤:"或"预期结果:"（可能是标题格式）
                if (IsStepsLine(trimmedLine))
                {
                    // 保存之前的列表内容
                    SaveCurrentListItems(currentStepsNode, currentResultsNode, currentSection, currentListItems);
                    currentListItems.Clear();

                    if (currentLevel3Node != null)
                    {
                        // 创建"步骤"节点，作为当前用例的子节点
                        currentStepsNode = new TestCase
                        {
                            Title = "步骤",
                            Level = 4
                        };
                        currentLevel3Node.Children.Add(currentStepsNode);
                        currentSection = "steps";
                    }
                }
                // 检测"预期结果:"文本（可能是标题或普通文本）
                else if (IsExpectedResultsLine(trimmedLine))
                {
                    // 保存之前的列表内容
                    SaveCurrentListItems(currentStepsNode, currentResultsNode, currentSection, currentListItems);
                    currentListItems.Clear();

                    if (currentLevel3Node != null)
                    {
                        // 创建"预期结果"节点，作为当前用例的子节点
                        currentResultsNode = new TestCase
                        {
                            Title = "预期结果",
                            Level = 4
                        };
                        currentLevel3Node.Children.Add(currentResultsNode);
                        currentSection = "results";
                    }
                }
                // 检测标题（排除"步骤:"和"预期结果:"标题）
                else if (trimmedLine.StartsWith("#") && !IsStepsLine(trimmedLine) && !IsExpectedResultsLine(trimmedLine))
                {
                    // 保存之前的列表内容
                    SaveCurrentListItems(currentStepsNode, currentResultsNode, currentSection, currentListItems);
                    currentListItems.Clear();
                    currentSection = null;
                    currentStepsNode = null;
                    currentResultsNode = null;

                    var level = GetHeaderLevel(trimmedLine);
                    var title = trimmedLine.TrimStart('#').Trim();

                    if (level == 1)
                    {
                        // 一级标题：根节点
                        rootNode = new TestCase
                        {
                            Title = title,
                            Level = level,
                            IsRootNode = true
                        };
                        testCases.Add(rootNode);
                        currentLevel2Node = null;
                        currentLevel3Node = null;
                    }
                    else if (level == 2 && rootNode != null)
                    {
                        // 二级标题：用例集合（根节点下的子节点）
                        var level2Node = new TestCase
                        {
                            Title = title,
                            Level = level
                        };
                        rootNode.Children.Add(level2Node);
                        currentLevel2Node = level2Node;
                        currentLevel3Node = null;
                    }
                    else if (level == 3 && currentLevel2Node != null)
                    {
                        // 三级标题：用例名称（二级标题下的子节点）
                        var level3Node = new TestCase
                        {
                            Title = title,
                            Level = level
                        };
                        currentLevel2Node.Children.Add(level3Node);
                        currentLevel3Node = level3Node;
                    }
                }
                // 检测列表项（- 或 * 开头）
                else if ((trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* ")) &&
                         trimmedLine.Length > 2)
                {
                    var listItem = trimmedLine.Substring(2).Trim();
                    if (!string.IsNullOrEmpty(listItem))
                    {
                        currentListItems.Add(listItem);
                    }
                }
            }

            // 保存最后的列表内容
            SaveCurrentListItems(currentStepsNode, currentResultsNode, currentSection, currentListItems);

            return testCases;
        }

        private int GetHeaderLevel(string line)
        {
            int count = 0;
            foreach (char c in line)
            {
                if (c == '#')
                    count++;
                else
                    break;
            }
            return count;
        }

        private bool IsStepsLine(string line)
        {
            // 检测各种可能的"步骤:"格式
            return line == "#### 步骤:" || 
                   line == "#### 步骤：" ||
                   line == "步骤:" || 
                   line == "步骤：" ||
                   line.EndsWith("步骤:") || 
                   line.EndsWith("步骤：") ||
                   (line.StartsWith("#") && line.Contains("步骤") && (line.EndsWith(":") || line.EndsWith("：")));
        }

        private bool IsExpectedResultsLine(string line)
        {
            // 检测各种可能的"预期结果:"格式
            return line == "#### 预期结果:" || 
                   line == "#### 预期结果：" ||
                   line == "预期结果:" || 
                   line == "预期结果：" ||
                   line.EndsWith("预期结果:") || 
                   line.EndsWith("预期结果：") ||
                   (line.StartsWith("#") && line.Contains("预期结果") && (line.EndsWith(":") || line.EndsWith("：")));
        }

        private void SaveCurrentListItems(TestCase stepsNode, TestCase resultsNode, string section, List<string> listItems)
        {
            if (listItems.Count == 0)
                return;

            if (section == "steps" && stepsNode != null)
            {
                // 将列表项作为"步骤"节点的子节点
                foreach (var item in listItems)
                {
                    stepsNode.Children.Add(new TestCase
                    {
                        Title = item,
                        Level = 5
                    });
                }
            }
            else if (section == "results" && resultsNode != null)
            {
                // 将列表项作为"预期结果"节点的子节点
                foreach (var item in listItems)
                {
                    resultsNode.Children.Add(new TestCase
                    {
                        Title = item,
                        Level = 5
                    });
                }
            }
        }
    }
}
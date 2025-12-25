using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using md_to_test_case.Models;
using Newtonsoft.Json;

namespace md_to_test_case.Services
{
    public class XmindService : IXmindService
    {
        public async Task GenerateXmindFileAsync(List<TestCase> testCases, string outputPath)
        {
            // 生成 XMind JSON 结构
            var sheet = CreateXMindSheet(testCases);
            
            // 创建临时目录
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // JSON 序列化设置
                var jsonSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.None
                };
                
                // 生成 content.json
                var contentJson = JsonConvert.SerializeObject(new[] { sheet }, jsonSettings);
                await File.WriteAllTextAsync(Path.Combine(tempDir, "content.json"), contentJson);
                
                // 生成 manifest.json
                var manifest = new XMindManifest
                {
                    FileEntries = new Dictionary<string, object>
                    {
                        { "content.json", new { } },
                        { "metadata.json", new { } },
                        { "Thumbnails/thumbnail.png", new { } }
                    }
                };
                var manifestJson = JsonConvert.SerializeObject(manifest, jsonSettings);
                await File.WriteAllTextAsync(Path.Combine(tempDir, "manifest.json"), manifestJson);
                
                // 生成 metadata.json
                var metadata = new XMindMetadata
                {
                    DataStructureVersion = "2",
                    Creator = new XMindCreator
                    {
                        Name = "md_to_test_case",
                        Version = "1.0.0"
                    },
                    LayoutEngineVersion = "4"
                };
                var metadataJson = JsonConvert.SerializeObject(metadata, jsonSettings);
                await File.WriteAllTextAsync(Path.Combine(tempDir, "metadata.json"), metadataJson);
                
                // 创建 Thumbnails 目录（如果需要）
                var thumbnailsDir = Path.Combine(tempDir, "Thumbnails");
                Directory.CreateDirectory(thumbnailsDir);
                
                // 打包成 ZIP 文件
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
                
                ZipFile.CreateFromDirectory(tempDir, outputPath);
            }
            finally
            {
                // 清理临时目录
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
        
        private XMindSheet CreateXMindSheet(List<TestCase> testCases)
        {
            var sheetId = Guid.NewGuid().ToString("D");
            var revisionId = Guid.NewGuid().ToString("D");
            var rootTopicId = Guid.NewGuid().ToString("D");
            
            // 创建根主题
            var rootTopic = new XMindTopic
            {
                Id = rootTopicId,
                Class = "topic",
                Title = "测试用例",
                StructureClass = "org.xmind.ui.logic.right",
                Children = new XMindChildren
                {
                    Attached = new List<XMindTopic>()
                },
                Style = new XMindStyle
                {
                    Id = Guid.NewGuid().ToString("D"),
                    Properties = new Dictionary<string, string>
                    {
                        { "fo:color", "#FFFFFFFF" }
                    }
                }
            };
            
            // 添加测试用例节点
            foreach (var testCase in testCases)
            {
                var topic = ConvertTestCaseToTopic(testCase);
                rootTopic.Children!.Attached!.Add(topic);
            }
            
            // 创建主题样式
            var theme = CreateDefaultTheme();
            
            // 创建扩展
            var extensions = new List<XMindExtension>
            {
                new XMindExtension
                {
                    Provider = "org.xmind.ui.skeleton.structure.style",
                    Content = new Dictionary<string, object>
                    {
                        { "centralTopic", "org.xmind.ui.logic.right" }
                    }
                }
            };
            
            return new XMindSheet
            {
                Id = sheetId,
                RevisionId = revisionId,
                Class = "sheet",
                RootTopic = rootTopic,
                Title = "Sheet 1",
                Extensions = extensions,
                Theme = theme
            };
        }
        
        private XMindTopic ConvertTestCaseToTopic(TestCase testCase)
        {
            var attachedTopics = new List<XMindTopic>();
            
            // 递归处理所有子节点（包括"步骤"和"预期结果"节点，它们现在已经是 Children 的一部分）
            foreach (var child in testCase.Children)
            {
                var childTopic = ConvertTestCaseToTopic(child);
                attachedTopics.Add(childTopic);
            }
            
            // 兼容旧格式：如果 Steps 或 ExpectedResults 有数据，也添加它们
            // 这可以保持向后兼容性
            if (testCase.Steps.Count > 0)
            {
                var stepsTopic = new XMindTopic
                {
                    Id = Guid.NewGuid().ToString("D"),
                    Class = "topic",
                    Title = "步骤",
                    TitleUnedited = false,
                    Children = new XMindChildren
                    {
                        Attached = testCase.Steps.Select(step => new XMindTopic
                        {
                            Id = Guid.NewGuid().ToString("D"),
                            Class = "topic",
                            Title = step,
                            TitleUnedited = false
                        }).ToList()
                    }
                };
                attachedTopics.Add(stepsTopic);
            }
            
            if (testCase.ExpectedResults.Count > 0)
            {
                var resultsTopic = new XMindTopic
                {
                    Id = Guid.NewGuid().ToString("D"),
                    Class = "topic",
                    Title = "预期结果",
                    TitleUnedited = false,
                    Children = new XMindChildren
                    {
                        Attached = testCase.ExpectedResults.Select(result => new XMindTopic
                        {
                            Id = Guid.NewGuid().ToString("D"),
                            Class = "topic",
                            Title = result,
                            TitleUnedited = false
                        }).ToList()
                    }
                };
                attachedTopics.Add(resultsTopic);
            }
            
            var topic = new XMindTopic
            {
                Id = Guid.NewGuid().ToString("D"),
                Class = "topic",
                Title = testCase.Title,
                TitleUnedited = false
            };
            
            // 只有在有子节点时才添加 Children
            if (attachedTopics.Count > 0)
            {
                topic.Children = new XMindChildren
                {
                    Attached = attachedTopics
                };
            }
            
            return topic;
        }
        
        private XMindTheme CreateDefaultTheme()
        {
            var themeMapId = Guid.NewGuid().ToString("D");
            var centralTopicId = Guid.NewGuid().ToString("D");
            var mainTopicId = Guid.NewGuid().ToString("D");
            var subTopicId = Guid.NewGuid().ToString("D");
            
            return new XMindTheme
            {
                Map = new XMindThemeMap
                {
                    Id = themeMapId,
                    Properties = new Dictionary<string, string>
                    {
                        { "svg:fill", "#ffffff" },
                        { "multi-line-colors", "#F9423A #F6A04D #F3D321 #00BC7B #486AFF #4D49BE" },
                        { "color-list", "#000229 #1F2766 #52CC83 #4D86DB #99142F #245570" },
                        { "line-tapered", "none" }
                    }
                },
                CentralTopic = new XMindThemeTopic
                {
                    Id = centralTopicId,
                    Properties = new Dictionary<string, string>
                    {
                        { "fo:font-family", "NeverMind" },
                        { "fo:font-size", "28pt" },
                        { "fo:font-weight", "600" },
                        { "fo:font-style", "normal" },
                        { "fo:color", "inherited" },
                        { "fo:text-transform", "manual" },
                        { "fo:text-decoration", "none" },
                        { "fo:text-align", "center" },
                        { "svg:fill", "#000229" },
                        { "fill-pattern", "solid" },
                        { "line-width", "2pt" },
                        { "line-color", "#000229" },
                        { "line-pattern", "solid" },
                        { "border-line-color", "inherited" },
                        { "border-line-width", "0pt" },
                        { "border-line-pattern", "inherited" },
                        { "shape-class", "org.xmind.topicShape.roundedRect" },
                        { "line-class", "org.xmind.branchConnection.roundedfold" },
                        { "arrow-end-class", "org.xmind.arrowShape.none" },
                        { "alignment-by-level", "actived" }
                    }
                },
                MainTopic = new XMindThemeTopic
                {
                    Id = mainTopicId,
                    Properties = new Dictionary<string, string>
                    {
                        { "fo:font-family", "NeverMind" },
                        { "fo:font-size", "18pt" },
                        { "fo:font-weight", "600" },
                        { "fo:font-style", "normal" },
                        { "fo:color", "inherited" },
                        { "fo:text-transform", "manual" },
                        { "fo:text-decoration", "none" },
                        { "fo:text-align", "left" },
                        { "svg:fill", "inherited" },
                        { "fill-pattern", "solid" },
                        { "line-width", "inherited" },
                        { "line-color", "inherited" },
                        { "line-pattern", "inherited" },
                        { "border-line-color", "inherited" },
                        { "border-line-width", "0pt" },
                        { "border-line-pattern", "inherited" },
                        { "shape-class", "org.xmind.topicShape.roundedRect" },
                        { "line-class", "org.xmind.branchConnection.roundedElbow" },
                        { "arrow-end-class", "inherited" },
                        { "alignment-by-level", "inherited" }
                    }
                },
                SubTopic = new XMindThemeTopic
                {
                    Id = subTopicId,
                    Properties = new Dictionary<string, string>
                    {
                        { "fo:font-family", "NeverMind" },
                        { "fo:font-size", "14pt" },
                        { "fo:font-weight", "400" },
                        { "fo:font-style", "normal" },
                        { "fo:color", "inherited" },
                        { "fo:text-transform", "manual" },
                        { "fo:text-decoration", "none" },
                        { "fo:text-align", "left" },
                        { "svg:fill", "inherited" },
                        { "fill-pattern", "solid" },
                        { "line-width", "inherited" },
                        { "line-color", "inherited" },
                        { "line-pattern", "inherited" },
                        { "border-line-color", "inherited" },
                        { "border-line-width", "0pt" },
                        { "border-line-pattern", "inherited" },
                        { "shape-class", "org.xmind.topicShape.roundedRect" },
                        { "line-class", "org.xmind.branchConnection.roundedElbow" },
                        { "arrow-end-class", "inherited" },
                        { "alignment-by-level", "inherited" }
                    }
                }
            };
        }
    }
}
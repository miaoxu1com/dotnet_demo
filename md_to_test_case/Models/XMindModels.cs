using System.Collections.Generic;
using Newtonsoft.Json;

namespace md_to_test_case.Models
{
    // XMind Content JSON 结构
    public class XMindSheet
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("revisionId")]
        public string RevisionId { get; set; } = string.Empty;

        [JsonProperty("class")]
        public string Class { get; set; } = "sheet";

        [JsonProperty("rootTopic")]
        public XMindTopic RootTopic { get; set; } = new();

        [JsonProperty("title")]
        public string Title { get; set; } = "Sheet 1";

        [JsonProperty("extensions")]
        public List<XMindExtension>? Extensions { get; set; }

        [JsonProperty("theme")]
        public XMindTheme? Theme { get; set; }
    }

    public class XMindTopic
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("class")]
        public string Class { get; set; } = "topic";

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("titleUnedited")]
        public bool? TitleUnedited { get; set; }

        [JsonProperty("structureClass")]
        public string? StructureClass { get; set; }

        [JsonProperty("children")]
        public XMindChildren? Children { get; set; }

        [JsonProperty("style")]
        public XMindStyle? Style { get; set; }
    }

    public class XMindChildren
    {
        [JsonProperty("attached")]
        public List<XMindTopic>? Attached { get; set; }
    }

    public class XMindStyle
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, string>? Properties { get; set; }
    }

    public class XMindExtension
    {
        [JsonProperty("provider")]
        public string Provider { get; set; } = string.Empty;

        [JsonProperty("content")]
        public Dictionary<string, object>? Content { get; set; }
    }

    public class XMindTheme
    {
        [JsonProperty("map")]
        public XMindThemeMap? Map { get; set; }

        [JsonProperty("centralTopic")]
        public XMindThemeTopic? CentralTopic { get; set; }

        [JsonProperty("mainTopic")]
        public XMindThemeTopic? MainTopic { get; set; }

        [JsonProperty("subTopic")]
        public XMindThemeTopic? SubTopic { get; set; }
    }

    public class XMindThemeMap
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("properties")]
        public Dictionary<string, string>? Properties { get; set; }
    }

    public class XMindThemeTopic
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("properties")]
        public Dictionary<string, string>? Properties { get; set; }
    }

    // Manifest JSON 结构
    public class XMindManifest
    {
        [JsonProperty("file-entries")]
        public Dictionary<string, object> FileEntries { get; set; } = new();
    }

    // Metadata JSON 结构
    public class XMindMetadata
    {
        [JsonProperty("dataStructureVersion")]
        public string DataStructureVersion { get; set; } = "2";

        [JsonProperty("creator")]
        public XMindCreator Creator { get; set; } = new();

        [JsonProperty("layoutEngineVersion")]
        public string LayoutEngineVersion { get; set; } = "4";
    }

    public class XMindCreator
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "md_to_test_case";

        [JsonProperty("version")]
        public string Version { get; set; } = "1.0.0";
    }
}


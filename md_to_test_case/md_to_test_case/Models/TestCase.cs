using System.Collections.Generic;

namespace md_to_test_case.Models
{
    public class TestCase
    {
        public string Title { get; set; } = string.Empty;
        public int Level { get; set; }
        public List<string> Steps { get; set; } = new();
        public List<string> ExpectedResults { get; set; } = new();
        public List<TestCase> Children { get; set; } = new();
        public bool IsRootNode { get; set; } = false;
    }
}
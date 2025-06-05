using System.Collections.Generic;

namespace XmindToExcelConverter.Models
{
    public class FieldConfig
    {
        public required string Name { get; set; }
        public required string Label { get; set; }
    }

    public class AppConfig
    {
        public required List<FieldConfig> Fields { get; set; }
    }
}
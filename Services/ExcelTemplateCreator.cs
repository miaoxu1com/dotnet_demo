using ClosedXML.Excel;

namespace XmindToExcelConverter.Services;

public class ExcelTemplateCreator
{
    public static void CreateTemplate()
    {
        // Create a new workbook
        var workbook = new XLWorkbook();
        
        // Add a worksheet
        var worksheet = workbook.Worksheets.Add("TestCases");
        
        // Save the template
        workbook.SaveAs("template.xlsx");
    }
} 
using System;
using System.Linq;
using ClosedXML.Excel;

string excelPath = @"C:\Users\Agustin\.gemini\antigravity\scratch\SanPatricioRugby\ExcelReader_Temp\temp_data.xlsx";

using (var workbook = new XLWorkbook(excelPath))
{
    foreach (var worksheet in workbook.Worksheets)
    {
        Console.WriteLine($"--- Sheet: {worksheet.Name} ---");
        var headerRow = worksheet.Row(1);
        int nameCol = worksheet.Name.Contains("JAVIER") ? 10 : (worksheet.Name.Contains("MONICA") ? 3 : -1);
        
        if (nameCol != -1)
        {
            var names = worksheet.RowsUsed()
                .Skip(1)
                .Select(r => r.Cell(nameCol).GetValue<string>()?.Trim().ToUpper())
                .Where(n => !string.IsNullOrEmpty(n) && n != "APELLIDO Y NOMBRE")
                .Distinct()
                .ToList();
            
            Console.WriteLine($"Unique names: {names.Count}");
        }
        
        int maxCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 10;
        Console.WriteLine("Headers:");
        for (int i = 1; i <= maxCol; i++)
        {
            var val = headerRow.Cell(i).GetValue<string>();
            if (!string.IsNullOrEmpty(val))
                Console.WriteLine($"Col {i} ({GetExcelColumnName(i)}): {val}");
        }
        Console.WriteLine();
    }
}

string GetExcelColumnName(int columnNumber)
{
    int dividend = columnNumber;
    string columnName = String.Empty;
    int modulo;

    while (dividend > 0)
    {
        modulo = (dividend - 1) % 26;
        columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
        dividend = (int)((dividend - modulo) / 26);
    }

    return columnName;
}

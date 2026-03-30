using System;
using System.Linq;
using ClosedXML.Excel;

string excelPath = @"C:\Users\Agustin\.gemini\antigravity\scratch\SanPatricioRugby\MONICA DIAZ LISTADO DE SOCIOS DEBITO Y TRASFERENCIAS 2025 (7) (3).xlsx";

using (var workbook = new XLWorkbook(excelPath))
{
    foreach (var worksheet in workbook.Worksheets)
    {
        Console.WriteLine($"--- Sheet: {worksheet.Name} ---");
        var headerRow = worksheet.Row(1);
        for (int i = 1; i <= 25; i++)
        {
            var val = headerRow.Cell(i).GetValue<string>();
            if (!string.IsNullOrEmpty(val))
                Console.WriteLine($"Col {i} ({GetExcelColumnName(i)}): {val}");
        }
        
        Console.WriteLine("Sample rows (first 3):");
        var rows = worksheet.RowsUsed().Skip(1).Take(3);
        foreach (var row in rows)
        {
            Console.WriteLine($"Row {row.RowNumber()}: {string.Join(" | ", row.Cells(1, 25).Select(c => c.GetValue<string>()))}");
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

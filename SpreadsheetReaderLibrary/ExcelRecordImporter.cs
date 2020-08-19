using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SpreadsheetReaderLibrary
{
    /// <summary>
    /// Excel Report Importer class to load Excel data
    /// </summary>
    public class ExcelRecordImporter : IRecordImporter
    {
        private readonly ExcelWorkbook _workbook;

        public ExcelRecordImporter(string fileName)
        {
            var package = new ExcelPackage(new FileInfo(fileName));
            _workbook = package.Workbook;
        }

        public ExcelRecordImporter(Stream stream)
        {
            ExcelPackage package;
            try
            {
                package = new ExcelPackage(stream);
            }
            catch (FormatException)
            {
                throw new Exception("File is empty, corrupt or not an Excel 2010 file (.xlsx)");
            }
            _workbook = package.Workbook;
        }

        public IEnumerable<string> GetDataSetNames()
        {
            return _workbook.Worksheets.Select(worksheet => worksheet.Name);
        }

        public IEnumerable<Dictionary<string, string>> Import(string sheetName)
        {
            var worksheet = _workbook.Worksheets[sheetName];
            var top = worksheet.Dimension.Start.Column;
            var bottom = worksheet.Dimension.End.Row;
            var left = worksheet.Dimension.Start.Row;
            var right = worksheet.Dimension.End.Column;
            var rows = new List<Dictionary<string, string>>();

            // Go through each non-header cell
            for (int y = top + 1; y <= bottom; y++)
            {
                var row = new Dictionary<string, string>();
                // Populate new row with data
                for (int x = left; x <= right; x++)
                {
                    var headerVal = (worksheet.Cells[top, x].Value ?? "").ToString().ToLower();
                    if (headerVal != "" && !row.ContainsKey(headerVal)) row[headerVal] = (worksheet.Cells[y, x].Value ?? "").ToString();
                }
                rows.Add(row);
            }

            return rows;
        }

        public int TotalNumRows
        {
            get
            {
                int numRows = 0;

                int onlyOneSheet = 0;
                foreach (var worksheet in _workbook.Worksheets)
                {

                    if (onlyOneSheet > 0)
                    {
                        break;
                    }
                    numRows += worksheet.Dimension.End.Row - 1;
                    onlyOneSheet++;
                }
                return numRows;
            }
        }
    }
}

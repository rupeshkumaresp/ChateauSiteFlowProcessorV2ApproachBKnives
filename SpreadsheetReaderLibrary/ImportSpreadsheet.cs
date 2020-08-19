using System;
using System.Collections.Generic;
using System.IO;

namespace SpreadsheetReaderLibrary
{
    public class ImportSpreadsheet
    {
        /// <summary>
        /// Load the spreadsheet data, only process the first datasheet
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        public static IEnumerable<Dictionary<string, string>> Import(Stream inputStream)
        {
            var recordImporter = new ExcelRecordImporter(inputStream);

            foreach (var dataSetName in recordImporter.GetDataSetNames())
            {
                try
                {
                    var importedRows = recordImporter.Import(dataSetName);

                    return importedRows;
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to read input spreadsheet: " + ex.Message);
                }
            }

            return null;
        }
    }
}

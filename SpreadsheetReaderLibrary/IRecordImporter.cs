using System.Collections.Generic;

namespace SpreadsheetReaderLibrary
{
    /// <summary>
    /// Record Imported Interface
    /// </summary>
    public interface IRecordImporter
    {
        IEnumerable<string> GetDataSetNames();
        IEnumerable<Dictionary<string, string>> Import(string dataSetName);
        int TotalNumRows { get; }
    }
}

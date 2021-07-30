using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using PicsMeOrderHelper;
using PicsMeOrderHelper.Model;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SpreadsheetReaderLibrary;

namespace PicsMeSiteFlowApp
{
    public class GenerateReportOutputSpreadsheet
    {
        public ExcelPackage Package = new ExcelPackage();
        public ExcelWorksheet Worksheet;

        readonly OrderHelper _orderHelper = new OrderHelper();

     
        private void AddMainHeaderRowelfield(int rowJump)
        {
            // Set up columns
            var headerColumns = new Dictionary<string, int>();

            int icount = 1;

            headerColumns.Add("Order ID", icount);
            icount++;

            headerColumns.Add("Order Reference", icount);
            icount++;

            headerColumns.Add("Order Details Reference", icount);
            icount++;

            headerColumns.Add("BarCode", icount);
            icount++;

            headerColumns.Add("Attribute Design Code", icount);
            icount++;

            headerColumns.Add("Attribute Length", icount);
            icount++;

            headerColumns.Add("Quantity", icount);
            icount++;

            headerColumns.Add("ArtworkUrl", icount);
            icount++;

            // Write column headers
            foreach (var colKvp in headerColumns)
            {
                if (colKvp.Value > 0)
                {
                    Worksheet.Cells[rowJump, colKvp.Value].Value = colKvp.Key;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.HorizontalAlignment =
                        OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.VerticalAlignment =
                        OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Font.Bold = true;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }
            }
        }



        

        private void AddMainHeaderRowPreOrder(int rowJump)
        {
            // Set up columns
            var headerColumns = new Dictionary<string, int>();

            int icount = 1;

            headerColumns.Add("Order ID", icount);
            icount++;

            headerColumns.Add("Order Reference", icount);
            icount++;

            headerColumns.Add("Order Details Reference", icount);
            icount++;

            headerColumns.Add("BarCode", icount);
            icount++;

            headerColumns.Add("Substrate", icount);
            icount++;

            headerColumns.Add("Quantity", icount);
            icount++;

            headerColumns.Add("ArtworkUrl", icount);
            icount++;

            headerColumns.Add("Name", icount);
            icount++;

            headerColumns.Add("Address1", icount);
            icount++;

            headerColumns.Add("Address2", icount);
            icount++;

            headerColumns.Add("Address3", icount);
            icount++;

            headerColumns.Add("Town", icount);
            icount++;

            headerColumns.Add("State", icount);
            icount++;

            headerColumns.Add("Postcode", icount);
            icount++;

            headerColumns.Add("Country", icount);
            icount++;

            headerColumns.Add("Email", icount);
            icount++;

            headerColumns.Add("CompanyName", icount);
            icount++;

            headerColumns.Add("Phone", icount);
            icount++;

            // Write column headers
            foreach (var colKvp in headerColumns)
            {
                if (colKvp.Value > 0)
                {
                    Worksheet.Cells[rowJump, colKvp.Value].Value = colKvp.Key;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.HorizontalAlignment =
                        OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.VerticalAlignment =
                        OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Font.Bold = true;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }
            }
        }

        private void AddMainHeaderRowKnives(int rowJump)
        {
            // Set up columns
            var headerColumns = new Dictionary<string, int>();

            int icount = 1;

            headerColumns.Add("Order ID", icount);
            icount++;

            headerColumns.Add("Order Reference", icount);
            icount++;

            headerColumns.Add("Order Details Reference", icount);
            icount++;

            headerColumns.Add("BarCode", icount);
            icount++;

            headerColumns.Add("Attribute", icount);
            icount++;

            headerColumns.Add("Quantity", icount);
            icount++;

            headerColumns.Add("ArtworkUrl", icount);
            icount++;

            headerColumns.Add("Name", icount);
            icount++;

            headerColumns.Add("Address1", icount);
            icount++;

            headerColumns.Add("Address2", icount);
            icount++;

            headerColumns.Add("Address3", icount);
            icount++;

            headerColumns.Add("Town", icount);
            icount++;

            headerColumns.Add("State", icount);
            icount++;

            headerColumns.Add("Postcode", icount);
            icount++;

            headerColumns.Add("Country", icount);
            icount++;

            headerColumns.Add("Email", icount);
            icount++;

            headerColumns.Add("CompanyName", icount);
            icount++;

            headerColumns.Add("Phone", icount);
            icount++;

            // Write column headers
            foreach (var colKvp in headerColumns)
            {
                if (colKvp.Value > 0)
                {
                    Worksheet.Cells[rowJump, colKvp.Value].Value = colKvp.Key;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.HorizontalAlignment =
                        OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.VerticalAlignment =
                        OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Font.Bold = true;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    Worksheet.Cells[rowJump, colKvp.Value].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }
            }
        }

        public static void SaveStreamToFile(string fileFullPath, Stream stream)
        {
            if (stream.Length == 0) return;

            // Create a FileStream object to write a stream to a file
            using (FileStream fileStream = File.Create(fileFullPath, (int)stream.Length))
            {
                // Fill the bytes[] array with the stream data
                var bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, (int)bytesInStream.Length);

                // Use FileStream object to write to the specified file
                fileStream.Write(bytesInStream, 0, bytesInStream.Length);
            }
        }

    }
}

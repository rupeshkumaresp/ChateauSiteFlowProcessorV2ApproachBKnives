using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using ChateauOrderHelper;
using ChateauOrderHelper.Model;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SpreadsheetReaderLibrary;

namespace ChateauSiteFlowApp
{
    public class GenerateReportOutputSpreadsheet
    {
        public ExcelPackage Package = new ExcelPackage();
        public ExcelWorksheet Worksheet;

        readonly OrderHelper _orderHelper = new OrderHelper();

        public void CreateSpreadSheetBelfield(List<BelfieldModel> belfieldData)
        {
            var name = "BelfieldReport_" + System.DateTime.Now.ToString("dd-MM-yyyy HH_mm_ss");

            if (belfieldData.Count == 0)
                return;
            //CREATE IMPOSTIONS PDFS AND SAVE TO FOLDER AND MARK TO DATABASE THAT IMPOSTIONS DONE

            //GENERATE REPORT
            BuildBelfieldDataSheet(name, belfieldData);

            // Save file and return stream
            var fileName = Path.GetTempFileName();
            Package.SaveAs(new FileInfo(fileName));

            var currentDirectory = Environment.CurrentDirectory;
            if (!Directory.Exists(currentDirectory + @"\" + "Reports"))
            {
                Directory.CreateDirectory(currentDirectory + @"\" + "Reports");
            }

            var path = currentDirectory + @"\" + @"Reports\" + name + ".xlsx";
            SaveStreamToFile(path, new FileStream(fileName, FileMode.Open));

            Package.Dispose();

            var chateauBelfieldReportPath = ConfigurationManager.AppSettings["ChateauBelfieldReportPath"];

            File.Copy(path, chateauBelfieldReportPath + @"\\" + name + ".xlsx");

            EmailHelper.SendBelfieldReportEmail(path);

            MarkBelfieldExtractedOrders(belfieldData);

        }

        private void BuildBelfieldDataSheet(string name, List<BelfieldModel> reportData)
        {
            Worksheet = Package.Workbook.Worksheets.Add(name);
            int rowJump = 1;

            AddMainHeaderRowelfield(rowJump);
            rowJump++;

            var chateauBelfieldMasterPricingPath = ConfigurationManager.AppSettings["ChateauBelfieldMasterPricingPath"];

            ExcelRecordImporter importer = new ExcelRecordImporter(chateauBelfieldMasterPricingPath);

            foreach (var data in reportData)
            {
                int cell = 1;

                Worksheet.Cells[rowJump, cell].Value = data.OrderId;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center; // Alignment is center

                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;


                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.OrderReference;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.OrderDetailsReference;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.BarCode;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.AttributeDesignCode;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;


                Worksheet.Cells[rowJump, cell].Value = data.AttributeLength;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.Quantity;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.ArtworkUrl;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;


                int dataset = 1;
                var nameVal = "";
                var costVal = "";
                foreach (var dataSetName in importer.GetDataSetNames())
                {
                    if (dataset > 1)
                        break;

                    var importedRows = importer.Import(dataSetName);

                    foreach (var importedRow in importedRows)
                    {

                        var designCode = Convert.ToString(importedRow["Design Code".ToLower()]);
                        var longname = importedRow["Name".ToLower()];
                        var cost = importedRow["Cost".ToLower()];

                        //TODO: add column from mapping spreadsheet

                        designCode = designCode.Trim();

                        var designCodeVal = data.AttributeDesignCode.Trim();

                        if (designCode == designCodeVal)
                        {
                            nameVal = longname;
                            costVal = cost;
                            break;
                        }
                    }

                    dataset++;
                }

                Worksheet.Cells[rowJump, cell].Value = nameVal;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = costVal;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;



                rowJump++;
            }

            Worksheet.Column(1).Width = 20;
            Worksheet.Column(2).Width = 25;
            Worksheet.Column(3).Width = 25;
            Worksheet.Column(4).Width = 25;
            Worksheet.Column(5).Width = 25;
            Worksheet.Column(6).Width = 25;
            Worksheet.Column(7).Width = 15;
            Worksheet.Column(8).Width = 65;

        }

        private void MarkBelfieldExtractedOrders(List<BelfieldModel> belfieldData)
        {
            //mark each report as extracted

            foreach (var data in belfieldData)
            {
                _orderHelper.MarkBelfieldSentToProduction(data.Id);
            }

        }

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

        public void CreateSpreadSheetKnives(List<ChateauKnivesReportData> knivesData)
        {
            var name = "Report_" + System.DateTime.Now.ToString("dd-MM-yyyy HH_mm_ss");

            if (knivesData.Count == 0)
                return;

            BuildKnivesDataSheet(name, knivesData);

            // Save file and return stream
            var fileName = Path.GetTempFileName();
            Package.SaveAs(new FileInfo(fileName));

            var currentDirectory = Environment.CurrentDirectory;
            if (!Directory.Exists(currentDirectory + @"\" + "Reports"))
            {
                Directory.CreateDirectory(currentDirectory + @"\" + "Reports");
            }

            var path = currentDirectory + @"\" + @"Reports\" + name + ".xlsx";
            SaveStreamToFile(path, new FileStream(fileName, FileMode.Open));

            Package.Dispose();

            var chateauKnivesReportPath = ConfigurationManager.AppSettings["ChateauKnivesReportPath"];

            File.Copy(path, chateauKnivesReportPath + @"\\" + name + ".xlsx");

            EmailHelper.SendKnivesReportEmail(path);

            MarkExtractedKnivesOrders(knivesData);

        }

        private void BuildKnivesDataSheet(string name, List<ChateauKnivesReportData> reportData)
        {
            Worksheet = Package.Workbook.Worksheets.Add(name);
            int rowJump = 1;

            AddMainHeaderRowKnives(rowJump);
            rowJump++;

            foreach (var data in reportData)
            {
                int cell = 1;

                Worksheet.Cells[rowJump, cell].Value = data.OrderId;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center

                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;


                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.OrderReference;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.OrderDetailsReference;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.BarCode;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.Attribute;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.Quantity;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.ArtworkUrl;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.CustomerName;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.CustomerAddress1;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.CustomerAddress2;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.CustomerAddress3;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.CustomerTown;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.CustomerState;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.CustomerPostcode;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.CustomerCountry;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.CustomerEmail;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.CustomerCompanyName;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                Worksheet.Cells[rowJump, cell].Value = data.CustomerPhone;
                Worksheet.Cells[rowJump, cell].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Alignment is center
                Worksheet.Cells[rowJump, cell].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                Worksheet.Cells[rowJump, cell].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                cell++;

                rowJump++;
            }

            Worksheet.Column(1).Width = 15;
            Worksheet.Column(2).Width = 20;
            Worksheet.Column(3).Width = 25;
            Worksheet.Column(4).Width = 15;
            Worksheet.Column(5).Width = 35;
            Worksheet.Column(6).Width = 10;
            Worksheet.Column(7).Width = 55;
            Worksheet.Column(8).Width = 15;
            Worksheet.Column(9).Width = 20;
            Worksheet.Column(10).Width = 20;
            Worksheet.Column(11).Width = 20;
            Worksheet.Column(12).Width = 20;
            Worksheet.Column(13).Width = 20;
            Worksheet.Column(14).Width = 20;
            Worksheet.Column(15).Width = 20;
            Worksheet.Column(16).Width = 30;
            Worksheet.Column(17).Width = 20;
            Worksheet.Column(18).Width = 20;
        }

        private void MarkExtractedKnivesOrders(List<ChateauKnivesReportData> knivesData)
        {
            //mark each report as extracted

            foreach (var knife in knivesData)
            {
                _orderHelper.MarkKnifeSentToProduction(knife.Id);
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

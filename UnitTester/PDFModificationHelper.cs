using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Ghostscript.NET.Processor;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Image = iTextSharp.text.Image;
using Rectangle = iTextSharp.text.Rectangle;



namespace ChateauSiteFlowApp
{
    /// <summary>
    /// EXTEND PDF – ADD MIRRORED BARCODE
    /// </summary>
    public class PdfModificationHelper
    {
        static string _pdfPath = ConfigurationManager.AppSettings["WorkingDirectory"] + ConfigurationManager.AppSettings["ServiceFolderPath"] + @"PDFs/";

        static string AsposeLicense = ConfigurationManager.AppSettings["WorkingDirectory"] + ConfigurationManager.AppSettings["ServiceFolderPath"] + @"License/Aspose.Pdf.lic";

        public void AddBarcodeImage(string path, string fileName, string substrateName, string barcode, string orderId, string quantity)
        {
            var barcodeImg = path + @"modified/" + barcode + "_barcode_Mirror.jpg";

            if (substrateName == "Jute Shopper")
            {
                barcodeImg = path + @"modified/" + barcode + "_barcode_Normal.jpg";
            }

            using (Stream inputPdfStream =
                new FileStream(path + @"modified/" + "extended_" + fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (Stream inputImageStream = new FileStream(barcodeImg, FileMode.Open,
                FileAccess.Read, FileShare.Read))
            using (Stream outputPdfStream = new FileStream(path + @"modified/" + orderId + "_" + fileName, FileMode.Create,
                FileAccess.Write, FileShare.None))
            {
                var reader = new PdfReader(inputPdfStream);
                var stamper = new PdfStamper(reader, outputPdfStream);
                var pdfContentByte = stamper.GetOverContent(1);

                Rectangle rect = reader.GetCropBox(1);

                Image image = Image.GetInstance(inputImageStream);

                var width = Utilities.MillimetersToPoints(80);
                var height = Utilities.MillimetersToPoints(36);


                image.ScaleToFit(Convert.ToInt32(width),
                    Convert.ToInt32(height));

                if (substrateName == "Jute Shopper")
                    image.SetAbsolutePosition(rect.Width / 2 - 140, rect.Height - 250);
                else
                    image.SetAbsolutePosition(rect.Width - width, rect.Height / 4);

                image.Rotation = 0f;

                image.RotationDegrees = 0f;

                pdfContentByte.AddImage(image);

                stamper.Close();
            }
            var flatening = true;

            if (substrateName == "Jute Shopper" || substrateName == "Candle-TheOrangery" || substrateName == "Candle-WalledGarden" || substrateName == "Mini Apron")
                flatening = false;

            if (flatening)
                FlattenPdfFile(path, fileName, orderId);

            try
            {
                File.Delete(path + @"modified/" + "extended_" + fileName);
                File.Delete(path + @"modified/" + orderId + "_barcode.PDF");
                File.Delete(path + @"modified/" + orderId + "_barcode.TIFF");
                File.Delete(path + @"modified/" + orderId + "_barcode_Mirror.JPG");
            }
            catch
            {
            }
        }

        private static void FlattenPdfFile(string path, string fileName, string orderId)
        {
            //convert PDF to psot script
            var filePath = path + @"modified/" + orderId + "_" + fileName;

            var unflattenFile = path + @"modified/" + "unflatten_" + orderId + "_" + fileName;

            File.Copy(filePath, unflattenFile, true);

            File.Delete(filePath);

            var fileNameWithoutExtn = Path.GetFileNameWithoutExtension(filePath);

            var psFilePath = _pdfPath + fileNameWithoutExtn + ".ps";

            using (GhostscriptProcessor processor = new GhostscriptProcessor())
            {
                List<string> switches = new List<string>();

                switches.Add("-dNOPAUSE");
                switches.Add("-dBATCH");

                switches.Add("-sDEVICE=ps2write");
                switches.Add("-sOutputFile=" + psFilePath);
                switches.Add("-f");
                switches.Add(unflattenFile);

                processor.StartProcessing(switches.ToArray(), null);
            }

            using (GhostscriptProcessor processor = new GhostscriptProcessor())
            {
                List<string> switches = new List<string>();

                switches.Add("-dNOPAUSE");
                switches.Add("-dBATCH");

                switches.Add("-sDEVICE=pdfwrite");
                switches.Add("-sOutputFile=" + filePath);
                switches.Add("-f");
                switches.Add(psFilePath);

                processor.StartProcessing(switches.ToArray(), null);
            }
        }

        public void CreateBarcodeMirrorImage(string substrateName, string barcode, string orderId, string quantity)
        {
            var fixedHeight = 23;

            BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

            iTextSharp.text.Font font = new iTextSharp.text.Font(bf, 10, iTextSharp.text.Font.NORMAL);

            var width = Utilities.MillimetersToPoints(80);
            var height = Utilities.MillimetersToPoints(36);

            var doc = new Document(new Rectangle(width, height), 0, 0, 0, 0);

            var output = new FileStream(_pdfPath + @"modified/" + orderId + "_barcode.pdf", FileMode.Create);

            var writer = PdfWriter.GetInstance(doc, output);
            PdfContentByte cb = new PdfContentByte(writer);

            doc.Open();

            PdfPTable boxTable = new PdfPTable(1);
            boxTable.DefaultCell.Border = 0;
            boxTable.WidthPercentage = 80;

            float[] widths = new float[] { 226f };
            boxTable.SetWidths(widths);


            PdfPCell cell11 = new PdfPCell();
            cell11.AddElement(new Paragraph(new Chunk(substrateName.ToUpper(), font)));
            cell11.BorderWidthTop = 0;

            cell11.BorderWidthBottom = 0;
            cell11.BorderWidthLeft = 0;
            cell11.BorderWidthRight = 0;
            cell11.FixedHeight = fixedHeight;

            boxTable.AddCell(cell11);

            boxTable.CompleteRow();
            //New Row Added

            PdfPCell cell21 = new PdfPCell();
            cell21.BorderWidthTop = 0;
            cell21.BorderWidthBottom = 0;
            cell21.BorderWidthRight = 0;
            cell21.BorderWidthLeft = 0;
            cell21.FixedHeight = fixedHeight;

            Barcode128 code128 = new Barcode128
            {
                CodeType = Barcode.CODE128,
                ChecksumText = true,
                GenerateChecksum = true,
                StartStopText = true,
                Code = barcode
            };


            var bm = new Bitmap(code128.CreateDrawingImage(Color.Black, Color.White));

            cell21.AddElement(Image.GetInstance(bm, ImageFormat.Jpeg));

            boxTable.AddCell(cell21);

            boxTable.CompleteRow();

            PdfPCell cell31 = new PdfPCell();
            cell31.AddElement(new Paragraph(new Chunk(barcode, font)));
            cell31.BorderWidthTop = 0;
            cell31.BorderWidthBottom = 0;
            cell31.BorderWidthLeft = 0;
            cell31.BorderWidthRight = 0;
            cell31.FixedHeight = fixedHeight;

            boxTable.AddCell(cell31);

            boxTable.CompleteRow();

            PdfPCell cell41 = new PdfPCell();
            cell41.AddElement(new Paragraph(new Chunk("ORDER ID: " + orderId + " | QTY:" + quantity, font)));
            cell41.BorderWidthTop = 0;

            cell41.BorderWidthBottom = 0;
            cell41.BorderWidthLeft = 0;
            cell41.BorderWidthRight = 0;
            cell41.FixedHeight = fixedHeight;

            boxTable.AddCell(cell41);

            boxTable.CompleteRow();

            doc.Add(boxTable);

            doc.Close();

            var outputTiff = _pdfPath + @"modified/" + barcode + "_barcode.tiff";

            ExtractImageFromPdf(outputTiff, _pdfPath + @"modified/" + orderId + "_barcode.pdf");

            if (substrateName == "Jute Shopper")
            {
                GetNormalImage(outputTiff, Path.GetFileNameWithoutExtension(outputTiff));
            }
            else
            {
                GetMirrorImage(outputTiff, Path.GetFileNameWithoutExtension(outputTiff));
            }

        }

        public void ExtendPdfJuteShopper(string path, string fileName)
        {
            string src = path + @"original\" + fileName;
            string dest = path + @"modified\extended_" + fileName;

            File.Copy(src, dest);

            return;

            using (PdfReader pdfReader = new PdfReader(src))
            {
                PdfDictionary pageDict;
                PdfArray cropBox;
                PdfArray mediaBox;

                int pageCount = pdfReader.NumberOfPages;
                Rectangle rect = pdfReader.GetCropBox(1);

                var widthPoints = Convert.ToInt32(Math.Round(Utilities.MillimetersToPoints(256)));
                var heightPoints = Convert.ToInt32(Math.Round(Utilities.MillimetersToPoints(36)));

                int deviationY = 90;
                int deviationX = 150;

                for (int i = 1; i <= pageCount; i++)
                {
                    pageDict = pdfReader.GetPageN(i);
                    cropBox = pageDict.GetAsArray(PdfName.CROPBOX);
                    mediaBox = pageDict.GetAsArray(PdfName.MEDIABOX);

                    if (cropBox != null)
                    {
                        cropBox[0] = new PdfNumber(deviationX);
                        cropBox[1] = new PdfNumber(deviationY);
                        cropBox[2] = new PdfNumber(widthPoints + deviationX);
                        cropBox[3] = new PdfNumber(heightPoints + deviationY);
                        pageDict.Put(PdfName.CROPBOX, cropBox);
                    }

                    if (mediaBox != null)
                    {
                        mediaBox[0] = new PdfNumber(deviationX);
                        mediaBox[1] = new PdfNumber(deviationY);
                        mediaBox[2] = new PdfNumber(widthPoints + deviationX);
                        mediaBox[3] = new PdfNumber(heightPoints + deviationY);
                        pageDict.Put(PdfName.MEDIABOX, mediaBox);
                    }
                }

                PdfStamper stamper = new PdfStamper(pdfReader, new FileStream(dest, FileMode.Create));
                stamper.Close();
            }
        }

        public void ExtendPdf(string path, string fileName)
        {
            string src = path + @"original\" + fileName;
            string dest = path + @"modified\extended_" + fileName;

            using (PdfReader pdfReader = new PdfReader(src))
            {
                PdfDictionary pageDict;
                PdfArray cropBox;
                PdfArray mediaBox;

                int pageCount = pdfReader.NumberOfPages;
                Rectangle rect = pdfReader.GetCropBox(1);

                var widthMillExtended = Convert.ToInt32(Math.Round(Utilities.PointsToMillimeters(rect.Width))) + 80;

                var widthPoints = Convert.ToInt32(Math.Round(Utilities.MillimetersToPoints(widthMillExtended)));

                var heightPoints = rect.Height;

                for (int i = 1; i <= pageCount; i++)
                {
                    pageDict = pdfReader.GetPageN(i);
                    cropBox = pageDict.GetAsArray(PdfName.CROPBOX);
                    mediaBox = pageDict.GetAsArray(PdfName.MEDIABOX);

                    if (cropBox != null)
                    {
                        cropBox[0] = new PdfNumber(0);
                        cropBox[1] = new PdfNumber(0);
                        cropBox[2] = new PdfNumber(widthPoints);
                        cropBox[3] = new PdfNumber(heightPoints);
                        pageDict.Put(PdfName.CROPBOX, cropBox);
                    }

                    if (mediaBox != null)
                    {
                        mediaBox[0] = new PdfNumber(0);
                        mediaBox[1] = new PdfNumber(0);
                        mediaBox[2] = new PdfNumber(widthPoints);
                        mediaBox[3] = new PdfNumber(heightPoints);
                        pageDict.Put(PdfName.MEDIABOX, mediaBox);
                    }
                }

                PdfStamper stamper = new PdfStamper(pdfReader, new FileStream(dest, FileMode.Create));
                stamper.Close();
            }
        }

        public void GetMirrorImage(string outputFile, string filenameWithoutExtension)
        {
            var bitmap1 = (Bitmap)Bitmap.FromFile(outputFile);

            bitmap1.RotateFlip(RotateFlipType.Rotate180FlipY);

            bitmap1.Save(_pdfPath + @"modified\" + filenameWithoutExtension + "_Mirror.jpg", ImageFormat.Jpeg);
        }

        public void GetNormalImage(string outputFile, string filenameWithoutExtension)
        {
            var bitmap1 = (Bitmap)Bitmap.FromFile(outputFile);

            bitmap1.Save(_pdfPath + @"modified\" + filenameWithoutExtension + "_Normal.jpg", ImageFormat.Jpeg);

        }

        public static void ExtractImageFromPdf(string outputFile, string inputFile)
        {
            List<string> switches = new List<string>();
            switches.Add(string.Empty);

            // set required switches
            switches.Add("-sDEVICE=tiff12nc");
            switches.Add("-dBATCH");
            switches.Add("-r1000");
            switches.Add("-dNOPAUSE");

            switches.Add("-sOutputFile=" + outputFile);
            switches.Add("-f");
            switches.Add(inputFile);

            // create a new instance of the GhostscriptProcessor
            using (GhostscriptProcessor processor = new GhostscriptProcessor())
            {
                // start processing pdf file
                processor.StartProcessing(switches.ToArray(), null);
            }
        }

        /// <summary>
        ///  PDF modifications & update the json with new PDF path to database
        /// </summary>
        public void PdfModifications(string file, string substrateName, string barcode, string orderId, string qty)
        {
            CreateBarcodeMirrorImage(substrateName, barcode, orderId, qty);

            if (substrateName == "Jute Shopper")
                ExtendPdfJuteShopper(_pdfPath, Path.GetFileName(file));
            else
                ExtendPdf(_pdfPath, Path.GetFileName(file));

            AddBarcodeImage(_pdfPath, Path.GetFileName(file), substrateName, barcode, orderId, qty);
        }

        internal void ChateauCandleLabelGeneration(string labelFileName, string substrate, string orderbarcode, string orderorderId, string qtyString)
        {
            //Generate  label of size  54x25mm

            var fixedHeight = 13;

            BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

            iTextSharp.text.Font font = new iTextSharp.text.Font(bf, 6, iTextSharp.text.Font.NORMAL);

            var width = Utilities.MillimetersToPoints(54);
            var height = Utilities.MillimetersToPoints(25);

            var doc = new Document(new Rectangle(width, height), 0, 0, 0, 0);

            var output = new FileStream(labelFileName, FileMode.Create);

            var writer = PdfWriter.GetInstance(doc, output);
            PdfContentByte cb = new PdfContentByte(writer);

            doc.Open();

            PdfPTable boxTableBarcode = new PdfPTable(1);
            boxTableBarcode.DefaultCell.Border = 0;
            boxTableBarcode.WidthPercentage = 100;

            float[] widths = new float[] { 452f };
            boxTableBarcode.SetWidths(widths);

            PdfPCell cell11Border = new PdfPCell();
            cell11Border.BorderWidthTop = 0;
            cell11Border.BorderWidthBottom = 0;
            cell11Border.BorderWidthRight = 0;
            cell11Border.BorderWidthLeft = 0;
            cell11Border.FixedHeight = 18;
            cell11Border.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;

            cell11Border.PaddingLeft = 35;
            Barcode128 code128 = new Barcode128
            {
                CodeType = Barcode.CODE128,
                ChecksumText = true,
                GenerateChecksum = true,
                StartStopText = true,
                Code = orderbarcode
            };


            //code128.BarHeight = (code128.BarcodeSize.Height) * (float)(1.20);

            var bm = new Bitmap(code128.CreateDrawingImage(Color.Black, Color.White));


            cell11Border.AddElement(Image.GetInstance(bm, ImageFormat.Jpeg));

            boxTableBarcode.AddCell(cell11Border);

            boxTableBarcode.CompleteRow();

            PdfPTable boxTable = new PdfPTable(2);
            boxTable.DefaultCell.Border = 0;
            boxTable.WidthPercentage = 100;

            widths = new float[] { 226f, 226f };
            boxTable.SetWidths(widths);



            PdfPCell cell21 = new PdfPCell();
            cell21.AddElement(new Paragraph(new Chunk("Order Number:", font)));
            cell21.BorderWidthTop = 0;
            cell21.Colspan = 1;
            cell21.BorderWidthBottom = 0;
            cell21.BorderWidthLeft = 0;
            cell21.BorderWidthRight = 0;
            cell21.FixedHeight = fixedHeight;
            //cell21.PaddingTop = -5;

            boxTable.AddCell(cell21);


            PdfPCell cell22 = new PdfPCell();
            cell22.AddElement(new Paragraph(new Chunk(orderorderId, font)));
            cell22.BorderWidthTop = 0;
            cell22.Colspan = 1;
            cell22.BorderWidthBottom = 0;
            cell22.BorderWidthLeft = 0;
            cell22.BorderWidthRight = 0;
            cell22.FixedHeight = fixedHeight;
            //cell22.PaddingTop = -5;

            boxTable.AddCell(cell22);

            boxTable.CompleteRow();


            PdfPCell cell31 = new PdfPCell();
            cell31.AddElement(new Paragraph(new Chunk("Barcode:", font)));
            cell31.BorderWidthTop = 0;
            cell31.Colspan = 1;
            cell31.BorderWidthBottom = 0;
            cell31.BorderWidthLeft = 0;
            cell31.BorderWidthRight = 0;
            cell31.FixedHeight = fixedHeight;
            //cell31.PaddingTop = -10;

            boxTable.AddCell(cell31);


            PdfPCell cell32 = new PdfPCell();
            cell32.AddElement(new Paragraph(new Chunk(orderbarcode, font)));
            cell32.BorderWidthTop = 0;
            cell32.Colspan = 1;
            cell32.BorderWidthBottom = 0;
            cell32.BorderWidthLeft = 0;
            cell32.BorderWidthRight = 0;
            cell32.FixedHeight = fixedHeight;
            //cell32.PaddingTop = -10;

            boxTable.AddCell(cell32);

            boxTable.CompleteRow();


            PdfPCell cell41 = new PdfPCell();
            cell41.AddElement(new Paragraph(new Chunk("Scent:", font)));
            cell41.BorderWidthTop = 0;
            cell41.Colspan = 1;
            cell41.BorderWidthBottom = 0;
            cell41.BorderWidthLeft = 0;
            cell41.BorderWidthRight = 0;
            cell41.FixedHeight = fixedHeight;
            //cell41.PaddingTop = -15;
            boxTable.AddCell(cell41);


            PdfPCell cell42 = new PdfPCell();
            cell42.AddElement(new Paragraph(new Chunk(substrate, font)));
            cell42.BorderWidthTop = 0;
            cell42.Colspan = 1;
            cell42.BorderWidthBottom = 0;
            cell42.BorderWidthLeft = 0;
            cell42.BorderWidthRight = 0;
            cell42.FixedHeight = fixedHeight;
            //cell42.PaddingTop = -15;
            boxTable.AddCell(cell42);

            boxTable.CompleteRow();

            //New Row Added


            PdfPCell cell51 = new PdfPCell();
            cell51.AddElement(new Paragraph(new Chunk("Qty:", font)));
            cell51.BorderWidthTop = 0;
            cell51.Colspan = 1;
            cell51.BorderWidthBottom = 0;
            cell51.BorderWidthLeft = 0;
            cell51.BorderWidthRight = 0;
            cell51.FixedHeight = fixedHeight;
            //cell51.PaddingTop = -20;
            boxTable.AddCell(cell51);


            PdfPCell cell52 = new PdfPCell();
            cell52.AddElement(new Paragraph(new Chunk(qtyString, font)));
            cell52.BorderWidthTop = 0;
            cell52.Colspan = 1;
            cell52.BorderWidthBottom = 0;
            cell52.BorderWidthLeft = 0;
            cell52.BorderWidthRight = 0;
            cell52.FixedHeight = fixedHeight;
            //cell52.PaddingTop = -20;
            boxTable.AddCell(cell52);

            boxTable.CompleteRow();

            doc.Add(boxTableBarcode);

            doc.Add(boxTable);

            doc.Close();

        }

        internal void RotatePDF(string orderfileName, string destFilename, int DEGREE)
        {
            using (FileStream outStream = new FileStream(destFilename, FileMode.Create))
            {
                iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(orderfileName);
                iTextSharp.text.pdf.PdfStamper stamper = new iTextSharp.text.pdf.PdfStamper(reader, outStream);

                iTextSharp.text.pdf.PdfDictionary pageDict = reader.GetPageN(1);
                int desiredRot = DEGREE; // 90 degrees clockwise from what it is now
                iTextSharp.text.pdf.PdfNumber rotation = pageDict.GetAsNumber(iTextSharp.text.pdf.PdfName.ROTATE);


                pageDict.Put(iTextSharp.text.pdf.PdfName.ROTATE, new iTextSharp.text.pdf.PdfNumber(desiredRot));

                stamper.Close();
            }
        }

        public string ChateauStationeryPDFModifications(string orderorderId, string inputPDFPath, string code, string StationeryStyle, string StationeryType, string customerName)
        {

            //50time file copy
            var directory = Path.GetDirectoryName(inputPDFPath);
            var fileName = Path.GetFileNameWithoutExtension(inputPDFPath);

            List<string> clonedFiles = new List<string>();
            for (int i = 1; i <= 50; i++)
            {

                var newFileName = fileName + "-" + i.ToString() + ".PDF";

                File.Copy(inputPDFPath, Path.Combine(directory, newFileName), true);

                clonedFiles.Add(Path.Combine(directory, newFileName));
            }

            var coverPdfFile = "";//Get based on stationery style and type

            var staticPdfPath = ConfigurationManager.AppSettings["StaticPDFPath"];

            if (code == "Stationery")
            {
                var ChateauStationeryBasePath = Path.Combine(staticPdfPath, "Chateau-Stationery");

                ChateauStationeryBasePath = Path.Combine(ChateauStationeryBasePath, StationeryType);

                coverPdfFile = ChateauStationeryBasePath + "//" + StationeryStyle + ".PDF";
            }

            //Apply additional text to cover page from attribute

            var modifiedCoverPdfFile = Path.Combine(directory, orderorderId + "-StationeryCoverStyle.PDF");

            ApplyAdditionalTextToCover(orderorderId, coverPdfFile, modifiedCoverPdfFile, customerName);

            //merge Files

            clonedFiles.Insert(0, modifiedCoverPdfFile);

            var output = Path.Combine(directory, orderorderId + "-Stationery-Output.PDF");
            Merge(clonedFiles, output);

            for (int i = 1; i <= 50; i++)
            {
                var newFileName = fileName + "-" + i.ToString() + ".PDF";
                File.Delete(Path.Combine(directory, newFileName));
            }
            return output;
        }
        public string ChateauStationerySetPDFModifications(string orderorderId, string inputPDFPath, string code, string StationeryStyle, string StationeryType, string customerName)
        {
            //50time file copy
            var directory = Path.GetDirectoryName(inputPDFPath);
            var fileName = Path.GetFileNameWithoutExtension(inputPDFPath);

            List<string> clonedFiles = new List<string>();
            for (int i = 1; i <= 50; i++)
            {
                var newFileName = fileName + "-" + i.ToString() + ".PDF";

                File.Copy(inputPDFPath, Path.Combine(directory, newFileName), true);

                clonedFiles.Add(Path.Combine(directory, newFileName));
            }

            var coverPdfFile = "";//Get based on stationery style and type
            //Apply additional text to cover page from attribute

            var staticPdfPath = ConfigurationManager.AppSettings["StaticPDFPath"];

            if (code == "StationerySet")
            {
                var ChateauStationerySetBasePath = Path.Combine(staticPdfPath, "Chateau-StationerySet");

                ChateauStationerySetBasePath = Path.Combine(ChateauStationerySetBasePath, StationeryType);

                coverPdfFile = ChateauStationerySetBasePath + "//" + StationeryStyle + ".PDF";
            }

            var modifiedCoverPdfFile = Path.Combine(directory, orderorderId + "-StationeryCoverStyle.PDF");

            ApplyAdditionalTextToCover(orderorderId, coverPdfFile, modifiedCoverPdfFile, customerName);

            //merge Files

            clonedFiles.Insert(0, modifiedCoverPdfFile);

            var output = Path.Combine(directory, orderorderId + "-StationerySet-Output.PDF");
            Merge(clonedFiles, output);

            for (int i = 1; i <= 50; i++)
            {
                var newFileName = fileName + "-" + i.ToString() + ".PDF";
                File.Delete(Path.Combine(directory, newFileName));
            }
            return output;
        }

        public void SelectPages(string inputPdf, string pageSelection, string outputPdf)
        {
            using (PdfReader reader = new PdfReader(inputPdf))
            {
                reader.SelectPages(pageSelection);

                using (PdfStamper stamper = new PdfStamper(reader, File.Create(outputPdf)))
                {
                    stamper.Close();
                }
            }
        }


        public void ApplyAdditionalTextToCover(string orderorderId, string coverPdfFile, string modifiedCoverPdfFile, string customerName)
        {
            if (File.Exists(coverPdfFile))
            {
                File.Copy(coverPdfFile, modifiedCoverPdfFile, true);

                var orderID = orderorderId;//.TrimStart('0');

                ReplaceTextInPDF(coverPdfFile, modifiedCoverPdfFile, "#ORDER", orderID, "#name", customerName);

            }
        }


        private void ReplaceTextInPDF(String input, String result, string FindText1, String newText1, string FindText2, String newText2)
        {
            Aspose.Pdf.License license = new Aspose.Pdf.License();
            license.SetLicense(AsposeLicense);

            var shortName = Path.GetFileNameWithoutExtension(result);
            var dir = Path.GetDirectoryName(result);

            // Open document
            Aspose.Pdf.Document pdfDocument = new Aspose.Pdf.Document(input);

            // Create TextAbsorber object to find all instances of the input search phrase
            Aspose.Pdf.Text.TextFragmentAbsorber textFragmentAbsorber = new Aspose.Pdf.Text.TextFragmentAbsorber(FindText1);

            // Accept the absorber for all the pages
            pdfDocument.Pages.Accept(textFragmentAbsorber);

            // Get the extracted text fragments
            Aspose.Pdf.Text.TextFragmentCollection textFragmentCollection = textFragmentAbsorber.TextFragments;

            // Loop through the fragments
            foreach (Aspose.Pdf.Text.TextFragment textFragment in textFragmentCollection)
            {
                // Update text and other properties
                textFragment.Text = newText1;
            }

            // Create TextAbsorber object to find all instances of the input search phrase
            textFragmentAbsorber = new Aspose.Pdf.Text.TextFragmentAbsorber(FindText2);

            // Accept the absorber for all the pages
            pdfDocument.Pages.Accept(textFragmentAbsorber);

            // Get the extracted text fragments
            textFragmentCollection = textFragmentAbsorber.TextFragments;

            // Loop through the fragments
            foreach (Aspose.Pdf.Text.TextFragment textFragment in textFragmentCollection)
            {
                // Update text and other properties
                textFragment.Text = newText2;
            }

            pdfDocument.Save(result);

        }


        public static void Merge(List<String> InFiles, String OutFile)
        {
            using (FileStream stream = new FileStream(OutFile, FileMode.Create))
            using (Document doc = new Document())
            using (PdfCopy pdf = new PdfCopy(doc, stream))
            {
                doc.Open();

                PdfReader reader = null;
                PdfImportedPage page = null;

                //fixed typo
                InFiles.ForEach(file =>
                {
                    reader = new PdfReader(file);

                    for (int i = 0; i < reader.NumberOfPages; i++)
                    {
                        page = pdf.GetImportedPage(reader, i + 1);
                        pdf.AddPage(page);
                    }

                    pdf.FreeReader(reader);
                    reader.Close();
                });
            }
        }

    }
}
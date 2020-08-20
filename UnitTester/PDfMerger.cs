using System;
using System.Collections.Generic;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace ChateauSiteFlowApp
{
    public static class PdfMerger
    {
        /// <summary>
        /// Merge pdf files.
        /// </summary>
        /// <param name="sourceFiles">PDF files being merged.</param>
        /// <returns></returns>

        public static byte[] MergeFiles(List<byte[]> sourceFiles)
        {
            Document document = new Document();
            MemoryStream output = new MemoryStream();
            try
            {
                try
                {
                    // Initialize pdf writer
                    PdfWriter writer = PdfWriter.GetInstance(document, output);
                    writer.PageEvent = new PdfPageEvents();

                    // Open document to write
                    document.Open();
                    PdfContentByte content = writer.DirectContent;

                    // Iterate through all pdf documents
                    for (int fileCounter = 0; fileCounter < sourceFiles.Count; fileCounter++)
                    {
                        // Create pdf reader
                        PdfReader reader = new PdfReader(sourceFiles[fileCounter]);
                        int numberOfPages = reader.NumberOfPages;

                        // Iterate through all pages
                        for (int currentPageIndex = 1; currentPageIndex <= numberOfPages; currentPageIndex++)
                        {
                            GC.Collect();
                            // Determine page size for the current page
                            document.SetPageSize(reader.GetPageSizeWithRotation(currentPageIndex));
                            // Create page
                            document.NewPage();
                            PdfImportedPage importedPage = writer.GetImportedPage(reader, currentPageIndex);
                            // Determine page orientation
                            int pageOrientation = reader.GetPageRotation(currentPageIndex);
                            if ((pageOrientation == 90) || (pageOrientation == 270))
                            {
                                content.AddTemplate(importedPage, 0, -1f, 1f, 0, 0, reader.GetPageSizeWithRotation(currentPageIndex).Height);
                            }
                            else
                            {
                                content.AddTemplate(importedPage, 1f, 0, 0, 1f, 0, 0);
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception("There has an unexpected exception occured during the pdf merging process.", exception);
                }
            }
            finally
            {
                GC.Collect();
                document.Close();
            }
            return output.GetBuffer();
        }
    }

    /// <summary>
    /// Implements custom page events.
    /// </summary>
    internal class PdfPageEvents : IPdfPageEvent
    {
        #region members

        private BaseFont _baseFont = null;
        private PdfContentByte _content;

        #endregion

        #region IPdfPageEvent Members

        public void OnOpenDocument(PdfWriter writer, Document document)
        {
            _baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            _content = writer.DirectContent;
        }

        public void OnStartPage(PdfWriter writer, Document document)
        {
        }

        public void OnEndPage(PdfWriter writer, Document document)
        {
            //// Write header text
            //string headerText = "PDF Merger by Smart-Soft";
            //_content.BeginText();
            //_content.SetFontAndSize(_baseFont, 8);
            //_content.SetTextMatrix(GetCenterTextPosition(headerText, writer), writer.PageSize.Height - 10);
            //_content.ShowText(headerText);
            //_content.EndText();

            //// Write footer text (page numbers)
            //string text = "Page " + writer.PageNumber;
            //_content.BeginText();
            //_content.SetFontAndSize(_baseFont, 8);
            //_content.SetTextMatrix(GetCenterTextPosition(text, writer), 10);
            //_content.ShowText(text);
            //_content.EndText();
        }

        public void OnCloseDocument(PdfWriter writer, Document document)
        {
        }

        public void OnParagraph(PdfWriter writer, Document document, float paragraphPosition)
        {
        }

        public void OnParagraphEnd(PdfWriter writer, Document document, float paragraphPosition)
        {
        }

        public void OnChapter(PdfWriter writer, Document document, float paragraphPosition, Paragraph title)
        {
        }

        public void OnChapterEnd(PdfWriter writer, Document document, float paragraphPosition)
        {
        }

        public void OnSection(PdfWriter writer, Document document, float paragraphPosition, int depth, Paragraph title)
        {
        }

        public void OnSectionEnd(PdfWriter writer, Document document, float paragraphPosition)
        {
        }

        public void OnGenericTag(PdfWriter writer, Document document, Rectangle rect, string text)
        {
        }

        #endregion

        private float GetCenterTextPosition(string text, PdfWriter writer)
        {
            return writer.PageSize.Width / 2 - _baseFont.GetWidthPoint(text, 8) / 2;
        }
    }
}

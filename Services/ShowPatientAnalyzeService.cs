using iText.Commons.Datastructures;
using Labaratory.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf.IO;
using QRCoder;
using Spire.Doc;
using Spire.Doc.Fields;
using System.Drawing;

namespace Labaratory.Services
{
    public class ShowPatientAnalyzeService
    {
        public ShowPatientAnalyzeService() { }

        public byte[] EditPdfWithSpire(string filePath, PatientApplication patient)
        {
            // Load the PDF as a Word document (Spire.Doc can read PDF files as Word documents)
            var document = new Document();
            document.LoadFromFile(filePath);

            // Replace placeholders with patient data
            ReplaceTextWithFontSize(document, "{{FIO}}", CapitalizeWords(patient.FinalCost.ToString()), 10);
            ReplaceTextWithFontSize(document, "{{TotalCost}}", patient.TotalCost.ToString(), 10);
            ReplaceTextWithFontSize(document, "{{PaymentType}}", patient.PaymentType!, 10);
            ReplaceTextWithFontSize(document, "{{Date}}", DateTime.Now.ToString("dd.MM.yyyy HH:mm"), 10);

            // Generate QR code
            var qrCodeUrl = "https://www.youtube.com/watch?v=PeRCDH_zUnU";
            QRCodeGenerator qrGenerator = new();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrCodeUrl, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new(qrCodeData);
            Bitmap qrBitmap = qrCode.GetGraphic(20);

            // Convert the QR code image to a MemoryStream
            using (var qrStream = new MemoryStream())
            {
                qrBitmap.Save(qrStream, System.Drawing.Imaging.ImageFormat.Png);
                qrStream.Position = 0;

                // Add the QR code image to the document
                Spire.Doc.Section section = document.Sections[0];  // Assuming the first section of the document
                Spire.Doc.Documents.Paragraph paragraph = section.AddParagraph();
                DocPicture picture = paragraph.AppendPicture(qrStream);
                picture.Width = 100;
                picture.Height = 100;
            }

            // Save the modified document as a PDF
            using var ms = new MemoryStream();
            document.SaveToStream(ms, FileFormat.PDF);
            return ms.ToArray();
        }

        private static void ReplaceTextWithFontSize(Document document, string placeholder, string replacement, int fontSize)
        {
            foreach (Section section in document.Sections)
            {
                foreach (Spire.Doc.Documents.Paragraph paragraph in section.Paragraphs)
                {
                    // Replace the placeholder text with the actual value
                    if (paragraph.Text.Contains(placeholder))
                    {
                        paragraph.Replace(placeholder, replacement, false, true);

                        // Apply the font size directly to the paragraph
                        foreach (TextRange textRange in paragraph.Items)
                        {
                            textRange.CharacterFormat.FontSize = fontSize;
                        }
                    }
                }
            }
        }


        private static string CapitalizeWords(string text)
        {
            var words = text.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i][1..].ToLower();
            }
            return string.Join(" ", words);
        }
    }
}

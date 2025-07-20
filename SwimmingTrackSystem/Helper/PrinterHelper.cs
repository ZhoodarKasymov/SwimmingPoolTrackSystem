using System.Drawing;
using System.Drawing.Printing;
using QRCoder;

namespace SwimmingTrackSystem.Helper;

public static class PrinterHelper
    {
        public static string PrinterName;
        public static DateTime TransactionCreatedDate;
        public static DateTime ExpireDate;
        public static string Guid;
        public static string LastGeneratedGuid { get; private set; } // To store the GUID for external access

        public static void Print()
        {
            var print = new PrintDocument();
            print.PrintPage += PrintPageHandler;

            if (!string.IsNullOrEmpty(PrinterName))
                print.PrinterSettings.PrinterName = PrinterName;

            print.Print();
        }

        private static void PrintPageHandler(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            var leading = 4;
            float lineHeight;

            using (var font14 = new Font("Courier New", 14))
            {
                lineHeight = font14.GetHeight() + leading;
            }

            float startX = 0;
            float startY = leading;
            float offset = 0;

            var formatLeft = new StringFormat(StringFormatFlags.NoClip);
            var formatCenter = new StringFormat(formatLeft);
            var formatRight = new StringFormat(formatLeft);

            formatCenter.Alignment = StringAlignment.Center;
            formatRight.Alignment = StringAlignment.Far;
            formatLeft.Alignment = StringAlignment.Near;

            var layoutSize = new SizeF(280.0f - offset * 2, lineHeight);

            // Calculate dates
            DateTime creationDate = DateTime.Now;

            // Generate a unique GUID for the QR code
            string uniqueId = Guid;
            LastGeneratedGuid = uniqueId; // Store the GUID
            Bitmap qrCodeBitmap = GenerateQrCode(uniqueId);

            // New receipt content with a clean, modern layout
            var receiptContent = 
                                 $"Пропуск действует\n" +
                                 "".PadRight(32, '-') + "\n" +
                                 $"С: {TransactionCreatedDate:dd/MM/yyyy} {creationDate:HH:mm}\n" +
                                 $"По: {ExpireDate:dd/MM/yyyy HH:mm}\n" +
                                 "".PadRight(32, '-');

            var lines = receiptContent.Split('\n');
            var lineCount = 1;

            // Calculate the center position
            var paperWidth = e.PageSettings.PaperSize.Width;
            var centerPos = (paperWidth - e.MarginBounds.Width) / 2;
            centerPos -= 50;

            var bold10 = new Font("Arial", 10, FontStyle.Bold);
            var bold30 = new Font("Arial", 30, FontStyle.Bold);
            var bold15 = new Font("Arial", 15, FontStyle.Bold);
            var bold18 = new Font("Arial", 8, FontStyle.Bold);
            var regular9 = new Font("Arial", 9, FontStyle.Regular);

            // Print text lines
            foreach (var line in lines)
            {
                offset += lineHeight;
                var layout = new RectangleF(new PointF(startX, startY + offset), layoutSize);

                switch (lineCount)
                {
                    case 1: // Time
                        g.DrawString(line, bold10, Brushes.Black, layout, formatCenter);
                        break;
                    case 2: // Separator
                        g.DrawString(line, regular9, Brushes.Black, layout, formatCenter);
                        break;
                    case 3: // From Date
                        g.DrawString(line, bold10, Brushes.Black, layout, formatCenter);
                        break;
                    case 4: // To Date
                        g.DrawString(line, bold10, Brushes.Black, layout, formatCenter);
                        break;
                    case 5: // Separator
                        g.DrawString(line, regular9, Brushes.Black, layout, formatCenter);
                        break;
                    default:
                        g.DrawString(line, bold18, Brushes.Black, layout, formatCenter);
                        break;
                }

                lineCount++;
            }

            // Position QR code near the bottom center
            g.DrawImage(qrCodeBitmap, centerPos+7, 130, qrCodeBitmap.Width, qrCodeBitmap.Height);

            bold10.Dispose();
            bold15.Dispose();
            bold30.Dispose();
            bold18.Dispose();
            regular9.Dispose();
            qrCodeBitmap.Dispose();

            e.HasMorePages = false;
        }

        private static Bitmap GenerateQrCode(string data)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrCodeData);
            return qrCode.GetGraphic(5);
        }
    }
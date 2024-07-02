using PluginsCommon;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace ImageCommon
{
    public class ImageUtilites
    {
        public static BitmapImage CreateQuestionMarkImage(int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                Color backgroundColor = Color.Gray;
                graphics.Clear(backgroundColor);

                string text = "?";
                Font font = new Font("Arial", 40, FontStyle.Bold);
                Color textColor = Color.Black;

                SizeF textSize = graphics.MeasureString(text, font);
                PointF position = new PointF((width - textSize.Width) / 2, (height - textSize.Height) / 2);

                using (Brush textBrush = new SolidBrush(textColor))
                {
                    graphics.DrawString(text, font, textBrush, position);
                }
            }

            BitmapImage bitmapImage;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Png);
                memoryStream.Seek(0, SeekOrigin.Begin);

                bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
            }

            return bitmapImage;
        }
        public static BitmapImage LoadImageFromFile(string filePath)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }
        public static BitmapImage DownloadImage(string url)
        {
            var imageBytes = DownloaderCommon.HttpDownloader.DownloadData(url);
            BitmapImage bitmapImage = new BitmapImage();
            using (var stream = new MemoryStream(imageBytes))
            {
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            return bitmapImage;
        }
        public static void SaveImage(string filePath, BitmapImage bitmapImage, ImageFormat format = null)
        {
            filePath = FileSystem.FixPathLength(filePath);
            string fileDir = Path.GetDirectoryName(filePath);

            if (!FileSystem.DirectoryExists(fileDir))
            {
                FileSystem.CreateDirectory(fileDir);
            }

            if (format == null)
            {
                format = ImageFormat.Jpeg;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(ms);
                ms.Position = 0;

                // Create a Bitmap from the MemoryStream
                using (Bitmap bitmap = new Bitmap(ms))
                {
                    // Save the Bitmap to a file
                    bitmap.Save(filePath, format);
                }
            }
        }
    }
}

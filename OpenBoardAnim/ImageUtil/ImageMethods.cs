using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenBoardAnim.ImageUtil.Encoder;
using OpenBoardAnim.Util;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using PixelFormat = System.Windows.Media.PixelFormat;
using Size = System.Drawing.Size;

namespace OpenBoardAnim.ImageUtil
{
    /// <summary>
    /// Image algorithms.
    /// </summary>
    public static class ImageMethods
    {
        #region Create and Save Images

        /// <summary>
        /// Creates a solid color BitmapSource.
        /// </summary>
        /// <param name="color">The Background color.</param>
        /// <param name="width">The Width of the image.</param>
        /// <param name="height">The Height of the image.</param>
        /// <param name="dpi">The dpi of the image.</param>
        /// <param name="pixelFormat">The PixelFormat.</param>
        /// <returns>A BitmapSource of the given parameters.</returns>
        public static BitmapSource CreateEmtpyBitmapSource(System.Windows.Media.Color color, int width, int height, double dpi, PixelFormat pixelFormat)
        {
            int rawStride = (width * pixelFormat.BitsPerPixel + 7) / 8;
            var rawImage = new byte[rawStride * height];

            var colors = new List<System.Windows.Media.Color> { color };
            var myPalette = new BitmapPalette(colors);

            return BitmapSource.Create(width, height, dpi, dpi, pixelFormat, myPalette, rawImage, rawStride);
        }

        /// <summary>
        /// Converts a BitmapSource to a BitmapImage.
        /// </summary>
        /// <typeparam name="T">A BitmapEncoder derived class.</typeparam>
        /// <param name="bitmapSource">The source to convert.</param>
        /// <returns>A converted BitmapImage.</returns>
        private static BitmapImage GetBitmapImage<T>(BitmapSource bitmapSource) where T : BitmapEncoder, new()
        {
            var frame = BitmapFrame.Create(bitmapSource);
            var encoder = new T();
            encoder.Frames.Add(frame);

            var bitmapImage = new BitmapImage();
            bool isCreated;

            try
            {
                using (var ms = new MemoryStream())
                {
                    encoder.Save(ms);

                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();
                    isCreated = true;
                }
            }
            catch
            {
                isCreated = false;
            }

            return isCreated ? bitmapImage : null;
        }

        #endregion

        #region Edit Images

        /// <summary>
        /// Resizes the given image.
        /// </summary>
        /// <param name="source">The image source.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="margin">Cut margin.</param>
        /// <param name="dpi">The DPI of the image.</param>
        /// <returns>A resized ImageSource</returns>
        public static BitmapFrame ResizeImage(BitmapImage source, int width, int height, int margin = 0, double dpi = 96d)
        {
            var scale = dpi / 96d;

            var drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawImage(source, new Rect(0, 0, width, height));
            }

            var resizedImage = new RenderTargetBitmap(
                (int)Math.Round(width * scale),
                (int)Math.Round(height * scale),
                dpi, dpi,              // Default DPI values
                PixelFormats.Pbgra32); // Default pixel format
            resizedImage.Render(drawingVisual);

            return BitmapFrame.Create(resizedImage);
        }

        /// <summary>
        /// Crops a given image.
        /// </summary>
        /// <param name="source">The BitmapSource.</param>
        /// <param name="rect">The crop rectangle.</param>
        /// <returns>The Cropped image.</returns>
        public static BitmapFrame CropImage(BitmapSource source, Int32Rect rect)
        {
            var croppedImage = new CroppedBitmap(source, rect);

            return BitmapFrame.Create(croppedImage);
        }

        #endregion

        #region Others

        /// <summary>
        /// Gets the Bitmap from the source and closes the file usage.
        /// </summary>
        /// <param name="fileSource">The file to open.</param>
        /// <returns>The open Bitmap.</returns>
        public static Bitmap From(this string fileSource)
        {
            var bitmapAux = new Bitmap(fileSource);
            var bitmapReturn = new Bitmap(bitmapAux);
            bitmapAux.Dispose();

            return bitmapReturn;
        }

        /// <summary>
        /// Gets the BitmapSource from the source and closes the file usage.
        /// </summary>
        /// <param name="fileSource">The file to open.</param>
        /// <param name="size">The maximum height of the image.</param>
        /// <returns>The open BitmapSource.</returns>
        public static BitmapSource SourceFrom(this string fileSource, Int32? size = null)
        {
            //TODO: Add a try before opening.

            using (var stream = new FileStream(fileSource, FileMode.Open))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

                if (size.HasValue)
                    bitmapImage.DecodePixelHeight = size.Value;

                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // just in case you want to load the image in another thread
                return bitmapImage;
            }
        }

        /// <summary>
        /// Gets the BitmapSource from the source and closes the file usage.
        /// </summary>
        /// <param name="stream">The stream to open.</param>
        /// <param name="size">The maximum height of the image.</param>
        /// <returns>The open BitmapSource.</returns>
        public static BitmapSource SourceFrom(this Stream stream, Int32? size = null)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

            if (size.HasValue)
                bitmapImage.DecodePixelHeight = size.Value;

            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // just in case you want to load the image in another thread
            return bitmapImage;
        }

        /// <summary>
        /// Gets the BitmapSource from the source and closes the file usage.
        /// </summary>
        /// <param name="fileSource">The file to open.</param>
        /// <param name="rect">The desired crop area.</param>
        /// <returns>The open BitmapSource.</returns>
        public static BitmapSource CropFrom(this string fileSource, Int32Rect rect)
        {
            using (var stream = new FileStream(fileSource, FileMode.Open))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // just in case you want to load the image in another thread

                var scale = bitmapImage.DpiX / 96d;

                rect = new Int32Rect((int)(rect.X * scale), (int)(rect.Y * scale),
                    (int)(rect.Width * scale), (int)(rect.Height * scale));

                return new CroppedBitmap(bitmapImage, rect);
            }
        }

        /// <summary>
        /// Gets a render of the current UIElement
        /// </summary>
        /// <param name="source">UIElement to screenshot</param>
        /// <param name="dpi">The DPI of the source.</param>
        /// <param name="size">The size of the destination image.</param>
        /// <returns>An ImageSource</returns>
        public static RenderTargetBitmap GetRender(this Grid source, double dpi, System.Windows.Size size)
        {
            var bounds = VisualTreeHelper.GetDescendantBounds(source);

            var scale = dpi / 96.0;
            var width = (bounds.Width + bounds.X) * scale;
            var height = (bounds.Height + bounds.Y) * scale;

            var rtb = new RenderTargetBitmap((int)Math.Round(width, MidpointRounding.AwayFromZero),
                    (int)Math.Round(height, MidpointRounding.AwayFromZero), dpi, dpi, PixelFormats.Pbgra32);

            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                var vb = new VisualBrush(source);

                var locationRect = new System.Windows.Point(bounds.X, bounds.Y);
                var sizeRect = new System.Windows.Size(bounds.Width, bounds.Height);

                ctx.DrawRectangle(vb, null, new Rect(locationRect, sizeRect));
            }

            rtb.Render(dv);
            return (RenderTargetBitmap)rtb.GetAsFrozen();
        }

        /// <summary>
        /// Gets a render of the current UIElement
        /// </summary>
        /// <param name="source">UIElement to screenshot</param>
        /// <param name="dpi">The DPI of the source.</param>
        /// <param name="size">The size of the destination image.</param>
        /// <returns>An ImageSource</returns>
        public static RenderTargetBitmap GetRender(this UIElement source, double dpi, System.Windows.Size size)
        {
            Rect bounds = VisualTreeHelper.GetDescendantBounds(source);

            var scale = dpi / 96.0;
            var width = (bounds.Width + bounds.X) * scale;
            var height = (bounds.Height + bounds.Y) * scale;

            #region If no bounds

            if (bounds.IsEmpty)
            {
                var control = source as Control;

                if (control != null)
                {
                    width = control.ActualWidth * scale;
                    height = control.ActualHeight * scale;
                }

                bounds = new Rect(new System.Windows.Point(0d, 0d), new System.Windows.Point(width, height));
            }

            #endregion

            var rtb = new RenderTargetBitmap((int)Math.Round(width, MidpointRounding.AwayFromZero),
                    (int)Math.Round(height, MidpointRounding.AwayFromZero), dpi, dpi, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext ctx = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(source);

                var locationRect = new System.Windows.Point(bounds.X, bounds.Y);
                var sizeRect = new System.Windows.Size(bounds.Width, bounds.Height);

                ctx.DrawRectangle(vb, null, new Rect(locationRect, sizeRect));
            }

            rtb.Render(dv);
            return (RenderTargetBitmap)rtb.GetAsFrozen();
        }

        /// <summary>
        /// Gets a render of the current UIElement
        /// </summary>
        /// <param name="source">UIElement to screenshot</param>
        /// <param name="dpi">The DPI of the source.</param>
        /// <returns>An ImageSource</returns>
        public static RenderTargetBitmap GetRender(this UIElement source, double dpi)
        {
            Rect bounds = VisualTreeHelper.GetDescendantBounds(source);

            var scale = dpi / 96.0;
            var width = (bounds.Width + bounds.X) * scale;
            var height = (bounds.Height + bounds.Y) * scale;

            #region If no bounds

            if (bounds.IsEmpty)
            {
                var control = source as Control;

                if (control != null)
                {
                    width = control.ActualWidth * scale;
                    height = control.ActualHeight * scale;
                }

                bounds = new Rect(new System.Windows.Point(0d, 0d), new System.Windows.Point(width, height));
            }

            #endregion

            var rtb = new RenderTargetBitmap((int)Math.Round(width, MidpointRounding.AwayFromZero),
                    (int)Math.Round(height, MidpointRounding.AwayFromZero), dpi, dpi, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext ctx = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(source);

                var locationRect = new System.Windows.Point(bounds.X, bounds.Y);
                var sizeRect = new System.Windows.Size((int)Math.Round(bounds.Width, MidpointRounding.AwayFromZero), (int)Math.Round(bounds.Height, MidpointRounding.AwayFromZero));

                ctx.DrawRectangle(vb, null, new Rect(locationRect, sizeRect));
            }

            rtb.Render(dv);
            return (RenderTargetBitmap)rtb.GetAsFrozen();
        }

        /// <summary>
        /// Gets the DPI of given image.
        /// </summary>
        /// <param name="fileSource">The filename of the source.</param>
        /// <returns>The DPI.</returns>
        public static double DpiOf(this string fileSource)
        {
            using (var stream = new FileStream(fileSource, FileMode.Open))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.None;

                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                return bitmapImage.DpiX;
            }
        }

        /// <summary>
        /// Gets the size * scale of given image.
        /// </summary>
        /// <param name="fileSource">The filename of the source.</param>
        /// <returns>The size of the image.</returns>
        public static System.Windows.Size ScaledSize(this string fileSource)
        {
            using (var stream = new FileStream(fileSource, FileMode.Open))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.None;

                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                return new System.Windows.Size(bitmapImage.PixelWidth, bitmapImage.PixelHeight);
            }
        }

        /// <summary>
        /// Gets the BitmapSource from the source and closes the file usage.
        /// </summary>
        /// <param name="fileSource">The file to open.</param>
        /// <returns>The open BitmapSource.</returns>
        public static Size SizeOf(this string fileSource)
        {
            var bitmapAux = new Bitmap(fileSource);
            var size = new Size(bitmapAux.Width, bitmapAux.Height);
            bitmapAux.Dispose();

            return size;
        }

        #endregion
    }
}

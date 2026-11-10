using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using ZtarVega.Localization;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>Saves the images chosen by the user into the assets folder as reduced PNG files.</summary>
    internal sealed class ImageImporter
    {
        private readonly AssetLocator _locator;
        private const long JpegQuality = 85L;

        public ImageImporter(AssetLocator locator)
        {
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        }

        /// <summary>
        /// Reads the source image, reduces it to fit the given size (it is never enlarged) and stores it as the
        /// image of the game, replacing the previous one. Returns every path that was created or deleted, so
        /// the caches can be refreshed. Throws <see cref="InvalidDataException"/> when the file is not an image.
        /// </summary>
        public IReadOnlyList<string> Import(
            string sourcePath,
            string folderName,
            Game game,
            int? screenshotNumber,
            int maxWidth,
            int maxHeight)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new ArgumentException("The source path cannot be empty.", nameof(sourcePath));
            }

            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            using (Bitmap scaled = ImageCache.TryLoadScaled(sourcePath, maxWidth, maxHeight))
            {
                if (scaled == null)
                {
                    throw new InvalidDataException(Localizer.Format("imageImport.error.invalidImage", Path.GetFileName(sourcePath)));
                }

                string key = AssetLocator.GetPrimaryKey(game);
                string fileStem = screenshotNumber.HasValue
                    ? AssetLocator.GetScreenshotFileStem(key, screenshotNumber.Value)
                    : key;

                // Box art and cartridges are photographs: JPEG is several times smaller than PNG.
                // Logos and screenshots stay PNG, which is lossless and supports transparency.
                bool useJpeg = folderName == AssetLocator.BoxArtFolderName || folderName == AssetLocator.CartridgeFolderName;
                string destination = _locator.BuildImagePath(folderName, fileStem, useJpeg ? ".jpg" : ".png");
                string temporaryPath = destination + ".tmp";

                Directory.CreateDirectory(Path.GetDirectoryName(destination));

                if (useJpeg)
                {
                    SaveAsJpeg(scaled, temporaryPath);
                }
                else
                {
                    SaveAsPng(scaled, temporaryPath);
                }

                // The new file is written first and swapped in, so a failure never leaves the game without its image.
                if (File.Exists(destination))
                {
                    File.Replace(temporaryPath, destination, null);
                }
                else
                {
                    File.Move(temporaryPath, destination);
                }

                var affectedPaths = new List<string> { destination };

                // Files holding the same image under another key or extension (for example an old .jpg)
                // are removed, so the new one is the only match.
                foreach (string oldPath in _locator.FindAllImageFiles(folderName, game, screenshotNumber))
                {
                    if (!string.Equals(oldPath, destination, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(oldPath);
                        affectedPaths.Add(oldPath);
                    }
                }

                return affectedPaths;
            }
        }

        /// <summary>Deletes every file that holds the given image of the game. Returns the deleted paths.</summary>
        public IReadOnlyList<string> Remove(string folderName, Game game, int? screenshotNumber)
        {
            var deletedPaths = new List<string>();

            foreach (string path in _locator.FindAllImageFiles(folderName, game, screenshotNumber))
            {
                File.Delete(path);
                deletedPaths.Add(path);
            }

            return deletedPaths;
        }

        private static void SaveAsPng(Bitmap bitmap, string path)
        {
            // The cache works with premultiplied pixels; files are stored with standard transparency.
            using (Bitmap standard = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), PixelFormat.Format32bppArgb))
            {
                standard.Save(path, ImageFormat.Png);
            }
        }

        private static void SaveAsJpeg(Bitmap bitmap, string path)
        {
            // JPEG has no transparency, so the image is drawn over a white background first.
            using (var flattened = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb))
            {
                using (Graphics graphics = Graphics.FromImage(flattened))
                {
                    graphics.Clear(Color.White);
                    graphics.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                }

                ImageCodecInfo jpegCodec = ImageCodecInfo.GetImageEncoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

                using (var parameters = new EncoderParameters(1))
                {
                    parameters.Param[0] = new EncoderParameter(Encoder.Quality, JpegQuality);
                    flattened.Save(path, jpegCodec, parameters);
                }
            }
        }
    }
}
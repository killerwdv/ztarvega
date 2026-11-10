#if DEBUG
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using ZtarVega.Data;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>Generates fictional placeholder images so the UI can be tested. Debug builds only.</summary>
    internal static class DevelopmentAssetGenerator
    {
        private const int GamesWithAssets = 20;
        private const int GameIdWithoutImagesDivisor = 5;
        private const int GameIdWithFewScreenshotsDivisor = 3;
        private const int FewScreenshotsCount = 2;

        private static readonly Size BoxArtSize = new Size(300, 420);
        private static readonly Size CartridgeSize = new Size(320, 280);
        private static readonly Size LogoSize = new Size(400, 120);
        private static readonly Size ScreenshotSize = new Size(256, 240);

        /// <summary>
        /// Creates images for the first games when they do not exist yet. Games whose id is a multiple
        /// of 5 get no images and those that are a multiple of 3 get only two screenshots, so the
        /// placeholders can be tested.
        /// </summary>
        public static void GenerateIfMissing(IGameRepository repository, AssetLocator locator)
        {
            for (int gameId = 1; gameId <= GamesWithAssets; gameId++)
            {
                Game game = repository.GetById(gameId);

                if (game == null || gameId % GameIdWithoutImagesDivisor == 0)
                {
                    continue;
                }

                string key = AssetLocator.GetPrimaryKey(game);

                SaveImage(locator.BuildPngPath(AssetLocator.LogoFolderName, key), LogoSize, game.Title, "Logo", gameId);
                SaveImage(locator.BuildPngPath(AssetLocator.BoxArtFolderName, key), BoxArtSize, game.Title, "Box art", gameId + 1000);
                SaveImage(locator.BuildPngPath(AssetLocator.CartridgeFolderName, key), CartridgeSize, game.Title, "Cartridge", gameId + 2000);

                int screenshotCount = gameId % GameIdWithFewScreenshotsDivisor == 0
                    ? FewScreenshotsCount
                    : AssetLocator.ScreenshotCount;

                for (int number = 1; number <= screenshotCount; number++)
                {
                    SaveImage(
                        locator.BuildPngPath(AssetLocator.ScreenshotFolderName, AssetLocator.GetScreenshotFileStem(key, number)),
                        ScreenshotSize,
                        game.Title,
                        "Screenshot " + number,
                        gameId * 31 + number);
                }
            }
        }

        private static void SaveImage(string path, Size size, string title, string caption, int colorSeed)
        {
            if (File.Exists(path))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var random = new Random(colorSeed);
            Color topColor = Color.FromArgb(random.Next(40, 200), random.Next(40, 200), random.Next(40, 200));
            Color bottomColor = Color.FromArgb(random.Next(40, 200), random.Next(40, 200), random.Next(40, 200));
            var bounds = new Rectangle(Point.Empty, size);

            using (var bitmap = new Bitmap(size.Width, size.Height))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (var background = new LinearGradientBrush(bounds, topColor, bottomColor, LinearGradientMode.Vertical))
            using (var font = new Font(FontFamily.GenericSansSerif, size.Height / 12f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                graphics.FillRectangle(background, bounds);
                graphics.DrawString(title + "\n" + caption, font, Brushes.White, bounds, format);
                bitmap.Save(path, ImageFormat.Png);
            }
        }
    }
}
#endif
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using ZtarVega.Localization;

namespace ZtarVega.Services
{
    /// <summary>One image slot of a game (box art, cartridge, logo or one screenshot) and the size its images are stored at.</summary>
    internal sealed class ImageTarget
    {
        public static readonly Size BoxArtMaxSize = new Size(600, 840);
        public static readonly Size CartridgeMaxSize = new Size(600, 500);
        public static readonly Size LogoMaxSize = new Size(800, 240);
        public static readonly Size ScreenshotMaxSize = new Size(800, 720);

        private ImageTarget(string displayName, string folderName, int? screenshotNumber, Size maxSize)
        {
            DisplayName = displayName;
            FolderName = folderName;
            ScreenshotNumber = screenshotNumber;
            MaxSize = maxSize;
        }

        /// <summary>Every slot an image can be imported into, in the order they are offered to the user.</summary>
        public static IReadOnlyList<ImageTarget> All { get; } = CreateAll();

        public string DisplayName { get; }

        public string FolderName { get; }

        /// <summary>The screenshot number (1 to 5), or null when the slot is not a screenshot.</summary>
        public int? ScreenshotNumber { get; }

        public Size MaxSize { get; }

        /// <summary>A stable name of the image type ("boxart", "cartridges", "logos", "screenshots1"...), used to save settings.</summary>
        public string Key
        {
            get
            {
                return ScreenshotNumber.HasValue
                    ? FolderName + ScreenshotNumber.Value.ToString(CultureInfo.InvariantCulture)
                    : FolderName;
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }

        private static IReadOnlyList<ImageTarget> CreateAll()
        {
            var targets = new List<ImageTarget>
            {
                new ImageTarget(Localizer.Get("images.slot.boxArt"), AssetLocator.BoxArtFolderName, null, BoxArtMaxSize),
                new ImageTarget(Localizer.Get("images.slot.cartridge"), AssetLocator.CartridgeFolderName, null, CartridgeMaxSize),
                new ImageTarget(Localizer.Get("images.slot.logo"), AssetLocator.LogoFolderName, null, LogoMaxSize)
            };

            for (int number = 1; number <= AssetLocator.ScreenshotCount; number++)
            {
                targets.Add(new ImageTarget(
                    Localizer.Format("images.slot.screenshot", number),
                    AssetLocator.ScreenshotFolderName,
                    number,
                    ScreenshotMaxSize));
            }

            return targets;
        }
    }
}
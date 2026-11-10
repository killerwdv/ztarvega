using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>
    /// Finds the image files of a game. Images are named after the ROM checksum, which is the same on every
    /// installation (rebuilding the database can never mix the images up). Games that have no checksum yet
    /// use their id instead.
    /// </summary>
    internal sealed class AssetLocator
    {
        public const int ScreenshotCount = 5;

        public const string BoxArtFolderName = "boxart";
        public const string CartridgeFolderName = "cartridges";
        public const string LogoFolderName = "logos";
        public const string ScreenshotFolderName = "screenshots";
        private const int MaxInheritanceDepth = 8;

        private static readonly string[] SupportedExtensions = { ".png", ".jpg", ".jpeg" };

        private readonly string _assetsRoot;

        public AssetLocator(string assetsRoot)
        {
            if (string.IsNullOrWhiteSpace(assetsRoot))
            {
                throw new ArgumentException("The assets folder path cannot be empty.", nameof(assetsRoot));
            }

            _assetsRoot = assetsRoot;
        }

        /// <summary>The name (without extension) used to store new images of a game.</summary>
        public static string GetPrimaryKey(Game game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            return !string.IsNullOrEmpty(game.Crc32)
                ? game.Crc32.ToUpperInvariant()
                : IdKey(game);
        }

        /// <summary>Returns the file name (without extension) of a screenshot, e.g. "A1B2C3D4_3".</summary>
        public static string GetScreenshotFileStem(string key, int number)
        {
            return key + "_" + number.ToString(CultureInfo.InvariantCulture);
        }

        public string FindBoxArt(Game game)
        {
            Game owner;
            return FindInheritedImage(BoxArtFolderName, game, null, out owner);
        }

        public string FindCartridge(Game game)
        {
            Game owner;
            return FindInheritedImage(CartridgeFolderName, game, null, out owner);
        }

        public string FindLogo(Game game)
        {
            Game owner;
            return FindInheritedImage(LogoFolderName, game, null, out owner);
        }

        /// <summary>Finds a screenshot, inherited if needed. The number goes from 1 to <see cref="ScreenshotCount"/>.</summary>
        public string FindScreenshot(Game game, int number)
        {
            if (number < 1 || number > ScreenshotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(number));
            }

            Game owner;
            return FindInheritedImage(ScreenshotFolderName, game, number, out owner);
        }

        /// <summary>True when the game has at least one screenshot of its own.</summary>
        public bool HasOwnScreenshots(Game game)
        {
            for (int number = 1; number <= ScreenshotCount; number++)
            {
                if (FindImage(ScreenshotFolderName, game, number) != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Finds an image of the game or, when it has none of its own, the one of its base game (and so on up the
        /// chain). Screenshots are inherited as a set: the first game in the chain that has any screenshot of its
        /// own supplies all of them. <paramref name="owner"/> receives the game the image belongs to.
        /// </summary>
        public string FindInheritedImage(string folderName, Game game, int? screenshotNumber, out Game owner)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            owner = null;
            int depth = 0;

            for (Game candidate = game; candidate != null && depth < MaxInheritanceDepth; candidate = candidate.BaseGame, depth++)
            {
                if (screenshotNumber.HasValue)
                {
                    if (HasOwnScreenshots(candidate))
                    {
                        owner = candidate;
                        return FindImage(folderName, candidate, screenshotNumber);
                    }

                    continue;
                }

                string path = FindImage(folderName, candidate, null);

                if (path != null)
                {
                    owner = candidate;
                    return path;
                }
            }

            return null;
        }

        /// <summary>Builds the path where a PNG image should be saved.</summary>
        public string BuildPngPath(string folderName, string fileNameWithoutExtension)
        {
            return Path.Combine(_assetsRoot, folderName, fileNameWithoutExtension + ".png");
        }

        /// <summary>Builds the path where an image with the given extension (".png" or ".jpg") should be saved.</summary>
        public string BuildImagePath(string folderName, string fileNameWithoutExtension, string extension)
        {
            return Path.Combine(_assetsRoot, folderName, fileNameWithoutExtension + extension);
        }

        /// <summary>Creates the (empty) asset folders so the user knows where to put the images.</summary>
        public void EnsureFoldersExist()
        {
            string[] folderNames =
            {
                BoxArtFolderName,
                CartridgeFolderName,
                LogoFolderName,
                ScreenshotFolderName
            };

            foreach (string folderName in folderNames)
            {
                Directory.CreateDirectory(Path.Combine(_assetsRoot, folderName));
            }
        }

        /// <summary>True when at least one image file exists in the assets folders.</summary>
        public bool HasAnyImages()
        {
            string[] folderNames = { BoxArtFolderName, CartridgeFolderName, LogoFolderName, ScreenshotFolderName };

            try
            {
                foreach (string folderName in folderNames)
                {
                    string folder = Path.Combine(_assetsRoot, folderName);

                    if (Directory.Exists(folder) && Directory.EnumerateFiles(folder).Any())
                    {
                        return true;
                    }
                }
            }
            catch (IOException)
            {
                // A folder that cannot be read counts as having no images.
            }
            catch (UnauthorizedAccessException)
            {
            }

            return false;
        }

        private static string IdKey(Game game)
        {
            return "id-" + game.Id.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>The checksum comes first; the id-based name is the fallback for images saved before the checksum was known.</summary>
        private static IEnumerable<string> GetCandidateKeys(Game game)
        {
            if (!string.IsNullOrEmpty(game.Crc32))
            {
                yield return game.Crc32.ToUpperInvariant();
            }

            yield return IdKey(game);
        }

        /// <summary>Finds the file of an image of a game, or returns null when it has none.</summary>
        public string FindImage(string folderName, Game game, int? screenshotNumber)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            return EnumerateCandidatePaths(folderName, game, screenshotNumber).FirstOrDefault(path => File.Exists(path));
        }

        /// <summary>Returns every existing file that holds the given image of a game, whatever its key or extension.</summary>
        public IReadOnlyList<string> FindAllImageFiles(string folderName, Game game, int? screenshotNumber)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            return EnumerateCandidatePaths(folderName, game, screenshotNumber).Where(path => File.Exists(path)).ToList();
        }

        private IEnumerable<string> EnumerateCandidatePaths(string folderName, Game game, int? screenshotNumber)
        {
            string folder = Path.Combine(_assetsRoot, folderName);

            foreach (string key in GetCandidateKeys(game))
            {
                string fileStem = screenshotNumber.HasValue
                    ? GetScreenshotFileStem(key, screenshotNumber.Value)
                    : key;

                foreach (string extension in SupportedExtensions)
                {
                    yield return Path.Combine(folder, fileStem + extension);
                }
            }
        }
    }
}
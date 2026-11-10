using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ZtarVega.Data;
using ZtarVega.Localization;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>Imports the images of a folder, matching each file to a game by its name.</summary>
    internal sealed class ImageFolderImporter
    {
        private const int ProgressReportInterval = 10;

        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };

        // GoodTools-style tags that mark a hack ("[h]", "[h1C]") or a translation ("[T+Spa]", "[tr ru]").
        private static readonly Regex ModifiedVersionPattern =
            new Regex(@"\[(?:h|t[+\-]|tr)[^\]]*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IGameRepository _repository;
        private readonly AssetLocator _locator;
        private readonly ImageImporter _imageImporter;
        private readonly string _reportPath;

        public ImageFolderImporter(IGameRepository repository, AssetLocator locator, ImageImporter imageImporter, string reportPath)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            _imageImporter = imageImporter ?? throw new ArgumentNullException(nameof(imageImporter));
            _reportPath = reportPath ?? throw new ArgumentNullException(nameof(reportPath));
        }

        /// <summary>
        /// Imports the images on a background thread. When cancelled, the images already saved are kept and the
        /// result says so. Throws <see cref="DirectoryNotFoundException"/> when the folder does not exist.
        /// </summary>
        public Task<ImageFolderImportResult> ImportAsync(
            string folderPath,
            ImageTarget target,
            bool replaceExisting,
            bool includeModifiedVersions,
            IProgress<ScanProgress> progress,
            CancellationToken cancellationToken)
        {
            return Task.Run(() => Import(folderPath, target, replaceExisting, includeModifiedVersions, progress, cancellationToken));
        }

        private ImageFolderImportResult Import(
            string folderPath,
            ImageTarget target,
            bool replaceExisting,
            bool includeModifiedVersions,
            IProgress<ScanProgress> progress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentException("The folder path cannot be empty.", nameof(folderPath));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException(Localizer.Format("imageImport.error.folderNotFound", folderPath));
            }

            List<string> files = FindImageFiles(folderPath);
            var index = new GameImageIndex(_repository.GetAll());
            var log = new ImportLog();
            var matches = new List<FileMatch>();

            foreach (string filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                string stem = Path.GetFileNameWithoutExtension(filePath);

                // The image of a hack or a translation must never become the image of the original game.
                bool isModifiedVersion = ModifiedVersionPattern.IsMatch(stem);

                if (isModifiedVersion && !includeModifiedVersions)
                {
                    log.SkippedModified.Add(fileName);
                    continue;
                }

                ImageMatch match = isModifiedVersion ? index.FindModified(stem) : index.Find(stem);

                if (match.Kind == ImageMatchKind.None)
                {
                    log.Unmatched.Add(isModifiedVersion
                        ? fileName + "  (the catalog has no hack or translation with this title)"
                        : fileName);
                }
                else if (match.Kind == ImageMatchKind.Ambiguous)
                {
                    log.Ambiguous.Add(fileName + "  ->  " + match.Detail);
                }
                else
                {
                    matches.Add(new FileMatch(filePath, fileName, match));
                }
            }

            // Exact matches go first, so a precise match is never displaced by an approximate one for the same game.
            List<FileMatch> ordered = matches
                .OrderBy(item => item.Match.Kind == ImageMatchKind.Exact ? 0 : 1)
                .ThenBy(item => item.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var handledGameIds = new HashSet<int>();
            int importedCount = 0;
            int alreadyHadImageCount = 0;
            bool wasCancelled = false;

            for (int position = 0; position < ordered.Count; position++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    wasCancelled = true;
                    break;
                }

                FileMatch item = ordered[position];

                if (position % ProgressReportInterval == 0)
                {
                    progress?.Report(new ScanProgress(position, ordered.Count, item.FileName));
                }

                Game game = item.Match.Game;

                // Only one file can supply the image of a game in a single import.
                if (handledGameIds.Contains(game.Id))
                {
                    log.Duplicates.Add(item.FileName + "  ->  " + game.Title);
                    continue;
                }

                if (!replaceExisting && _locator.FindImage(target.FolderName, game, target.ScreenshotNumber) != null)
                {
                    handledGameIds.Add(game.Id);
                    alreadyHadImageCount++;
                    continue;
                }

                try
                {
                    _imageImporter.Import(
                        item.FilePath,
                        target.FolderName,
                        game,
                        target.ScreenshotNumber,
                        target.MaxSize.Width,
                        target.MaxSize.Height);

                    handledGameIds.Add(game.Id);
                    importedCount++;
                }
                catch (InvalidDataException exception)
                {
                    log.Failures.Add(item.FileName + "  :  " + exception.Message);
                }
                catch (IOException exception)
                {
                    log.Failures.Add(item.FileName + "  :  " + exception.Message);
                }
                catch (UnauthorizedAccessException exception)
                {
                    log.Failures.Add(item.FileName + "  :  " + exception.Message);
                }
                catch (ExternalException exception)
                {
                    // GDI+ reports its failures with this exception.
                    log.Failures.Add(item.FileName + "  :  " + exception.Message);
                }
            }

            string reportPath = WriteReport(
                folderPath, target, replaceExisting, files.Count, importedCount, alreadyHadImageCount, wasCancelled, log);

            return new ImageFolderImportResult(
                files.Count,
                importedCount,
                alreadyHadImageCount,
                log.Duplicates.Count,
                log.Unmatched.Count,
                log.Ambiguous.Count,
                log.SkippedModified.Count,
                log.Failures.Count,
                wasCancelled,
                reportPath);
        }

        private static List<string> FindImageFiles(string folderPath)
        {
            return Directory.EnumerateFiles(folderPath, "*", SearchOption.TopDirectoryOnly)
                .Where(path => ImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Writes a text report with every file that was not imported and why. Returns its path, or null if it could not be written.</summary>
        private string WriteReport(
            string folderPath,
            ImageTarget target,
            bool replaceExisting,
            int filesFound,
            int importedCount,
            int alreadyHadImageCount,
            bool wasCancelled,
            ImportLog log)
        {
            var builder = new StringBuilder();
            builder.AppendLine("ZtarVega image import report");
            builder.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            builder.AppendLine("Folder: " + folderPath);
            builder.AppendLine("Image type: " + target.DisplayName);
            builder.AppendLine("Mode: " + (replaceExisting ? "replace existing images" : "fill only the games without an image"));
            builder.AppendLine(wasCancelled ? "The import was cancelled before it finished." : "The import finished.");
            builder.AppendLine();
            builder.AppendLine("Files found: " + filesFound);
            builder.AppendLine("Images saved: " + importedCount);
            builder.AppendLine("Games that already had an image: " + alreadyHadImageCount);
            builder.AppendLine();

            AppendSection(builder, "NOT MATCHED TO ANY GAME", log.Unmatched);
            AppendSection(builder, "AMBIGUOUS (several games could own the image)", log.Ambiguous);
            AppendSection(builder, "SKIPPED (images of hacks and translations)", log.SkippedModified);
            AppendSection(builder, "DUPLICATES (another file already supplied the image of that game)", log.Duplicates);
            AppendSection(builder, "FAILED", log.Failures);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_reportPath));
                File.WriteAllText(_reportPath, builder.ToString(), Encoding.UTF8);
                return _reportPath;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static void AppendSection(StringBuilder builder, string title, List<string> lines)
        {
            if (lines.Count == 0)
            {
                return;
            }

            builder.AppendLine("== " + title + " (" + lines.Count + ") ==");

            foreach (string line in lines)
            {
                builder.AppendLine(line);
            }

            builder.AppendLine();
        }

        /// <summary>A file together with the game it was matched to.</summary>
        private sealed class FileMatch
        {
            public FileMatch(string filePath, string fileName, ImageMatch match)
            {
                FilePath = filePath;
                FileName = fileName;
                Match = match;
            }

            public string FilePath { get; }

            public string FileName { get; }

            public ImageMatch Match { get; }
        }

        /// <summary>The files that were not imported, grouped by the reason.</summary>
        private sealed class ImportLog
        {
            public List<string> Unmatched { get; } = new List<string>();

            public List<string> Ambiguous { get; } = new List<string>();

            public List<string> SkippedModified { get; } = new List<string>();

            public List<string> Duplicates { get; } = new List<string>();

            public List<string> Failures { get; } = new List<string>();
        }
    }
}
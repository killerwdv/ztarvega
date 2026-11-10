using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Services;

namespace ZtarVega.UI.Actions
{
    /// <summary>The actions that bring data into the catalog: scanning ROMs and importing catalogs, metadata and images.</summary>
    internal sealed class ImportActions : ViewActions
    {
        private readonly GameCatalog _catalog;
        private readonly RomScanner _scanner;
        private readonly RomImporter _romImporter;
        private readonly DatCatalogImporter _datImporter;
        private readonly MetadataImporter _metadataImporter;
        private readonly ImageFolderImporter _imageFolderImporter;
        private readonly ImageCache _imageCache;
        private readonly SettingsSession _settings;
        private readonly SettingsActions _settingsActions;

        private string _lastImageFolder = string.Empty;

        public ImportActions(
            GameCatalog catalog,
            RomScanner scanner,
            RomImporter romImporter,
            DatCatalogImporter datImporter,
            MetadataImporter metadataImporter,
            ImageFolderImporter imageFolderImporter,
            ImageCache imageCache,
            SettingsSession settings,
            SettingsActions settingsActions)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            _romImporter = romImporter ?? throw new ArgumentNullException(nameof(romImporter));
            _datImporter = datImporter ?? throw new ArgumentNullException(nameof(datImporter));
            _metadataImporter = metadataImporter ?? throw new ArgumentNullException(nameof(metadataImporter));
            _imageFolderImporter = imageFolderImporter ?? throw new ArgumentNullException(nameof(imageFolderImporter));
            _imageCache = imageCache ?? throw new ArgumentNullException(nameof(imageCache));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settingsActions = settingsActions ?? throw new ArgumentNullException(nameof(settingsActions));
        }

        // ---------------------------------------------------------------- ROM scan

        public async Task ScanRomsAsync()
        {
            string romsFolder = _settings.Current.RomsFolder;

            if (string.IsNullOrWhiteSpace(romsFolder) || !Directory.Exists(romsFolder))
            {
                _settingsActions.AskToOpenSettings(Localizer.Get("launch.error.romsFolderNotConfigured"));
                return;
            }

            ScanResult result;

            try
            {
                result = await RunScanWithProgressAsync(romsFolder);
            }
            catch (OperationCanceledException)
            {
                MessageDialogs.ShowInformation(MainWindow, Localizer.Get("scan.cancelled"));
                return;
            }
            catch (IOException exception)
            {
                ShowScanError(exception);
                return;
            }
            catch (UnauthorizedAccessException exception)
            {
                ShowScanError(exception);
                return;
            }
            catch (DbException exception)
            {
                ShowScanError(exception);
                return;
            }

            // Back on the UI thread: reload the games and refresh the list, keeping the selected game.
            _catalog.Reload();
            MainWindow.RefreshGames();

            string summary = BuildScanSummary(result);

            if (result.UnmatchedRoms.Count == 0)
            {
                MessageDialogs.ShowInformation(MainWindow, summary);
                return;
            }

            bool addUnmatched = MessageDialogs.Ask(
                MainWindow,
                summary + "\n" + Localizer.Format("scan.addUnmatched.question", result.UnmatchedRoms.Count),
                MessageBoxIcon.Question);

            if (addUnmatched)
            {
                ImportUnmatchedRoms(result.UnmatchedRoms);
            }
        }

        private async Task<ScanResult> RunScanWithProgressAsync(string romsFolder)
        {
            using (var cancellation = new CancellationTokenSource())
            using (var progressForm = new ScanProgressForm(cancellation, "progress.scan.title", "progress.scan.format"))
            {
                // Progress<T> delivers the reports on the UI thread, where it was created.
                var progress = new Progress<ScanProgress>(progressForm.Report);
                Task<ScanResult> scanTask = _scanner.ScanAsync(romsFolder, progress, cancellation.Token);

                // Close the dialog as soon as the scan ends, whatever its outcome.
                Task closeDialogTask = scanTask.ContinueWith(
                    completedTask => progressForm.CloseWhenFinished(),
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext());

                // Blocks (keeping the window responsive) until the scan ends and the dialog closes.
                progressForm.ShowDialog(MainWindow);

                // That continuation is what closed the dialog, so it has already run: awaiting it only makes
                // sure that any error it had is not lost.
                await closeDialogTask;

                // The task has already finished: this returns its result or rethrows its error.
                return await scanTask;
            }
        }

        private static string BuildScanSummary(ScanResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine(Localizer.Get("scan.summary.finished"));
            builder.AppendLine();
            builder.AppendLine(Localizer.Format("scan.summary.filesFound", result.RomFilesFound));
            builder.AppendLine(Localizer.Format("scan.summary.available", result.GamesAvailable));
            builder.AppendLine(Localizer.Format("scan.summary.newlyAvailable", result.NewlyAvailable));
            builder.AppendLine(Localizer.Format("scan.summary.noLongerAvailable", result.NoLongerAvailable));
            builder.AppendLine(Localizer.Format("scan.summary.unmatched", result.UnmatchedRoms.Count));

            if (result.DuplicateFiles > 0)
            {
                builder.AppendLine(Localizer.Format("scan.summary.duplicates", result.DuplicateFiles));
            }

            if (result.UnreadableFiles > 0)
            {
                builder.AppendLine(Localizer.Format("scan.summary.unreadable", result.UnreadableFiles));
            }

            return builder.ToString();
        }

        private void ShowScanError(Exception exception)
        {
            MessageDialogs.ShowWarning(MainWindow, Localizer.Format("scan.error", exception.Message));
        }

        private void ImportUnmatchedRoms(IReadOnlyList<UnmatchedRom> roms)
        {
            RomImportResult importResult;

            try
            {
                importResult = _romImporter.Import(roms);
            }
            catch (DbException exception)
            {
                MessageDialogs.ShowWarning(MainWindow, Localizer.Format("scan.import.failed", exception.Message));
                return;
            }

            _catalog.Reload();
            MainWindow.RefreshGames();

            string message = Localizer.Format("import.gamesAdded", importResult.ImportedCount);

            if (importResult.SkippedCount > 0)
            {
                message += "\n" + Localizer.Format("scan.import.skipped", importResult.SkippedCount);
            }

            if (importResult.LinkedCount > 0)
            {
                message += "\n" + Localizer.Format("scan.import.linked", importResult.LinkedCount);
            }

            MessageDialogs.ShowInformation(MainWindow, message);
        }

        // ---------------------------------------------------------------- catalog from a DAT file

        public async Task ImportCatalogFromDatAsync()
        {
            string datFilePath;

            using (var dialog = new OpenFileDialog
            {
                Title = Localizer.Get("dat.dialogTitle"),
                Filter = Localizer.Get("dat.filter"),
                CheckFileExists = true
            })
            {
                if (dialog.ShowDialog(MainWindow) != DialogResult.OK)
                {
                    return;
                }

                datFilePath = dialog.FileName;
            }

            DatImportResult result;
            MainWindow.SetBusy(true);

            try
            {
                // Reading a large DAT takes a moment, so it runs off the UI thread.
                result = await Task.Run(() => _datImporter.Import(datFilePath));
            }
            catch (InvalidDataException exception)
            {
                ShowDatError(exception);
                return;
            }
            catch (IOException exception)
            {
                ShowDatError(exception);
                return;
            }
            catch (UnauthorizedAccessException exception)
            {
                ShowDatError(exception);
                return;
            }
            catch (DbException exception)
            {
                ShowDatError(exception);
                return;
            }
            finally
            {
                MainWindow.SetBusy(false);
            }

            _catalog.Reload();
            MainWindow.RefreshGames();

            var message = new StringBuilder();
            message.AppendLine(Localizer.Format("import.gamesAdded", result.AddedCount));

            if (result.AlreadyInCatalogCount > 0)
            {
                message.AppendLine(Localizer.Format("import.alreadyInCatalog", result.AlreadyInCatalogCount));
            }

            if (result.IgnoredCount > 0)
            {
                message.AppendLine(Localizer.Format("import.ignored", result.IgnoredCount));
            }

            if (result.LinkedCount > 0)
            {
                message.AppendLine(Localizer.Format("import.linked", result.LinkedCount));
            }

            string romsFolder = _settings.Current.RomsFolder;
            bool canScan = !string.IsNullOrWhiteSpace(romsFolder) && Directory.Exists(romsFolder);

            if (result.AddedCount == 0)
            {
                MessageDialogs.ShowInformation(MainWindow, message.ToString());
                return;
            }

            if (!canScan)
            {
                message.AppendLine();
                message.AppendLine(Localizer.Get("import.scanHint"));
                MessageDialogs.ShowInformation(MainWindow, message.ToString());
                return;
            }

            bool scanNow = MessageDialogs.Ask(
                MainWindow,
                message + "\n" + Localizer.Get("import.scanQuestion"),
                MessageBoxIcon.Question);

            if (scanNow)
            {
                await ScanRomsAsync();
            }
        }

        private void ShowDatError(Exception exception)
        {
            MessageDialogs.ShowWarning(MainWindow, Localizer.Format("dat.error", exception.Message));
        }

        // ---------------------------------------------------------------- metadata files

        public async Task ImportMetadataAsync()
        {
            string[] filePaths;

            using (var dialog = new OpenFileDialog
            {
                Title = Localizer.Get("metadata.dialogTitle"),
                Filter = Localizer.Get("metadata.filter"),
                CheckFileExists = true,
                Multiselect = true
            })
            {
                if (dialog.ShowDialog(MainWindow) != DialogResult.OK)
                {
                    return;
                }

                filePaths = dialog.FileNames;
            }

            bool replaceExisting;

            using (var options = new MetadataImportOptionsForm(filePaths.Length))
            {
                if (options.ShowDialog(MainWindow) != DialogResult.OK)
                {
                    return;
                }

                replaceExisting = options.ReplaceExisting;
            }

            MetadataImportResult result;
            MainWindow.SetBusy(true);

            try
            {
                result = await Task.Run(() => _metadataImporter.Import(filePaths, replaceExisting));
            }
            catch (InvalidDataException exception)
            {
                ShowMetadataError(exception);
                return;
            }
            catch (IOException exception)
            {
                ShowMetadataError(exception);
                return;
            }
            catch (UnauthorizedAccessException exception)
            {
                ShowMetadataError(exception);
                return;
            }
            catch (DbException exception)
            {
                ShowMetadataError(exception);
                return;
            }
            finally
            {
                MainWindow.SetBusy(false);
            }

            _catalog.Reload();
            MainWindow.RefreshGames();

            var summary = new StringBuilder();
            summary.AppendLine(Localizer.Format("metadata.summary.entries", result.EntriesRead));
            summary.AppendLine(Localizer.Format("metadata.summary.matched", result.MatchedGames, result.TotalGames));

            if (result.InheritedGames > 0)
            {
                summary.AppendLine(Localizer.Format("metadata.summary.inherited", result.InheritedGames));
            }

            summary.AppendLine(Localizer.Format("metadata.summary.updated", result.UpdatedGames));
            summary.AppendLine();
            summary.AppendLine(Localizer.Get(replaceExisting ? "metadata.summary.replaced" : "metadata.summary.filled"));

            MessageDialogs.ShowInformation(MainWindow, summary.ToString());
        }

        private void ShowMetadataError(Exception exception)
        {
            MessageDialogs.ShowWarning(MainWindow, Localizer.Format("metadata.error", exception.Message));
        }

        // ---------------------------------------------------------------- images from a folder

        public async Task ImportImagesFromFolderAsync()
        {
            string folderPath;
            ImageTarget target;
            bool replaceExisting;
            bool includeModified;

            using (var options = new ImageFolderImportOptionsForm(_lastImageFolder))
            {
                if (options.ShowDialog(MainWindow) != DialogResult.OK)
                {
                    return;
                }

                folderPath = options.FolderPath;
                target = options.Target;
                replaceExisting = options.ReplaceExisting;
                includeModified = options.IncludeModifiedVersions;
            }

            _lastImageFolder = folderPath;
            ImageFolderImportResult result;

            try
            {
                result = await RunImageImportWithProgressAsync(folderPath, target, replaceExisting, includeModified);
            }
            catch (IOException exception)
            {
                ShowImageImportError(exception);
                return;
            }
            catch (UnauthorizedAccessException exception)
            {
                ShowImageImportError(exception);
                return;
            }
            catch (DbException exception)
            {
                ShowImageImportError(exception);
                return;
            }

            // The panels let go of their images first; the cache then drops every image it holds,
            // because many files changed. The images are loaded again from disk when needed.
            MainWindow.ReloadSelectedGameImages();
            _imageCache.Clear();

            var summary = new StringBuilder();
            summary.AppendLine(Localizer.Get(result.WasCancelled ? "imageImport.cancelled" : "imageImport.finished"));
            summary.AppendLine();
            summary.AppendLine(Localizer.Format("imageImport.type", target.DisplayName));
            summary.AppendLine(Localizer.Format("imageImport.filesFound", result.FilesFound));
            summary.AppendLine(Localizer.Format("imageImport.saved", result.ImportedCount));

            if (result.AlreadyHadImageCount > 0)
            {
                summary.AppendLine(Localizer.Format("imageImport.alreadyHad", result.AlreadyHadImageCount));
            }

            if (result.UnmatchedCount > 0)
            {
                summary.AppendLine(Localizer.Format("imageImport.unmatched", result.UnmatchedCount));
            }

            if (result.AmbiguousCount > 0)
            {
                summary.AppendLine(Localizer.Format("imageImport.ambiguous", result.AmbiguousCount));
            }

            if (result.SkippedModifiedCount > 0)
            {
                summary.AppendLine(Localizer.Format("imageImport.skippedModified", result.SkippedModifiedCount));
            }

            if (result.DuplicateCount > 0)
            {
                summary.AppendLine(Localizer.Format("imageImport.duplicates", result.DuplicateCount));
            }

            if (result.FailedCount > 0)
            {
                summary.AppendLine(Localizer.Format("imageImport.failed", result.FailedCount));
            }

            int notImportedCount = result.UnmatchedCount + result.AmbiguousCount + result.SkippedModifiedCount +
                                   result.DuplicateCount + result.FailedCount;

            if (result.ReportPath == null || notImportedCount == 0)
            {
                MessageDialogs.ShowInformation(MainWindow, summary.ToString());
                return;
            }

            summary.AppendLine();
            summary.AppendLine(Localizer.Get("imageImport.reportQuestion"));

            if (MessageDialogs.Ask(MainWindow, summary.ToString(), MessageBoxIcon.Information))
            {
                OpenReport(result.ReportPath);
            }
        }

        private async Task<ImageFolderImportResult> RunImageImportWithProgressAsync(
            string folderPath,
            ImageTarget target,
            bool replaceExisting,
            bool includeModified)
        {
            using (var cancellation = new CancellationTokenSource())
            using (var progressForm = new ScanProgressForm(cancellation, "progress.images.title", "progress.images.format"))
            {
                // Progress<T> delivers the reports on the UI thread, where it was created.
                var progress = new Progress<ScanProgress>(progressForm.Report);
                Task<ImageFolderImportResult> importTask =
                    _imageFolderImporter.ImportAsync(folderPath, target, replaceExisting, includeModified, progress, cancellation.Token);

                // Close the dialog as soon as the scan ends, whatever its outcome.
                Task closeDialogTask = importTask.ContinueWith(
                    completedTask => progressForm.CloseWhenFinished(),
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext());

                // Blocks (keeping the window responsive) until the scan ends and the dialog closes.
                progressForm.ShowDialog(MainWindow);

                // That continuation is what closed the dialog, so it has already run: awaiting it only makes
                // sure that any error it had is not lost.
                await closeDialogTask;

                // The task has already finished: this returns its result or rethrows its error.
                return await importTask;
            }
        }

        private void OpenReport(string reportPath)
        {
            try
            {
                Process.Start(reportPath);
            }
            catch (Win32Exception)
            {
                MessageDialogs.ShowInformation(MainWindow, Localizer.Format("imageImport.reportOpenFailed", reportPath));
            }
        }

        private void ShowImageImportError(Exception exception)
        {
            MessageDialogs.ShowWarning(MainWindow, Localizer.Format("imageImport.error", exception.Message));
        }
    }
}
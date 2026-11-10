using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Models;
using ZtarVega.Services;

namespace ZtarVega.UI.Actions
{
    /// <summary>The action that downloads the images of the games from a website chosen by the user.</summary>
    internal sealed class DownloadActions : ViewActions
    {
        // Above this number of games, the user is asked to confirm before the requests start.
        private const int ManyImagesThreshold = 300;

        private readonly GameCatalog _catalog;
        private readonly ImageDownloader _downloader;
        private readonly ImageCache _imageCache;
        private readonly SettingsSession _settings;

        public DownloadActions(GameCatalog catalog, ImageDownloader downloader, ImageCache imageCache, SettingsSession settings)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
            _imageCache = imageCache ?? throw new ArgumentNullException(nameof(imageCache));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task DownloadImagesAsync()
        {
            Game selectedGame = MainWindow.SelectedGame;

            // The test button uses the selected game or, when there is none, the first game of the catalog.
            Game testGame = selectedGame ?? _catalog.Search(new GameQuery()).FirstOrDefault();

            ImageTarget target;
            string urlTemplate;
            ImageDownloadScope scope;
            bool replaceExisting;

            using (var options = new ImageDownloadOptionsForm(_settings.Current, selectedGame, testGame, _downloader))
            {
                if (options.ShowDialog(MainWindow) != DialogResult.OK)
                {
                    return;
                }

                target = options.Target;
                urlTemplate = options.UrlTemplate;
                scope = options.Scope;
                replaceExisting = options.ReplaceExisting;
            }

            RememberAddress(target, urlTemplate);

            IReadOnlyList<Game> games = SelectGames(scope, selectedGame);

            if (games.Count == 0)
            {
                MessageDialogs.ShowInformation(MainWindow, Localizer.Get("download.noGames"));
                return;
            }

            if (games.Count > ManyImagesThreshold &&
                !MessageDialogs.Ask(MainWindow, Localizer.Format("download.confirmMany", games.Count), MessageBoxIcon.Question))
            {
                return;
            }

            ImageDownloadResult result;

            try
            {
                result = await RunDownloadWithProgressAsync(games, target, urlTemplate, replaceExisting);
            }
            catch (IOException exception)
            {
                ShowError(exception);
                return;
            }
            catch (UnauthorizedAccessException exception)
            {
                ShowError(exception);
                return;
            }

            // The panels let go of their images first; the cache then drops every image it holds,
            // because many files changed. The images are loaded again from disk when needed.
            MainWindow.ReloadSelectedGameImages();
            _imageCache.Clear();

            ShowSummary(result, target);
        }

        private void RememberAddress(ImageTarget target, string urlTemplate)
        {
            _settings.Current.SetImageSource(target.Key, urlTemplate);

            try
            {
                _settings.Save();
            }
            catch (IOException)
            {
                // The address is also saved when the application closes, so this failure is not worth a message.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private IReadOnlyList<Game> SelectGames(ImageDownloadScope scope, Game selectedGame)
        {
            switch (scope)
            {
                case ImageDownloadScope.AllGames:
                    return _catalog.Search(new GameQuery());

                case ImageDownloadScope.SelectedGame:
                    return selectedGame == null ? new List<Game>() : new List<Game> { selectedGame };

                default:
                    return _catalog.Search(new GameQuery { Mode = GameFilterMode.AvailableOnly });
            }
        }

        private async Task<ImageDownloadResult> RunDownloadWithProgressAsync(
            IReadOnlyList<Game> games,
            ImageTarget target,
            string urlTemplate,
            bool replaceExisting)
        {
            using (var cancellation = new CancellationTokenSource())
            using (var progressForm = new ScanProgressForm(cancellation, "progress.download.title", "progress.download.format"))
            {
                // Progress<T> delivers the reports on the UI thread, where it was created.
                var progress = new Progress<ScanProgress>(progressForm.Report);
                Task<ImageDownloadResult> downloadTask =
                    _downloader.DownloadAsync(games, target, urlTemplate, replaceExisting, progress, cancellation.Token);

                // Close the dialog as soon as the scan ends, whatever its outcome.
                Task closeDialogTask = downloadTask.ContinueWith(
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
                return await downloadTask;
            }
        }

        private void ShowSummary(ImageDownloadResult result, ImageTarget target)
        {
            var summary = new StringBuilder();

            if (result.WasCancelled)
            {
                summary.AppendLine(Localizer.Get("download.summary.cancelled"));
            }
            else if (result.WasStoppedByConnectionErrors)
            {
                summary.AppendLine(Localizer.Get("download.summary.aborted"));
            }
            else
            {
                summary.AppendLine(Localizer.Get("download.summary.finished"));
            }

            summary.AppendLine();
            summary.AppendLine(Localizer.Format("download.summary.type", target.DisplayName));
            summary.AppendLine(Localizer.Format("download.summary.games", result.GamesInSelection));
            summary.AppendLine(Localizer.Format("download.summary.saved", result.DownloadedCount));

            if (result.AlreadyHadImageCount > 0)
            {
                summary.AppendLine(Localizer.Format("download.summary.alreadyHad", result.AlreadyHadImageCount));
            }

            if (result.NotFoundCount > 0)
            {
                summary.AppendLine(Localizer.Format("download.summary.notFound", result.NotFoundCount));
            }

            if (result.NotAnImageCount > 0)
            {
                summary.AppendLine(Localizer.Format("download.summary.notAnImage", result.NotAnImageCount));
            }

            if (result.NoChecksumCount > 0)
            {
                summary.AppendLine(Localizer.Format("download.summary.noChecksum", result.NoChecksumCount));
            }

            if (result.FailedCount > 0)
            {
                summary.AppendLine(Localizer.Format("download.summary.failed", result.FailedCount));
            }

            // When nothing was found, the address is the most likely culprit: show one so it can be checked in a browser.
            if (result.DownloadedCount == 0 && result.FirstNotFoundAddress != null)
            {
                summary.AppendLine();
                summary.AppendLine(Localizer.Format("download.summary.exampleNotFound", result.FirstNotFoundAddress));
            }

            int notDownloadedCount = result.NotFoundCount + result.NotAnImageCount + result.FailedCount;

            if (result.ReportPath == null || notDownloadedCount == 0)
            {
                MessageDialogs.ShowInformation(MainWindow, summary.ToString());
                return;
            }

            summary.AppendLine();
            summary.AppendLine(Localizer.Get("download.summary.reportQuestion"));

            if (MessageDialogs.Ask(MainWindow, summary.ToString(), MessageBoxIcon.Information))
            {
                OpenReport(result.ReportPath);
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

        private void ShowError(Exception exception)
        {
            MessageDialogs.ShowWarning(MainWindow, Localizer.Format("download.error", exception.Message));
        }
    }
}
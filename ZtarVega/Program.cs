using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ZtarVega.Data;
using ZtarVega.Localization;
using ZtarVega.Models;
using ZtarVega.Services;
using ZtarVega.UI;
using ZtarVega.UI.Actions;

namespace ZtarVega
{
    internal static class Program
    {
        /// <summary>The argument given to the copy of the application that starts when the application restarts itself.</summary>
        internal const string RestartArgument = "--restarted";

        private const string ApplicationName = "ZtarVega";
        private const string SingleInstanceMutexName = @"Local\ZtarVega.SingleInstance";
        private const int RestartWaitAttempts = 50;
        private const int RestartWaitIntervalMilliseconds = 100;
        private const string DatabaseFileName = "ztarvega.db";
        private const string SettingsFileName = "config.json";
        private const string AssetsFolderName = "assets";
        private const string LanguagesFolderName = "languages";
        private const string LogFolderName = "logs";
        private const string LogFileName = "ztarvega.log";
        private const string ImageReportFileName = "image-import-report.txt";
        private const string TemporaryFolderName = "ZtarVega";
        private const string ExtractedRomsFolderName = "roms";
        private const long ImageCacheBudgetInBytes = 24L * 1024 * 1024;
        private const string ProfileArgument = "--profile";
        private const string TimingsFileName = "startup-timings.txt";
        private const string DownloadsFolderName = "downloads";
        private const string ImageDownloadReportFileName = "image-download-report.txt";

        [STAThread]
        private static void Main(string[] arguments)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var logger = new ErrorLogger(Path.Combine(baseDirectory, LogFolderName, LogFileName));
            if (arguments.Contains(ProfileArgument))
            {
                StartupProfiler.Enable(Path.Combine(baseDirectory, LogFolderName, TimingsFileName));
            }
            var settingsService = new SettingsService(Path.Combine(baseDirectory, SettingsFileName));
            string languagesFolder = Path.Combine(baseDirectory, LanguagesFolderName);

            // The language is chosen before any window or message is created.
            AppSettings startupSettings = settingsService.Load();
            Localizer.Initialize(languagesFolder, startupSettings.LanguageCode);
            StartupProfiler.Mark("Settings and language loaded");

            // After a restart, the previous copy may need a moment to close, so the new one waits for it.
            bool isRestart = arguments.Contains(RestartArgument);

            // The mutex is held for as long as the application runs: a second copy cannot get it.
            using (Mutex instanceMutex = AcquireSingleInstance(isRestart))
            {
                if (instanceMutex == null)
                {
                    MessageBox.Show(
                        Localizer.Get("app.alreadyRunning"),
                        ApplicationName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                LanguageTemplateWriter.Write(languagesFolder);

                // These handlers must be installed before any window is created.
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (sender, e) =>
                {
                    ReportUnexpectedError(logger, e.Exception);
                    Application.Exit();
                };
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                    ReportUnexpectedError(logger, e.ExceptionObject as Exception);

                try
                {
                    Run(logger, baseDirectory, settingsService, startupSettings);
                }
                catch (Exception exception)
                {
                    // Last line of defense: anything not handled earlier ends up in the log and the user sees a message.
                    ReportUnexpectedError(logger, exception);
                }
            }
        }

        /// <summary>Takes the single-instance lock. Returns null when another copy is running (after waiting for it, if asked to).</summary>
        private static Mutex AcquireSingleInstance(bool waitForPreviousInstance)
        {
            int attempts = waitForPreviousInstance ? RestartWaitAttempts : 1;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                bool isFirstInstance;
                var mutex = new Mutex(true, SingleInstanceMutexName, out isFirstInstance);

                if (isFirstInstance)
                {
                    return mutex;
                }

                mutex.Dispose();

                if (attempt < attempts - 1)
                {
                    Thread.Sleep(RestartWaitIntervalMilliseconds);
                }
            }

            return null;
        }

        /// <summary>Builds the whole object graph of the application and runs it. This is the only place that knows every piece.</summary>
        private static void Run(ErrorLogger logger, string baseDirectory, SettingsService settingsService, AppSettings startupSettings)
        {
            string databasePath = Path.Combine(baseDirectory, DatabaseFileName);

            var connectionFactory = new DatabaseConnectionFactory(databasePath);
            new DatabaseInitializer(connectionFactory).Initialize();
            StartupProfiler.Mark("Database ready");

            IGameRepository repository = new GameRepository(connectionFactory);
            var assetLocator = new AssetLocator(Path.Combine(baseDirectory, AssetsFolderName));
            var launcher = new EmulatorLauncher(Path.Combine(Path.GetTempPath(), TemporaryFolderName, ExtractedRomsFolderName));
            launcher.DeleteOldExtractedFiles(TimeSpan.FromDays(7));
            var scanner = new RomScanner(repository);
            var romImporter = new RomImporter(repository);
            var datImporter = new DatCatalogImporter(repository);
            var imageImporter = new ImageImporter(assetLocator);
            var metadataImporter = new MetadataImporter(repository);
            var imageFolderImporter = new ImageFolderImporter(
                repository,
                assetLocator,
                imageImporter,
                Path.Combine(baseDirectory, LogFolderName, ImageReportFileName));
            var imageDownloader = new ImageDownloader(
                assetLocator,
                imageImporter,
                Path.Combine(Path.GetTempPath(), TemporaryFolderName, DownloadsFolderName),
                Path.Combine(baseDirectory, LogFolderName, ImageDownloadReportFileName));

            EnsureAssetFolders(assetLocator, logger);

#if DEBUG
            DevelopmentSeeder.SeedIfEmpty(repository);
            DevelopmentAssetGenerator.GenerateIfMissing(repository, assetLocator);
#endif

            var catalog = new GameCatalog(repository);
            catalog.Reload();
            StartupProfiler.Mark("Catalog loaded");

            if (StartupProfiler.IsEnabled)
            {
                MeasureCatalogSpeed(catalog);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var imageCache = new ImageCache(ImageCacheBudgetInBytes))
            {
                var settingsSession = new SettingsSession(settingsService, startupSettings);
                var settingsActions = new SettingsActions(settingsSession);
                var importActions = new ImportActions(
                    catalog,
                    scanner,
                    romImporter,
                    datImporter,
                    metadataImporter,
                    imageFolderImporter,
                    imageCache,
                    settingsSession,
                    settingsActions);
                var downloadActions = new DownloadActions(catalog, imageDownloader, imageCache, settingsSession);

                var actions = new MainWindowActions(
                    new GameActions(catalog, assetLocator, imageCache, imageImporter, launcher, settingsSession, settingsActions),
                    importActions,
                    downloadActions,
                    settingsActions,
                    new GuideActions(settingsSession, catalog, assetLocator, importActions, downloadActions, settingsActions));

                var mainForm = new MainForm(catalog, assetLocator, imageCache, settingsSession, actions);
                StartupProfiler.Mark("Main window created");
                Application.Run(mainForm);
            }
        }

        /// <summary>Times the operations of the catalog that the list repeats while the user types and filters.</summary>
        private static void MeasureCatalogSpeed(GameCatalog catalog)
        {
            StartupProfiler.Measure("Catalog: first search (it also prepares the titles)", () => catalog.Search(new GameQuery { SearchText = "a" }));
            StartupProfiler.Measure("Catalog: second search", () => catalog.Search(new GameQuery { SearchText = "super" }));
            StartupProfiler.Measure("Catalog: list of genres", () => catalog.GetGenres());
            StartupProfiler.Measure("Catalog: filter hiding the alternate versions", () => catalog.Search(new GameQuery { HideAlternateVersions = true }));
        }

        private static void EnsureAssetFolders(AssetLocator assetLocator, ErrorLogger logger)
        {
            try
            {
                assetLocator.EnsureFoldersExist();
            }
            catch (IOException exception)
            {
                // Not fatal: the application works without images.
                logger.Log("The assets folders could not be created.", exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                logger.Log("The assets folders could not be created.", exception);
            }
        }

        private static void ReportUnexpectedError(ErrorLogger logger, Exception exception)
        {
            logger.Log("Unexpected error", exception);

            MessageBox.Show(
                Localizer.Format("app.unexpectedError", logger.FilePath),
                ApplicationName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
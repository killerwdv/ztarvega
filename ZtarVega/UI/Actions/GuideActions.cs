using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZtarVega.Services;

namespace ZtarVega.UI.Actions
{
    /// <summary>The actions of the "Getting started" guide. Each step uses the same actions as the menus.</summary>
    internal sealed class GuideActions : ViewActions
    {
        private readonly SettingsSession _settings;
        private readonly GameCatalog _catalog;
        private readonly AssetLocator _assetLocator;
        private readonly ImportActions _imports;
        private readonly DownloadActions _downloads;
        private readonly SettingsActions _settingsActions;

        private SetupGuideForm _form;

        public GuideActions(
            SettingsSession settings,
            GameCatalog catalog,
            AssetLocator assetLocator,
            ImportActions imports,
            DownloadActions downloads,
            SettingsActions settingsActions)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _assetLocator = assetLocator ?? throw new ArgumentNullException(nameof(assetLocator));
            _imports = imports ?? throw new ArgumentNullException(nameof(imports));
            _downloads = downloads ?? throw new ArgumentNullException(nameof(downloads));
            _settingsActions = settingsActions ?? throw new ArgumentNullException(nameof(settingsActions));
        }

        /// <summary>True when the guide opens by itself while there are steps left.</summary>
        public bool ShowAtStartup
        {
            get
            {
                return !_settings.Current.HideSetupGuide;
            }

            set
            {
                _settings.Current.HideSetupGuide = !value;

                try
                {
                    _settings.Save();
                }
                catch (IOException)
                {
                    // The preference is also saved when the application closes.
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        public SetupGuideStatus GetStatus()
        {
            return SetupGuideStatus.Create(_settings.Current, _catalog, _assetLocator);
        }

        /// <summary>Opens the guide, or brings it to the front when it is already open.</summary>
        public void ShowSetupGuide()
        {
            if (_form != null && !_form.IsDisposed)
            {
                _form.RefreshState();
                _form.Activate();
                return;
            }

            _form = new SetupGuideForm(this);

            // With the main window as its owner, the guide always stays above it, closes with it,
            // and is disabled with the rest of the window while an operation is running.
            var mainForm = MainWindow as Form;

            if (mainForm != null)
            {
                _form.Owner = mainForm;
            }

            _form.Show();
        }

        /// <summary>Opens the guide when the application starts, but only while required steps are left and the user did not turn it off.</summary>
        public void ShowOnStartupIfNeeded()
        {
            if (!ShowAtStartup || GetStatus().RequiredStepsDone)
            {
                return;
            }

            ShowSetupGuide();
        }

        public async Task RunStepAsync(SetupStep step)
        {
            switch (step)
            {
                case SetupStep.Settings:
                    _settingsActions.ShowSettingsDialog();
                    break;

                case SetupStep.Catalog:
                    await _imports.ImportCatalogFromDatAsync();
                    break;

                case SetupStep.Scan:
                    await _imports.ScanRomsAsync();
                    break;

                case SetupStep.Metadata:
                    await _imports.ImportMetadataAsync();
                    break;
            }
        }

        public Task ImportImagesFromFolderAsync()
        {
            return _imports.ImportImagesFromFolderAsync();
        }

        public Task DownloadImagesAsync()
        {
            return _downloads.DownloadImagesAsync();
        }
    }
}
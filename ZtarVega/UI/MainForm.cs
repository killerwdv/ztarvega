using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Models;
using ZtarVega.Services;
using ZtarVega.UI.Actions;

namespace ZtarVega.UI
{
    /// <summary>
    /// Main application window: game list on the left, details in the center, artwork on the right.
    /// It only builds the panels and connects their events to the actions that do the work.
    /// </summary>
    internal sealed partial class MainForm : ThemedForm, IMainView
    {
        private readonly SettingsSession _settings;
        private readonly MainWindowActions _actions;

        public MainForm(
            GameCatalog catalog,
            AssetLocator assetLocator,
            ImageCache imageCache,
            SettingsSession settings,
            MainWindowActions actions)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));

            InitializeComponent();
            ApplyLanguage();

            // Use the application icon (set in the project properties) for the window as well.
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            ThemeManager.SetTheme(_settings.Current.ThemeName);
            RestoreWindowState();

            // The actions work through this window, so they are attached to it now.
            _actions.Attach(this);

            // The side panels must be ready before the list selects its first game.
            _detailsPanel.Initialize(assetLocator, imageCache);
            _artworkPanel.Initialize(assetLocator, imageCache);

            _addGameMenuItem.Click += (sender, e) => _actions.Games.AddGame();
            _editGameMenuItem.Click += (sender, e) => _actions.Games.EditGame();
            _setImagesMenuItem.Click += (sender, e) => _actions.Games.SetImages();
            _deleteGameMenuItem.Click += (sender, e) => _actions.Games.DeleteGame();
            _toggleFavoriteMenuItem.Click += (sender, e) => _actions.Games.ToggleFavorite();
            _settingsMenuItem.Click += (sender, e) => _actions.Settings.ShowSettingsDialog();
            _aboutMenuItem.Click += OnAboutMenuItemClick;

            // Scanning, importing and downloading are asynchronous: an async lambda is the accepted way to handle
            // such an event. The work itself handles the expected errors.
            _scanRomsMenuItem.Click += async (sender, e) => await _actions.Imports.ScanRomsAsync();
            _importDatMenuItem.Click += async (sender, e) => await _actions.Imports.ImportCatalogFromDatAsync();
            _importMetadataMenuItem.Click += async (sender, e) => await _actions.Imports.ImportMetadataAsync();
            _importImagesMenuItem.Click += async (sender, e) => await _actions.Imports.ImportImagesFromFolderAsync();
            _downloadImagesMenuItem.Click += async (sender, e) => await _actions.Downloads.DownloadImagesAsync();
            _guideMenuItem.Click += (sender, e) => _actions.Guide.ShowSetupGuide();

            _artworkPanel.PlayRequested += (sender, e) => _actions.Games.Launch(e.Game);
            _artworkPanel.FavoriteToggleRequested += (sender, e) => _actions.Games.ToggleFavorite();
            _gameListPanel.GameActivated += (sender, e) => _actions.Games.Launch(e.Game);
            _gameListPanel.SelectedGameChanged += OnSelectedGameChanged;
            _gameListPanel.Initialize(catalog, ListFilterState.FromSettings(_settings.Current));
        }

        Game IMainView.SelectedGame
        {
            get { return _gameListPanel.SelectedGame; }
        }

        GameFilterMode IMainView.FilterMode
        {
            get { return _gameListPanel.FilterMode; }
        }

        void IMainView.RefreshGames()
        {
            _gameListPanel.RefreshGames();
        }

        void IMainView.RefreshGames(int gameIdToSelect)
        {
            _gameListPanel.RefreshGames(gameIdToSelect);
        }

        void IMainView.RedrawGameRows()
        {
            _gameListPanel.RedrawRows();
        }

        void IMainView.RefreshFavoriteState()
        {
            _artworkPanel.RefreshFavoriteState();
        }

        void IMainView.ReloadSelectedGameImages()
        {
            Game selectedGame = _gameListPanel.SelectedGame;
            _detailsPanel.ShowGame(selectedGame);
            _artworkPanel.ShowGame(selectedGame);
        }

        void IMainView.SetBusy(bool isBusy)
        {
            UseWaitCursor = isBusy;

            // Menus and child windows are where operations start: while one is running, none can start another.
            _menuStrip.Enabled = !isBusy;

            foreach (Form ownedForm in OwnedForms)
            {
                ownedForm.Enabled = !isBusy;
            }
        }

        void IMainView.CloseWindow()
        {
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (!e.Cancel)
            {
                SaveWindowState();
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            StartupProfiler.Mark("Window shown");
            StartupProfiler.WriteReport();
            _actions.Guide.ShowOnStartupIfNeeded();
        }

        private void OnSelectedGameChanged(object sender, GameEventArgs e)
        {
            _detailsPanel.ShowGame(e.Game);
            _artworkPanel.ShowGame(e.Game);
        }

        private void OnAboutMenuItemClick(object sender, EventArgs e)
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            MessageBox.Show(
                this,
                MessageDialogs.Caption + " " + version.ToString(3) + "\n\n" + Localizer.Get("about.description"),
                Localizer.Format("about.title", MessageDialogs.Caption),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        /// <summary>Sets the texts of the menus. The designer file only holds the English defaults.</summary>
        private void ApplyLanguage()
        {
            _gameMenuItem.Text = Localizer.Get("menu.game");
            _addGameMenuItem.Text = Localizer.Get("menu.game.add");
            _editGameMenuItem.Text = Localizer.Get("menu.game.edit");
            _setImagesMenuItem.Text = Localizer.Get("menu.game.setImages");
            _deleteGameMenuItem.Text = Localizer.Get("menu.game.delete");
            _toggleFavoriteMenuItem.Text = Localizer.Get("menu.game.toggleFavorite");
            _toolsMenuItem.Text = Localizer.Get("menu.tools");
            _importDatMenuItem.Text = Localizer.Get("menu.tools.importDat");
            _importMetadataMenuItem.Text = Localizer.Get("menu.tools.importMetadata");
            _importImagesMenuItem.Text = Localizer.Get("menu.tools.importImages");
            _downloadImagesMenuItem.Text = Localizer.Get("menu.tools.downloadImages");
            _scanRomsMenuItem.Text = Localizer.Get("menu.tools.scanRoms");
            _settingsMenuItem.Text = Localizer.Get("menu.tools.settings");
            _helpMenuItem.Text = Localizer.Get("menu.help");
            _guideMenuItem.Text = Localizer.Get("menu.help.guide");
            _aboutMenuItem.Text = Localizer.Get("menu.help.about");
        }

        private void RestoreWindowState()
        {
            AppSettings settings = _settings.Current;

            if (!settings.WindowLeft.HasValue || !settings.WindowTop.HasValue ||
                !settings.WindowWidth.HasValue || !settings.WindowHeight.HasValue)
            {
                return;
            }

            var bounds = new Rectangle(
                settings.WindowLeft.Value,
                settings.WindowTop.Value,
                settings.WindowWidth.Value,
                settings.WindowHeight.Value);

            // A saved position can become invalid (for example, a monitor was disconnected).
            // In that case the default position of the window is kept.
            if (bounds.Width <= 0 || bounds.Height <= 0 || !IsVisibleOnAnyScreen(bounds))
            {
                return;
            }

            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;

            if (settings.IsWindowMaximized)
            {
                WindowState = FormWindowState.Maximized;
            }
        }

        private void SaveWindowState()
        {
            AppSettings settings = _settings.Current;

            _gameListPanel.GetFilterState().WriteTo(settings);

            // When the window is maximized or minimized, RestoreBounds holds its normal size and position.
            Rectangle bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;

            settings.WindowLeft = bounds.Left;
            settings.WindowTop = bounds.Top;
            settings.WindowWidth = bounds.Width;
            settings.WindowHeight = bounds.Height;
            settings.IsWindowMaximized = WindowState == FormWindowState.Maximized;

            try
            {
                _settings.Save();
            }
            catch (IOException)
            {
                // The application is closing: failing to remember the window position is not worth a message.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static bool IsVisibleOnAnyScreen(Rectangle bounds)
        {
            // Only the title bar has to be reachable, so the window can always be dragged back.
            var titleBar = new Rectangle(bounds.Left, bounds.Top, bounds.Width, SystemInformation.CaptionHeight);

            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(titleBar))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
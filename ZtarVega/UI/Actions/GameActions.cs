using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Models;
using ZtarVega.Services;

namespace ZtarVega.UI.Actions
{
    /// <summary>The actions on a game: add, edit, delete, favorites, images and launching it.</summary>
    internal sealed class GameActions : ViewActions
    {
        private readonly GameCatalog _catalog;
        private readonly AssetLocator _assetLocator;
        private readonly ImageCache _imageCache;
        private readonly ImageImporter _imageImporter;
        private readonly EmulatorLauncher _launcher;
        private readonly SettingsSession _settings;
        private readonly SettingsActions _settingsActions;

        public GameActions(
            GameCatalog catalog,
            AssetLocator assetLocator,
            ImageCache imageCache,
            ImageImporter imageImporter,
            EmulatorLauncher launcher,
            SettingsSession settings,
            SettingsActions settingsActions)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _assetLocator = assetLocator ?? throw new ArgumentNullException(nameof(assetLocator));
            _imageCache = imageCache ?? throw new ArgumentNullException(nameof(imageCache));
            _imageImporter = imageImporter ?? throw new ArgumentNullException(nameof(imageImporter));
            _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settingsActions = settingsActions ?? throw new ArgumentNullException(nameof(settingsActions));
        }

        public void AddGame()
        {
            var newGame = new Game
            {
                Title = string.Empty,
                Region = GameRegion.World,
                RomType = RomType.Original
            };

            using (var dialog = new GameEditForm(newGame, true, _catalog))
            {
                if (dialog.ShowDialog(MainWindow) != DialogResult.OK)
                {
                    return;
                }

                Game addedGame = dialog.EditedGame;

                if (!TrySave(() => _catalog.AddGame(addedGame), Localizer.Get("main.game.addFailed")))
                {
                    return;
                }

                MainWindow.RefreshGames(addedGame.Id);
            }
        }

        public void EditGame()
        {
            Game selectedGame = MainWindow.SelectedGame;

            if (selectedGame == null)
            {
                return;
            }

            using (var dialog = new GameEditForm(selectedGame, false, _catalog))
            {
                if (dialog.ShowDialog(MainWindow) != DialogResult.OK)
                {
                    return;
                }

                Game editedGame = dialog.EditedGame;

                if (!TrySave(() => _catalog.UpdateGame(editedGame), Localizer.Get("main.game.saveFailed")))
                {
                    return;
                }

                MainWindow.RefreshGames(editedGame.Id);
            }
        }

        public void SetImages()
        {
            Game selectedGame = MainWindow.SelectedGame;

            if (selectedGame == null)
            {
                return;
            }

            IReadOnlyList<string> changedPaths;

            using (var dialog = new GameImagesForm(selectedGame, _assetLocator, _imageImporter))
            {
                dialog.ShowDialog(MainWindow);
                changedPaths = dialog.ChangedPaths;
            }

            if (changedPaths.Count == 0)
            {
                return;
            }

            // The panels let go of their images first: the cache disposes the old ones, and a control
            // that still showed a disposed image would fail when it repainted.
            MainWindow.ReloadSelectedGameImages();

            foreach (string path in changedPaths)
            {
                _imageCache.Invalidate(path);
            }
        }

        public void DeleteGame()
        {
            Game selectedGame = MainWindow.SelectedGame;

            if (selectedGame == null)
            {
                return;
            }

            bool confirmed = MessageDialogs.Ask(
                MainWindow,
                Localizer.Format("main.game.deleteConfirm", selectedGame.DisplayTitle),
                MessageBoxIcon.Warning,
                noIsDefault: true);

            if (!confirmed)
            {
                return;
            }

            if (TrySave(() => _catalog.DeleteGame(selectedGame), Localizer.Get("main.game.deleteFailed")))
            {
                MainWindow.RefreshGames();
            }
        }

        public void ToggleFavorite()
        {
            Game game = MainWindow.SelectedGame;

            if (game == null)
            {
                return;
            }

            try
            {
                _catalog.SetFavorite(game, !game.IsFavorite);
            }
            catch (DbException exception)
            {
                MessageDialogs.ShowWarning(MainWindow, Localizer.Format("main.favorite.saveFailed", exception.Message));
                return;
            }

            MainWindow.RefreshFavoriteState();

            // Only the "favorites" view changes its content when a favorite is toggled.
            if (MainWindow.FilterMode == GameFilterMode.FavoritesOnly)
            {
                MainWindow.RefreshGames();
            }
            else
            {
                MainWindow.RedrawGameRows();
            }
        }

        public void Launch(Game game)
        {
            if (game == null)
            {
                return;
            }

            LaunchResult result = _launcher.Launch(game, _settings.Current);

            if (result.Succeeded)
            {
                RegisterPlay(game);
                return;
            }

            bool isConfigurationProblem =
                result.Failure == LaunchFailure.EmulatorNotConfigured ||
                result.Failure == LaunchFailure.RomsFolderNotConfigured;

            if (isConfigurationProblem)
            {
                _settingsActions.AskToOpenSettings(result.ErrorMessage);
                return;
            }

            MessageDialogs.ShowWarning(MainWindow, result.ErrorMessage);
        }

        private void RegisterPlay(Game game)
        {
            try
            {
                _catalog.RegisterPlay(game);
            }
            catch (DbException)
            {
                // The emulator is already running; failing to record the history is not worth interrupting the user.
                return;
            }

            // In the "recently played" view the launched game moves to the top.
            if (MainWindow.FilterMode == GameFilterMode.RecentlyPlayed)
            {
                MainWindow.RefreshGames();
            }
        }

        private bool TrySave(Action saveAction, string failureMessage)
        {
            try
            {
                saveAction();
                return true;
            }
            catch (DbException exception)
            {
                MessageDialogs.ShowWarning(MainWindow, failureMessage + "\n\n" + exception.Message);
                return false;
            }
        }
    }
}
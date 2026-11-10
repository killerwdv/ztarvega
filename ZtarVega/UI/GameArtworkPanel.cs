using System;
using System.Drawing;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Models;
using ZtarVega.Services;

namespace ZtarVega.UI
{
    /// <summary>Right panel: box art, cartridge image, favorite button and the Play button.</summary>
    internal sealed class GameArtworkPanel : UserControl
    {
        private const int ImageLoadDelayMilliseconds = 120;

        private const int BoxArtMaxWidth = 400;
        private const int BoxArtMaxHeight = 560;
        private const int CartridgeMaxWidth = 320;
        private const int CartridgeMaxHeight = 240;

        private const int FavoriteButtonHeight = 38;
        private const int PlayButtonHeight = 64;
        private const float BoxArtRowPercent = 62F;
        private const float CartridgeRowPercent = 38F;

        private static string AddToFavoritesText
        {
            get { return "\u2606  " + Localizer.Get("artwork.favorite.add"); }
        }

        private static string RemoveFromFavoritesText
        {
            get { return "\u2605  " + Localizer.Get("artwork.favorite.remove"); }
        }

        private readonly Font _playButtonFont;
        private readonly ImageView _boxArtView;
        private readonly ImageView _cartridgeView;
        private readonly Button _favoriteButton;
        private readonly Button _playButton;
        private readonly Timer _imageLoadTimer;

        private AssetLocator _assetLocator;
        private ImageCache _imageCache;
        private Game _currentGame;

        public GameArtworkPanel()
        {
            _playButtonFont = new Font(Font.FontFamily, 16F, FontStyle.Bold);

            _boxArtView = new ImageView
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 6),
                PlaceholderText = Localizer.Get("artwork.noBoxArt")
            };

            _cartridgeView = new ImageView
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 0, 0),
                PlaceholderText = Localizer.Get("artwork.noCartridge")
            };

            _favoriteButton = new Button
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 0, 0),
                Text = AddToFavoritesText,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _favoriteButton.Click += OnFavoriteButtonClick;

            _playButton = new Button
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 0, 0),
                Text = Localizer.Get("artwork.play"),
                Tag = ThemeManager.AccentButtonTag,
                Font = _playButtonFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _playButton.FlatAppearance.BorderSize = 0;
            _playButton.Click += OnPlayButtonClick;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(8)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, BoxArtRowPercent));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, CartridgeRowPercent));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, FavoriteButtonHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, PlayButtonHeight));
            layout.Controls.Add(_boxArtView, 0, 0);
            layout.Controls.Add(_cartridgeView, 0, 1);
            layout.Controls.Add(_favoriteButton, 0, 2);
            layout.Controls.Add(_playButton, 0, 3);
            Controls.Add(layout);

            _imageLoadTimer = new Timer { Interval = ImageLoadDelayMilliseconds };
            _imageLoadTimer.Tick += OnImageLoadTimerTick;

            RefreshFavoriteState();
            UpdatePlayButtonAppearance();
        }

        /// <summary>Raised when the user presses the Play button.</summary>
        public event EventHandler<GameEventArgs> PlayRequested;

        /// <summary>Raised when the user presses the favorite button.</summary>
        public event EventHandler FavoriteToggleRequested;

        /// <summary>Provides the services needed to find and load images.</summary>
        public void Initialize(AssetLocator assetLocator, ImageCache imageCache)
        {
            _assetLocator = assetLocator ?? throw new ArgumentNullException(nameof(assetLocator));
            _imageCache = imageCache ?? throw new ArgumentNullException(nameof(imageCache));
        }

        /// <summary>Shows the artwork of the given game, or an empty state when the game is null.</summary>
        public void ShowGame(Game game)
        {
            if (game != null && (_assetLocator == null || _imageCache == null))
            {
                throw new InvalidOperationException("Initialize must be called before showing a game.");
            }

            _currentGame = game;

            // Images wait until the selection settles, like in the details panel.
            _imageLoadTimer.Stop();
            _boxArtView.Image = null;
            _cartridgeView.Image = null;

            _playButton.Enabled = game != null;
            UpdatePlayButtonAppearance();
            RefreshFavoriteState();

            if (game != null)
            {
                _imageLoadTimer.Start();
            }
        }

        /// <summary>Updates the favorite button to match the current game. Does not reload any image.</summary>
        public void RefreshFavoriteState()
        {
            _favoriteButton.Enabled = _currentGame != null;
            _favoriteButton.Text = _currentGame != null && _currentGame.IsFavorite
                ? RemoveFromFavoritesText
                : AddToFavoritesText;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _imageLoadTimer.Dispose();
                _playButtonFont.Dispose();
            }

            base.Dispose(disposing);
        }

        private void UpdatePlayButtonAppearance()
        {
            ThemeManager.StyleAccentButton(_playButton);
        }

        private void OnImageLoadTimerTick(object sender, EventArgs e)
        {
            _imageLoadTimer.Stop();

            if (_currentGame == null)
            {
                return;
            }

            _boxArtView.Image = _imageCache.Get(_assetLocator.FindBoxArt(_currentGame), BoxArtMaxWidth, BoxArtMaxHeight);
            _cartridgeView.Image = _imageCache.Get(_assetLocator.FindCartridge(_currentGame), CartridgeMaxWidth, CartridgeMaxHeight);
        }

        private void OnFavoriteButtonClick(object sender, EventArgs e)
        {
            FavoriteToggleRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnPlayButtonClick(object sender, EventArgs e)
        {
            if (_currentGame != null)
            {
                PlayRequested?.Invoke(this, new GameEventArgs(_currentGame));
            }
        }
    }
}
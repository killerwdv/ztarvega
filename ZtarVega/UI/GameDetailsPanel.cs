using System;
using System.Drawing;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Models;
using ZtarVega.Services;

namespace ZtarVega.UI
{
    /// <summary>Center panel: game logo, title, information fields and a screenshot gallery.</summary>
    internal sealed class GameDetailsPanel : UserControl
    {
        private const int ImageLoadDelayMilliseconds = 120;

        private const int LogoMaxWidth = 480;
        private const int LogoMaxHeight = 120;
        private const int PreviewMaxWidth = 512;
        private const int PreviewMaxHeight = 480;
        private const int ThumbnailMaxWidth = 160;
        private const int ThumbnailMaxHeight = 120;

        private const int LogoViewHeight = 90;
        private const int ThumbnailViewHeight = 72;

        private const string EmptyValueText = "-";

        private readonly Font _titleFont;
        private readonly Label _titleLabel;
        private readonly Label _yearValueLabel;
        private readonly Label _regionValueLabel;
        private readonly Label _languageValueLabel;
        private readonly Label _romTypeValueLabel;
        private readonly Label _developerValueLabel;
        private readonly Label _playersValueLabel;
        private readonly Label _tagsValueLabel;
        private readonly ImageView _logoView;
        private readonly ImageView _previewView;
        private readonly ImageView[] _thumbnailViews = new ImageView[AssetLocator.ScreenshotCount];
        private readonly string[] _screenshotPaths = new string[AssetLocator.ScreenshotCount];
        private readonly Timer _imageLoadTimer;

        private AssetLocator _assetLocator;
        private ImageCache _imageCache;
        private Game _currentGame;

        public GameDetailsPanel()
        {
            _titleFont = new Font(Font.FontFamily, 16F, FontStyle.Bold);

            _logoView = new ImageView
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 6),
                PlaceholderText = Localizer.Get("details.noLogo")
            };

            _titleLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoEllipsis = true,
                Font = _titleFont,
                Height = _titleFont.Height + 6,
                Margin = new Padding(0, 0, 0, 6)
            };

            var infoTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2
            };
            infoTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            infoTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _yearValueLabel = AddInfoRow(infoTable, Localizer.Get("details.field.year"));
            _regionValueLabel = AddInfoRow(infoTable, Localizer.Get("details.field.region"));
            _languageValueLabel = AddInfoRow(infoTable, Localizer.Get("details.field.language"));
            _romTypeValueLabel = AddInfoRow(infoTable, Localizer.Get("details.field.romType"));
            _developerValueLabel = AddInfoRow(infoTable, Localizer.Get("details.field.developer"));
            _playersValueLabel = AddInfoRow(infoTable, Localizer.Get("details.field.players"));
            _tagsValueLabel = AddInfoRow(infoTable, Localizer.Get("details.field.tags"));

            _previewView = new ImageView
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 0, 6),
                PlaceholderText = Localizer.Get("details.noScreenshots")
            };

            var thumbnailTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = AssetLocator.ScreenshotCount,
                RowCount = 1
            };
            thumbnailTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            for (int index = 0; index < AssetLocator.ScreenshotCount; index++)
            {
                thumbnailTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / AssetLocator.ScreenshotCount));

                var thumbnailView = new ImageView
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(2),
                    Cursor = Cursors.Hand
                };
                thumbnailView.Click += OnThumbnailClick;

                _thumbnailViews[index] = thumbnailView;
                thumbnailTable.Controls.Add(thumbnailView, index, 0);
            }

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(8)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, LogoViewHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, ThumbnailViewHeight));
            layout.Controls.Add(_logoView, 0, 0);
            layout.Controls.Add(_titleLabel, 0, 1);
            layout.Controls.Add(infoTable, 0, 2);
            layout.Controls.Add(_previewView, 0, 3);
            layout.Controls.Add(thumbnailTable, 0, 4);
            Controls.Add(layout);

            _imageLoadTimer = new Timer { Interval = ImageLoadDelayMilliseconds };
            _imageLoadTimer.Tick += OnImageLoadTimerTick;

            ShowGame(null);
        }

        /// <summary>Provides the services needed to find and load images. Must be called before <see cref="ShowGame"/> receives a game.</summary>
        public void Initialize(AssetLocator assetLocator, ImageCache imageCache)
        {
            _assetLocator = assetLocator ?? throw new ArgumentNullException(nameof(assetLocator));
            _imageCache = imageCache ?? throw new ArgumentNullException(nameof(imageCache));
        }

        /// <summary>Shows the given game, or an empty state when the game is null.</summary>
        public void ShowGame(Game game)
        {
            if (game != null && (_assetLocator == null || _imageCache == null))
            {
                throw new InvalidOperationException("Initialize must be called before showing a game.");
            }

            _currentGame = game;

            // The text changes immediately; the images wait until the selection settles.
            _imageLoadTimer.Stop();
            ClearImages();
            UpdateText(game);

            if (game != null)
            {
                _imageLoadTimer.Start();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _imageLoadTimer.Dispose();
                _titleFont.Dispose();
            }

            base.Dispose(disposing);
        }

        private static Label AddInfoRow(TableLayoutPanel table, string caption)
        {
            var captionLabel = new Label
            {
                Text = caption,
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(0, 3, 12, 3)
            };

            var valueLabel = new Label
            {
                Text = EmptyValueText,
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoEllipsis = true,
                Height = captionLabel.PreferredHeight,
                Margin = new Padding(0, 3, 0, 3)
            };

            int row = table.RowStyles.Count;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowCount = table.RowStyles.Count;
            table.Controls.Add(captionLabel, 0, row);
            table.Controls.Add(valueLabel, 1, row);

            return valueLabel;
        }

        private void UpdateText(Game game)
        {
            if (game == null)
            {
                _titleLabel.Text = Localizer.Get("details.noGame");
                SetValues(EmptyValueText, EmptyValueText, EmptyValueText, EmptyValueText, EmptyValueText, EmptyValueText, EmptyValueText);
                return;
            }

            _titleLabel.Text = game.DisplayTitle;
            SetValues(
                game.Year.HasValue ? game.Year.Value.ToString() : EmptyValueText,
                Localizer.RegionName(game.Region),
                ValueOrDash(game.Language),
                Localizer.RomTypeName(game.RomType),
                ValueOrDash(game.Developer),
                game.Players.HasValue ? game.Players.Value.ToString() : EmptyValueText,
                ValueOrDash(game.Tags));
        }

        private void SetValues(string year, string region, string language, string romType, string developer, string players, string tags)
        {
            _yearValueLabel.Text = year;
            _regionValueLabel.Text = region;
            _languageValueLabel.Text = language;
            _romTypeValueLabel.Text = romType;
            _developerValueLabel.Text = developer;
            _playersValueLabel.Text = players;
            _tagsValueLabel.Text = tags;
        }

        private static string ValueOrDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? EmptyValueText : value;
        }

        private void ClearImages()
        {
            // Detach the images from the views first: the cache may dispose them later.
            _logoView.Image = null;
            _previewView.Image = null;

            for (int index = 0; index < AssetLocator.ScreenshotCount; index++)
            {
                _thumbnailViews[index].Image = null;
                _thumbnailViews[index].IsHighlighted = false;
                _screenshotPaths[index] = null;
            }
        }

        private void OnImageLoadTimerTick(object sender, EventArgs e)
        {
            _imageLoadTimer.Stop();

            if (_currentGame != null)
            {
                LoadImages(_currentGame);
            }
        }

        private void LoadImages(Game game)
        {
            _logoView.Image = _imageCache.Get(_assetLocator.FindLogo(game), LogoMaxWidth, LogoMaxHeight);

            int firstAvailableIndex = -1;

            for (int index = 0; index < AssetLocator.ScreenshotCount; index++)
            {
                string path = _assetLocator.FindScreenshot(game, index + 1);
                _screenshotPaths[index] = path;
                _thumbnailViews[index].Image = _imageCache.Get(path, ThumbnailMaxWidth, ThumbnailMaxHeight);

                if (path != null && firstAvailableIndex < 0)
                {
                    firstAvailableIndex = index;
                }
            }

            ShowScreenshot(firstAvailableIndex);
        }

        private void ShowScreenshot(int index)
        {
            for (int thumbnailIndex = 0; thumbnailIndex < AssetLocator.ScreenshotCount; thumbnailIndex++)
            {
                _thumbnailViews[thumbnailIndex].IsHighlighted = thumbnailIndex == index;
            }

            _previewView.Image = index < 0
                ? null
                : _imageCache.Get(_screenshotPaths[index], PreviewMaxWidth, PreviewMaxHeight);
        }

        private void OnThumbnailClick(object sender, EventArgs e)
        {
            int index = Array.IndexOf(_thumbnailViews, sender as ImageView);

            if (index >= 0 && _screenshotPaths[index] != null)
            {
                ShowScreenshot(index);
            }
        }
    }
}
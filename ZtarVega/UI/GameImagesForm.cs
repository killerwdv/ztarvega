using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Models;
using ZtarVega.Services;

namespace ZtarVega.UI
{
    /// <summary>
    /// Modal dialog where the user picks the images of a game: box art, cartridge, logo and screenshots.
    /// Choices are only applied when the user presses OK. Images the game does not have of its own are
    /// shown as inherited from its base game.
    /// </summary>
    internal sealed class GameImagesForm : ThemedForm
    {
        private const int PreviewWidth = 128;
        private const int PreviewHeight = 80;

        // Images are stored reduced to these sizes (they are never enlarged).
        private static readonly Size BoxArtStoredSize = ImageTarget.BoxArtMaxSize;
        private static readonly Size CartridgeStoredSize = ImageTarget.CartridgeMaxSize;
        private static readonly Size LogoStoredSize = ImageTarget.LogoMaxSize;
        private static readonly Size ScreenshotStoredSize = ImageTarget.ScreenshotMaxSize;

        private readonly Game _game;
        private readonly AssetLocator _locator;
        private readonly ImageImporter _importer;
        private readonly List<ImageSlot> _slots = new List<ImageSlot>();
        private readonly List<string> _changedPaths = new List<string>();

        public GameImagesForm(Game game, AssetLocator locator, ImageImporter importer)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            _importer = importer ?? throw new ArgumentNullException(nameof(importer));

            Text = Localizer.Format("images.title", game.DisplayTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(660, 520);

            var rowsLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4,
                Location = Point.Empty,
                Padding = new Padding(8)
            };

            AddSlot(rowsLayout, Localizer.Get("images.slot.boxArt"), AssetLocator.BoxArtFolderName, null, BoxArtStoredSize);
            AddSlot(rowsLayout, Localizer.Get("images.slot.cartridge"), AssetLocator.CartridgeFolderName, null, CartridgeStoredSize);
            AddSlot(rowsLayout, Localizer.Get("images.slot.logo"), AssetLocator.LogoFolderName, null, LogoStoredSize);

            for (int number = 1; number <= AssetLocator.ScreenshotCount; number++)
            {
                AddSlot(
                    rowsLayout,
                    Localizer.Format("images.slot.screenshot", number),
                    AssetLocator.ScreenshotFolderName,
                    number,
                    ScreenshotStoredSize);
            }

            // The list of images scrolls, so the dialog fits small screens.
            var scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            scrollPanel.Controls.Add(rowsLayout);

            var okButton = new Button { Text = Localizer.Get("common.ok"), AutoSize = true, MinimumSize = new Size(80, 0) };
            okButton.Click += OnOkButtonClick;

            var cancelButton = new Button
            {
                Text = Localizer.Get("common.cancel"),
                AutoSize = true,
                MinimumSize = new Size(80, 0),
                DialogResult = DialogResult.Cancel
            };

            var buttonsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(8)
            };
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(okButton);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(scrollPanel, 0, 0);
            layout.Controls.Add(buttonsPanel, 0, 1);
            Controls.Add(layout);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            // Every slot is ready: work out what each one shows (own, inherited or nothing).
            RefreshAllSlots();
        }

        /// <summary>
        /// Every file that was created or deleted. It is filled even when the dialog is cancelled after a
        /// failed save, so the caller can always refresh its caches.
        /// </summary>
        public IReadOnlyList<string> ChangedPaths
        {
            get { return _changedPaths; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (ImageSlot slot in _slots)
                {
                    slot.View.Image = null;

                    if (slot.PreviewBitmap != null)
                    {
                        slot.PreviewBitmap.Dispose();
                    }
                }
            }

            base.Dispose(disposing);
        }

        private void AddSlot(TableLayoutPanel layout, string caption, string folderName, int? screenshotNumber, Size storedSize)
        {
            var view = new ImageView
            {
                Size = new Size(PreviewWidth, PreviewHeight),
                Margin = new Padding(4),
                PlaceholderText = Localizer.Get("images.noImage")
            };

            var captionLabel = new Label
            {
                Text = caption,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 0, 12, 0)
            };

            var statusLabel = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(8, 0, 3, 0)
            };

            var chooseButton = new Button { Text = Localizer.Get("common.choose"), AutoSize = true };
            var removeButton = new Button { Text = Localizer.Get("common.remove"), AutoSize = true };

            var buttonsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                WrapContents = false
            };
            buttonsPanel.Controls.Add(chooseButton);
            buttonsPanel.Controls.Add(removeButton);

            var slot = new ImageSlot(caption, folderName, screenshotNumber, storedSize, view, statusLabel, removeButton);
            slot.ExistingPath = _locator.FindImage(folderName, _game, screenshotNumber);
            _slots.Add(slot);

            chooseButton.Click += (sender, e) => OnChooseImage(slot);
            removeButton.Click += (sender, e) => OnRemoveImage(slot);

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowCount = layout.RowStyles.Count;
            int row = layout.RowCount - 1;

            layout.Controls.Add(captionLabel, 0, row);
            layout.Controls.Add(view, 1, row);
            layout.Controls.Add(buttonsPanel, 2, row);
            layout.Controls.Add(statusLabel, 3, row);
        }

        private void OnChooseImage(ImageSlot slot)
        {
            using (var dialog = new OpenFileDialog
            {
                Title = Localizer.Format("images.chooseTitle", slot.Caption),
                Filter = Localizer.Get("images.chooseFilter"),
                CheckFileExists = true
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                Bitmap preview = ImageCache.TryLoadScaled(dialog.FileName, PreviewWidth, PreviewHeight);

                if (preview == null)
                {
                    MessageBox.Show(this, Localizer.Get("images.error.invalidImage"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                slot.PendingSourcePath = dialog.FileName;
                slot.PendingRemoval = false;

                // The preview is already loaded, so the refresh below does not load it again.
                SetPreview(slot, preview, dialog.FileName);
                RefreshAllSlots();
            }
        }

        private void OnRemoveImage(ImageSlot slot)
        {
            slot.PendingSourcePath = null;
            slot.PendingRemoval = slot.ExistingPath != null;
            RefreshAllSlots();
        }

        /// <summary>
        /// Recomputes what every slot shows. It is done for all slots at once because screenshots are inherited
        /// as a set: choosing one of its own makes the other screenshots stop being inherited.
        /// </summary>
        private void RefreshAllSlots()
        {
            bool hasOwnScreenshots = _slots.Any(slot => slot.ScreenshotNumber.HasValue && HasOwnImageAfterSave(slot));

            foreach (ImageSlot slot in _slots)
            {
                UpdateInheritedImage(slot, hasOwnScreenshots);
                RefreshPreview(slot);
                UpdateSlotState(slot);
            }
        }

        /// <summary>Looks for the image the slot would show if the game had none of its own: the one of its base game.</summary>
        private void UpdateInheritedImage(ImageSlot slot, bool hasOwnScreenshots)
        {
            slot.InheritedPath = null;
            slot.InheritedFrom = null;

            if (_game.BaseGame == null)
            {
                return;
            }

            // Screenshots are inherited as a set: only while the game has none of its own.
            if (slot.ScreenshotNumber.HasValue && hasOwnScreenshots)
            {
                return;
            }

            Game owner;
            slot.InheritedPath = _locator.FindInheritedImage(slot.FolderName, _game.BaseGame, slot.ScreenshotNumber, out owner);
            slot.InheritedFrom = owner;
        }

        /// <summary>True when, after saving, the game will have an image of its own in this slot.</summary>
        private static bool HasOwnImageAfterSave(ImageSlot slot)
        {
            return slot.PendingSourcePath != null || (slot.ExistingPath != null && !slot.PendingRemoval);
        }

        /// <summary>Shows what the game will use after saving: the new image, its own image, the inherited one, or nothing.</summary>
        private static void RefreshPreview(ImageSlot slot)
        {
            string pathToShow = slot.PendingSourcePath ?? (HasOwnImageAfterSave(slot) ? slot.ExistingPath : slot.InheritedPath);

            if (string.Equals(pathToShow, slot.PreviewPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Bitmap bitmap = pathToShow == null ? null : ImageCache.TryLoadScaled(pathToShow, PreviewWidth, PreviewHeight);
            SetPreview(slot, bitmap, pathToShow);
        }

        /// <summary>Replaces the preview of a slot. The dialog owns the preview bitmaps (they do not come from the cache).</summary>
        private static void SetPreview(ImageSlot slot, Bitmap bitmap, string path)
        {
            slot.View.Image = null;

            if (slot.PreviewBitmap != null)
            {
                slot.PreviewBitmap.Dispose();
            }

            slot.PreviewBitmap = bitmap;
            slot.PreviewPath = path;
            slot.View.Image = bitmap;
        }

        private static void UpdateSlotState(ImageSlot slot)
        {
            if (slot.PendingSourcePath != null)
            {
                slot.StatusLabel.Text = Localizer.Get("images.status.new");
            }
            else if (slot.ExistingPath != null && !slot.PendingRemoval)
            {
                slot.StatusLabel.Text = Localizer.Get("images.status.current");
            }
            else if (slot.InheritedPath != null)
            {
                slot.StatusLabel.Text = Localizer.Format(
                    slot.PendingRemoval ? "images.status.inheritedAfterSaving" : "images.status.inherited",
                    slot.InheritedFrom.DisplayTitle);
            }
            else if (slot.PendingRemoval)
            {
                slot.StatusLabel.Text = Localizer.Get("images.status.willBeRemoved");
            }
            else
            {
                slot.StatusLabel.Text = Localizer.Get("images.noImage");
            }

            slot.RemoveButton.Enabled = HasOwnImageAfterSave(slot);
        }

        private void OnOkButtonClick(object sender, EventArgs e)
        {
            try
            {
                foreach (ImageSlot slot in _slots)
                {
                    if (slot.PendingSourcePath != null)
                    {
                        _changedPaths.AddRange(_importer.Import(
                            slot.PendingSourcePath,
                            slot.FolderName,
                            _game,
                            slot.ScreenshotNumber,
                            slot.StoredSize.Width,
                            slot.StoredSize.Height));

                        // Applied: a retry after a later failure must not repeat it.
                        slot.PendingSourcePath = null;
                    }
                    else if (slot.PendingRemoval)
                    {
                        _changedPaths.AddRange(_importer.Remove(slot.FolderName, _game, slot.ScreenshotNumber));
                        slot.PendingRemoval = false;
                    }
                }
            }
            catch (InvalidDataException exception)
            {
                ShowSaveError(exception);
                return;
            }
            catch (IOException exception)
            {
                ShowSaveError(exception);
                return;
            }
            catch (UnauthorizedAccessException exception)
            {
                ShowSaveError(exception);
                return;
            }
            catch (ExternalException exception)
            {
                // GDI+ reports its failures with this exception.
                ShowSaveError(exception);
                return;
            }

            DialogResult = DialogResult.OK;
        }

        private void ShowSaveError(Exception exception)
        {
            MessageBox.Show(
                this,
                Localizer.Format("images.error.saveFailed", exception.Message),
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        /// <summary>The state of one image of the dialog (box art, logo, one screenshot...).</summary>
        private sealed class ImageSlot
        {
            public ImageSlot(
                string caption,
                string folderName,
                int? screenshotNumber,
                Size storedSize,
                ImageView view,
                Label statusLabel,
                Button removeButton)
            {
                Caption = caption;
                FolderName = folderName;
                ScreenshotNumber = screenshotNumber;
                StoredSize = storedSize;
                View = view;
                StatusLabel = statusLabel;
                RemoveButton = removeButton;
            }

            public string Caption { get; }

            public string FolderName { get; }

            public int? ScreenshotNumber { get; }

            public Size StoredSize { get; }

            public ImageView View { get; }

            public Label StatusLabel { get; }

            public Button RemoveButton { get; }

            /// <summary>The file that holds the image when the dialog opened, or null.</summary>
            public string ExistingPath { get; set; }

            /// <summary>The image the game inherits from its base game when it has none of its own, or null.</summary>
            public string InheritedPath { get; set; }

            /// <summary>The game the inherited image belongs to.</summary>
            public Game InheritedFrom { get; set; }

            /// <summary>The file chosen by the user that is not saved yet, or null.</summary>
            public string PendingSourcePath { get; set; }

            public bool PendingRemoval { get; set; }

            /// <summary>Preview shown in the dialog. Owned by the dialog, never by the cache.</summary>
            public Bitmap PreviewBitmap { get; set; }

            /// <summary>The file the current preview was loaded from, to avoid loading it again.</summary>
            public string PreviewPath { get; set; }
        }
    }
}
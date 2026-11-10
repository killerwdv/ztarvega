using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Models;
using ZtarVega.Services;

namespace ZtarVega.UI
{
    /// <summary>Modal dialog to create a game or edit its information.</summary>
    internal sealed class GameEditForm : ThemedForm
    {
        private const int MinimumYear = 1970;
        private const int MaximumYear = 2100;
        private const int MaximumPlayers = 8;
        private const int MaximumTextLength = 200;

        private readonly GameCatalog _catalog;
        private readonly TextBox _titleTextBox;
        private readonly TextBox _yearTextBox;
        private readonly ComboBox _regionComboBox;
        private readonly ComboBox _romTypeComboBox;
        private readonly TextBox _languageTextBox;
        private readonly TextBox _developerTextBox;
        private readonly TextBox _playersTextBox;
        private readonly TextBox _tagsTextBox;
        private readonly TextBox _baseGameTextBox;
        private readonly TextBox _romFilenameTextBox;
        private int? _baseGameId;

        public GameEditForm(Game game, bool isNewGame, GameCatalog catalog)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

            // The dialog edits a copy, so cancelling leaves the real game untouched.
            EditedGame = game.Clone();

            Text = Localizer.Get(isNewGame ? "edit.title.add" : "edit.title.edit");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 400);

            _titleTextBox = CreateTextBox(EditedGame.Title);
            _yearTextBox = CreateTextBox(EditedGame.Year.HasValue ? EditedGame.Year.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);
            _languageTextBox = CreateTextBox(EditedGame.Language);
            _developerTextBox = CreateTextBox(EditedGame.Developer);
            _playersTextBox = CreateTextBox(EditedGame.Players.HasValue ? EditedGame.Players.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);
            _tagsTextBox = CreateTextBox(EditedGame.Tags);
            _romFilenameTextBox = CreateTextBox(EditedGame.RomFilename);

            // A ROM already linked to a file by its checksum is managed by the scanner, not by the user.
            _romFilenameTextBox.ReadOnly = !string.IsNullOrEmpty(EditedGame.Crc32);

            _regionComboBox = CreateComboBox();
            foreach (GameRegion region in Enum.GetValues(typeof(GameRegion)))
            {
                _regionComboBox.Items.Add(new Option<GameRegion>(region, Localizer.RegionName(region)));
            }

            _romTypeComboBox = CreateComboBox();
            foreach (RomType romType in Enum.GetValues(typeof(RomType)))
            {
                _romTypeComboBox.Items.Add(new Option<RomType>(romType, Localizer.RomTypeName(romType)));
            }

            SelectOption(_regionComboBox, EditedGame.Region);
            SelectOption(_romTypeComboBox, EditedGame.RomType);

            _baseGameId = EditedGame.BaseGameId;
            _baseGameTextBox = new TextBox { ReadOnly = true, Dock = DockStyle.Fill, Margin = new Padding(0, 3, 6, 3) };

            var chooseBaseGameButton = new Button { Text = Localizer.Get("common.choose"), AutoSize = true, Margin = new Padding(0, 1, 6, 1) };
            chooseBaseGameButton.Click += OnChooseBaseGameClick;

            var clearBaseGameButton = new Button { Text = Localizer.Get("common.clear"), AutoSize = true, Margin = new Padding(0, 1, 0, 1) };
            clearBaseGameButton.Click += OnClearBaseGameClick;

            var baseGamePanel = new TableLayoutPanel
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0)
            };
            baseGamePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            baseGamePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            baseGamePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            baseGamePanel.Controls.Add(_baseGameTextBox, 0, 0);
            baseGamePanel.Controls.Add(chooseBaseGameButton, 1, 0);
            baseGamePanel.Controls.Add(clearBaseGameButton, 2, 0);
            UpdateBaseGameText();

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
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(okButton);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            AddRow(layout, Localizer.Get("edit.field.title"), _titleTextBox);
            AddRow(layout, Localizer.Get("details.field.year"), _yearTextBox);
            AddRow(layout, Localizer.Get("details.field.region"), _regionComboBox);
            AddRow(layout, Localizer.Get("details.field.romType"), _romTypeComboBox);
            AddRow(layout, Localizer.Get("details.field.language"), _languageTextBox);
            AddRow(layout, Localizer.Get("details.field.developer"), _developerTextBox);
            AddRow(layout, Localizer.Get("details.field.players"), _playersTextBox);
            AddRow(layout, Localizer.Get("details.field.tags"), _tagsTextBox);
            AddRow(layout, Localizer.Get("edit.field.baseGame"), baseGamePanel);
            AddRow(layout, Localizer.Get("edit.field.romFilename"), _romFilenameTextBox);

            // The last row takes the remaining space and holds the buttons.
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowCount = layout.RowStyles.Count;
            layout.Controls.Add(buttonsPanel, 0, layout.RowCount - 1);
            layout.SetColumnSpan(buttonsPanel, 2);

            Controls.Add(layout);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        /// <summary>The edited copy of the game. Only meaningful when the dialog was closed with OK.</summary>
        public Game EditedGame { get; }

        private static TextBox CreateTextBox(string text)
        {
            return new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                MaxLength = MaximumTextLength,
                Text = text ?? string.Empty
            };
        }

        private static ComboBox CreateComboBox()
        {
            return new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        private static void AddRow(TableLayoutPanel layout, string caption, Control control)
        {
            int row = layout.RowStyles.Count;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowCount = layout.RowStyles.Count;

            var captionLabel = new Label
            {
                Text = caption,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 6, 12, 6)
            };

            layout.Controls.Add(captionLabel, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        private static void SelectOption<TValue>(ComboBox comboBox, TValue value)
        {
            foreach (Option<TValue> option in comboBox.Items)
            {
                if (EqualityComparer<TValue>.Default.Equals(option.Value, value))
                {
                    comboBox.SelectedItem = option;
                    return;
                }
            }

            comboBox.SelectedIndex = 0;
        }

        private static string NullIfEmpty(string text)
        {
            string trimmed = text.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        private static bool TryParseOptionalNumber(string text, int minimum, int maximum, out int? number)
        {
            number = null;
            string trimmed = text.Trim();

            if (trimmed.Length == 0)
            {
                return true;
            }

            int parsed;

            // NumberStyles.None accepts digits only (no signs, no spaces, no separators).
            if (!int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out parsed) ||
                parsed < minimum || parsed > maximum)
            {
                return false;
            }

            number = parsed;
            return true;
        }

        private void UpdateBaseGameText()
        {
            Game baseGame = _baseGameId.HasValue ? _catalog.GetById(_baseGameId.Value) : null;
            _baseGameTextBox.Text = baseGame == null ? Localizer.Get("edit.baseGame.none") : baseGame.DisplayTitle;
        }

        private void OnChooseBaseGameClick(object sender, EventArgs e)
        {
            using (var picker = new BaseGamePickerForm(_catalog, EditedGame))
            {
                if (picker.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                _baseGameId = picker.SelectedGame.Id;
                UpdateBaseGameText();
            }
        }

        private void OnClearBaseGameClick(object sender, EventArgs e)
        {
            _baseGameId = null;
            UpdateBaseGameText();
        }

        private void OnOkButtonClick(object sender, EventArgs e)
        {
            string title = _titleTextBox.Text.Trim();

            if (title.Length == 0)
            {
                ShowValidationError(Localizer.Get("edit.error.title"), _titleTextBox);
                return;
            }

            int? year;

            if (!TryParseOptionalNumber(_yearTextBox.Text, MinimumYear, MaximumYear, out year))
            {
                ShowValidationError(Localizer.Format("edit.error.year", MinimumYear, MaximumYear), _yearTextBox);
                return;
            }

            int? players;

            if (!TryParseOptionalNumber(_playersTextBox.Text, 1, MaximumPlayers, out players))
            {
                ShowValidationError(Localizer.Format("edit.error.players", MaximumPlayers), _playersTextBox);
                return;
            }

            string romFilename = _romFilenameTextBox.Text.Trim();

            // Only a plain file name is accepted: the ROM is always looked up inside the ROMs folder.
            if (!_romFilenameTextBox.ReadOnly && romFilename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                ShowValidationError(Localizer.Get("edit.error.romFilename"), _romFilenameTextBox);
                return;
            }

            EditedGame.Title = title;
            EditedGame.Year = year;
            EditedGame.Region = ((Option<GameRegion>)_regionComboBox.SelectedItem).Value;
            EditedGame.RomType = ((Option<RomType>)_romTypeComboBox.SelectedItem).Value;
            EditedGame.Language = NullIfEmpty(_languageTextBox.Text);
            EditedGame.Developer = NullIfEmpty(_developerTextBox.Text);
            EditedGame.Players = players;
            EditedGame.Tags = NullIfEmpty(_tagsTextBox.Text);
            EditedGame.BaseGameId = _baseGameId;

            if (!_romFilenameTextBox.ReadOnly)
            {
                string newRomFilename = romFilename.Length == 0 ? null : romFilename;

                if (!string.Equals(EditedGame.RomFilename, newRomFilename, StringComparison.Ordinal))
                {
                    // Availability is unknown until the next scan verifies the file.
                    EditedGame.IsAvailable = false;
                }

                EditedGame.RomFilename = newRomFilename;
            }

            DialogResult = DialogResult.OK;
        }

        private void ShowValidationError(string message, Control controlToFocus)
        {
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            controlToFocus.Focus();
        }

        /// <summary>A drop-down entry: a value plus its display text.</summary>
        private sealed class Option<TValue>
        {
            private readonly string _text;

            public Option(TValue value, string text)
            {
                Value = value;
                _text = text;
            }

            public TValue Value { get; }

            public override string ToString()
            {
                return _text;
            }
        }
    }
}
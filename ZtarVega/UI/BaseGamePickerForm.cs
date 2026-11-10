using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Models;
using ZtarVega.Services;

namespace ZtarVega.UI
{
    /// <summary>Modal dialog to search the catalog and choose the base game of another game.</summary>
    internal sealed class BaseGamePickerForm : ThemedForm
    {
        private const int ResultLimit = 200;

        private readonly GameCatalog _catalog;
        private readonly Game _game;
        private readonly TextBox _searchTextBox;
        private readonly ListBox _gamesListBox;
        private readonly Label _hintLabel;
        private readonly Button _selectButton;

        public BaseGamePickerForm(GameCatalog catalog, Game game)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _game = game ?? throw new ArgumentNullException(nameof(game));

            Text = Localizer.Get("picker.title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(480, 420);

            _searchTextBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 6) };

            _gamesListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                DisplayMember = nameof(Game.DisplayTitle),
                IntegralHeight = false,
                Margin = new Padding(0, 0, 0, 6)
            };

            _hintLabel = new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(0, 0, 0, 6)
            };

            _selectButton = new Button { Text = Localizer.Get("common.select"), AutoSize = true, MinimumSize = new Size(80, 0) };
            _selectButton.Click += OnSelectButtonClick;

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
                Anchor = AnchorStyles.Right
            };
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(_selectButton);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(_searchTextBox, 0, 0);
            layout.Controls.Add(_gamesListBox, 0, 1);
            layout.Controls.Add(_hintLabel, 0, 2);
            layout.Controls.Add(buttonsPanel, 0, 3);
            Controls.Add(layout);

            AcceptButton = _selectButton;
            CancelButton = cancelButton;

            _searchTextBox.TextChanged += OnSearchTextChanged;
            _gamesListBox.SelectedIndexChanged += OnSelectionChanged;
            _gamesListBox.DoubleClick += OnSelectButtonClick;

            RefreshResults();
        }

        /// <summary>The chosen game. Only meaningful when the dialog was closed with OK.</summary>
        public Game SelectedGame { get; private set; }

        private void RefreshResults()
        {
            IReadOnlyList<Game> candidates = _catalog.FindBaseCandidates(_game, _searchTextBox.Text, ResultLimit + 1);

            _gamesListBox.BeginUpdate();
            _gamesListBox.Items.Clear();

            foreach (Game candidate in candidates.Take(ResultLimit))
            {
                _gamesListBox.Items.Add(candidate);
            }

            _gamesListBox.EndUpdate();

            _hintLabel.Text = candidates.Count > ResultLimit
                ? Localizer.Format("picker.limited", ResultLimit)
                : Localizer.Format("picker.count", candidates.Count);

            if (_gamesListBox.Items.Count > 0)
            {
                _gamesListBox.SelectedIndex = 0;
            }

            _selectButton.Enabled = _gamesListBox.SelectedItem != null;
        }

        private void OnSearchTextChanged(object sender, EventArgs e)
        {
            RefreshResults();
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            _selectButton.Enabled = _gamesListBox.SelectedItem != null;
        }

        private void OnSelectButtonClick(object sender, EventArgs e)
        {
            var game = _gamesListBox.SelectedItem as Game;

            if (game == null)
            {
                return;
            }

            SelectedGame = game;
            DialogResult = DialogResult.OK;
        }
    }
}
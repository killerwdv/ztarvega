using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Models;
using ZtarVega.Services;

namespace ZtarVega.UI
{
    /// <summary>Left panel: search box, filters and a virtual game list.</summary>
    internal sealed class GameListPanel : UserControl
    {
        private const int SearchDelayMilliseconds = 200;
        private const int FavoriteColumnWidth = 26;
        private const int YearColumnWidth = 60;
        private const int MinimumTitleColumnWidth = 100;
        private const int EmSetCueBanner = 0x1501;
        private const string FavoriteSymbol = "\u2605";

        private static readonly GameRegion?[] RegionChoices =
        {
            null,
            GameRegion.Usa,
            GameRegion.Japan,
            GameRegion.Europe,
            GameRegion.World,
            GameRegion.Other
        };

        private readonly TextBox _searchTextBox;
        private readonly ComboBox _filterComboBox;
        private readonly ComboBox _regionComboBox;
        private readonly ComboBox _genreComboBox;
        private readonly CheckBox _hideAlternatesCheckBox;
        private readonly ListView _gameListView;
        private readonly ColumnHeader _titleColumn;
        private readonly Label _countLabel;
        private readonly ToolTip _toolTip;
        private readonly Timer _searchTimer;

        private GameCatalog _catalog;
        private IReadOnlyList<Game> _visibleGames = new List<Game>();
        private Game _lastNotifiedGame;
        private bool _isRebuilding;
        private bool _isUpdatingFilters;
        private string _restoredGenre;

        public GameListPanel()
        {
            _searchTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 6, 6, 3)
            };

            _filterComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(6, 3, 6, 3)
            };
            _filterComboBox.Items.Add(new FilterOption(GameFilterMode.All, Localizer.Get("list.filter.all")));
            _filterComboBox.Items.Add(new FilterOption(GameFilterMode.AvailableOnly, Localizer.Get("list.filter.available")));
            _filterComboBox.Items.Add(new FilterOption(GameFilterMode.FavoritesOnly, Localizer.Get("list.filter.favorites")));
            _filterComboBox.Items.Add(new FilterOption(GameFilterMode.RecentlyPlayed, Localizer.Get("list.filter.recent")));
            _filterComboBox.SelectedIndex = 0;

            _regionComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 3, 0)
            };

            _regionComboBox.Items.Add(Localizer.Get("list.region.all"));
            foreach (GameRegion region in new[] { GameRegion.Usa, GameRegion.Japan, GameRegion.Europe, GameRegion.World, GameRegion.Other }) { _regionComboBox.Items.Add(Localizer.RegionName(region)); }

            _regionComboBox.SelectedIndex = 0;

            _genreComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(3, 0, 0, 0)
            };
            _genreComboBox.Items.Add(Localizer.Get("list.genre.all"));
            _genreComboBox.SelectedIndex = 0;

            var filtersRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(6, 3, 6, 3)
            };
            filtersRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            filtersRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            filtersRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            filtersRow.Controls.Add(_regionComboBox, 0, 0);
            filtersRow.Controls.Add(_genreComboBox, 1, 0);

            _hideAlternatesCheckBox = new CheckBox
            {
                Text = Localizer.Get("list.hideAlternates"),
                AutoSize = true,
                Margin = new Padding(9, 3, 6, 3)
            };

            _toolTip = new ToolTip();
            _toolTip.SetToolTip(_hideAlternatesCheckBox, Localizer.Get("list.hideAlternates.tip"));

            _gameListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                VirtualMode = true,
                FullRowSelect = true,
                MultiSelect = false,
                HideSelection = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Margin = new Padding(6, 3, 6, 3)
            };
            _gameListView.Columns.Add(FavoriteSymbol, FavoriteColumnWidth);
            _titleColumn = _gameListView.Columns.Add(Localizer.Get("list.column.title"), MinimumTitleColumnWidth);
            _gameListView.Columns.Add(Localizer.Get("list.column.year"), YearColumnWidth);

            _countLabel = new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(9, 3, 6, 6)
            };

            _searchTimer = new Timer { Interval = SearchDelayMilliseconds };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(_searchTextBox, 0, 0);
            layout.Controls.Add(_filterComboBox, 0, 1);
            layout.Controls.Add(filtersRow, 0, 2);
            layout.Controls.Add(_hideAlternatesCheckBox, 0, 3);
            layout.Controls.Add(_gameListView, 0, 4);
            layout.Controls.Add(_countLabel, 0, 5);
            Controls.Add(layout);

            _searchTextBox.HandleCreated += OnSearchTextBoxHandleCreated;
            _searchTextBox.TextChanged += OnSearchTextChanged;
            _searchTimer.Tick += OnSearchTimerTick;
            _filterComboBox.SelectedIndexChanged += OnFilterChanged;
            _regionComboBox.SelectedIndexChanged += OnFilterChanged;
            _genreComboBox.SelectedIndexChanged += OnFilterChanged;
            _hideAlternatesCheckBox.CheckedChanged += OnFilterChanged;
            _gameListView.RetrieveVirtualItem += OnRetrieveVirtualItem;
            _gameListView.SelectedIndexChanged += OnGameListSelectedIndexChanged;
            _gameListView.ItemActivate += OnGameListItemActivate;
            _gameListView.Resize += OnGameListResize;
            ResizeColumns();
        }

        /// <summary>Raised when the selected game changes. The game is null when nothing is selected.</summary>
        public event EventHandler<GameEventArgs> SelectedGameChanged;

        /// <summary>Raised when the user double-clicks a game or presses Enter on it.</summary>
        public event EventHandler<GameEventArgs> GameActivated;

        /// <summary>The currently selected game, or null when nothing is selected.</summary>
        public Game SelectedGame
        {
            get
            {
                if (_gameListView.SelectedIndices.Count == 0)
                {
                    return null;
                }

                int index = _gameListView.SelectedIndices[0];
                return index < _visibleGames.Count ? _visibleGames[index] : null;
            }
        }

        /// <summary>The filter currently chosen in the drop-down.</summary>
        public GameFilterMode FilterMode
        {
            get { return ((FilterOption)_filterComboBox.SelectedItem).Mode; }
        }

        /// <summary>Connects the panel to a catalog and shows its games with the given filters.</summary>
        public void Initialize(GameCatalog catalog, ListFilterState initialFilters)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            ApplyFilterState(initialFilters ?? ListFilterState.Default);
            PopulateGenres();
            ApplyFilter(false);
        }

        /// <summary>The filters currently chosen. The search text is not included.</summary>
        public ListFilterState GetFilterState()
        {
            return new ListFilterState(
                ((FilterOption)_filterComboBox.SelectedItem).Mode,
                RegionChoices[_regionComboBox.SelectedIndex],
                _genreComboBox.SelectedIndex > 0 ? (string)_genreComboBox.SelectedItem : null,
                _hideAlternatesCheckBox.Checked);
        }

        private void ApplyFilterState(ListFilterState state)
        {
            // Changing the controls raises events that must not rebuild the list: it is built once afterwards.
            _isUpdatingFilters = true;

            try
            {
                for (int index = 0; index < _filterComboBox.Items.Count; index++)
                {
                    if (((FilterOption)_filterComboBox.Items[index]).Mode == state.Mode)
                    {
                        _filterComboBox.SelectedIndex = index;
                        break;
                    }
                }

                int regionIndex = Array.IndexOf(RegionChoices, state.Region);
                _regionComboBox.SelectedIndex = regionIndex >= 0 ? regionIndex : 0;

                _hideAlternatesCheckBox.Checked = state.HideAlternateVersions;
            }
            finally
            {
                _isUpdatingFilters = false;
            }

            // The genre drop-down is filled afterwards, from the catalog: this is the genre to select then.
            _restoredGenre = state.Genre;
        }

        /// <summary>
        /// Rebuilds the list from the catalog keeping the selected game selected (or, when it is no
        /// longer listed, its neighbor). Call it after the catalog data has changed.
        /// </summary>
        public void RefreshGames()
        {
            PopulateGenres();
            ApplyFilter(true);
        }

        /// <summary>Rebuilds the list and selects the given game when it is listed (for example, a game that was just added).</summary>
        public void RefreshGames(int gameIdToSelect)
        {
            PopulateGenres();
            ApplyFilter(true, gameIdToSelect);
        }

        /// <summary>Repaints the visible rows without rebuilding the list. Use it when only the row contents changed.</summary>
        public void RedrawRows()
        {
            _gameListView.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _searchTimer.Dispose();
                _toolTip.Dispose();
            }

            base.Dispose(disposing);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr wParam, string lParam);

        /// <summary>Fills the genre drop-down with the genres of the catalog, keeping the chosen one when it still exists.</summary>
        private void PopulateGenres()
        {
            if (_catalog == null)
            {
                return;
            }

            // After restoring the filters, the genre saved last time is selected; otherwise the current one is kept.
            string selectedGenre = _restoredGenre ?? (_genreComboBox.SelectedIndex > 0 ? (string)_genreComboBox.SelectedItem : null);
            _restoredGenre = null;
            IReadOnlyList<string> genres = _catalog.GetGenres();

            // Changing the items raises selection events that must not rebuild the list.
            _isUpdatingFilters = true;

            try
            {
                _genreComboBox.BeginUpdate();
                _genreComboBox.Items.Clear();
                _genreComboBox.Items.Add(Localizer.Get("list.genre.all"));

                foreach (string genre in genres)
                {
                    _genreComboBox.Items.Add(genre);
                }

                int index = selectedGenre == null ? 0 : _genreComboBox.Items.IndexOf(selectedGenre);
                _genreComboBox.SelectedIndex = index >= 0 ? index : 0;
                _genreComboBox.EndUpdate();
            }
            finally
            {
                _isUpdatingFilters = false;
            }
        }

        private void ApplyFilter(bool keepSelection, int? gameIdToSelect = null)
        {
            if (_catalog == null)
            {
                return;
            }

            // Remember the selection while the OLD list is still in place.
            int? previousGameId = gameIdToSelect;
            int previousIndex = gameIdToSelect.HasValue ? 0 : -1;

            if (keepSelection && !gameIdToSelect.HasValue && _gameListView.SelectedIndices.Count > 0)
            {
                previousIndex = _gameListView.SelectedIndices[0];
                Game previousGame = SelectedGame;

                if (previousGame != null)
                {
                    previousGameId = previousGame.Id;
                }
            }

            var query = new GameQuery
            {
                SearchText = _searchTextBox.Text,
                Mode = ((FilterOption)_filterComboBox.SelectedItem).Mode,
                Region = RegionChoices[_regionComboBox.SelectedIndex],
                Genre = _genreComboBox.SelectedIndex > 0 ? (string)_genreComboBox.SelectedItem : null,
                HideAlternateVersions = _hideAlternatesCheckBox.Checked
            };

            IReadOnlyList<Game> newGames = _catalog.Search(query);
            int indexToSelect;

            // While the list is rebuilt, selection events are ignored: a single notification is sent at the end.
            _isRebuilding = true;
            _gameListView.BeginUpdate();

            try
            {
                // Clear the selection while the OLD list is still in place: the ListView
                // may request the previously selected row while the selection is cleared.
                _gameListView.SelectedIndices.Clear();

                // Scroll to the top while the old list is still valid, so the control has no
                // scroll position to restore when the size changes.
                if (_visibleGames.Count > 0)
                {
                    _gameListView.EnsureVisible(0);
                }

                // Shrinking to zero first avoids a WinForms bug when the list size changes while scrolled.
                _gameListView.VirtualListSize = 0;

                _visibleGames = newGames;
                _gameListView.VirtualListSize = _visibleGames.Count;

                indexToSelect = FindIndexToSelect(previousGameId, previousIndex);

                if (indexToSelect >= 0)
                {
                    _gameListView.SelectedIndices.Add(indexToSelect);
                }
            }
            finally
            {
                _gameListView.EndUpdate();
                _isRebuilding = false;
            }

            _countLabel.Text = Localizer.Format("list.count", _visibleGames.Count);

            // The list is already at the top, so only a selection further down needs scrolling.
            if (indexToSelect > 0)
            {
                _gameListView.EnsureVisible(indexToSelect);
            }

            NotifySelectionChanged();
        }

        private int FindIndexToSelect(int? previousGameId, int previousIndex)
        {
            if (_visibleGames.Count == 0)
            {
                return -1;
            }

            if (!previousGameId.HasValue)
            {
                return 0;
            }

            for (int index = 0; index < _visibleGames.Count; index++)
            {
                if (_visibleGames[index].Id == previousGameId.Value)
                {
                    return index;
                }
            }

            // The game left the list (for example, it was removed from the favorites): select its neighbor.
            return Math.Min(previousIndex, _visibleGames.Count - 1);
        }

        private void NotifySelectionChanged()
        {
            Game game = SelectedGame;

            // Nothing is raised when the same game stays selected, so the side panels do not reload their images.
            if (ReferenceEquals(game, _lastNotifiedGame))
            {
                return;
            }

            _lastNotifiedGame = game;
            SelectedGameChanged?.Invoke(this, new GameEventArgs(game));
        }

        private void ResizeColumns()
        {
            int availableWidth = _gameListView.ClientSize.Width - FavoriteColumnWidth - YearColumnWidth - SystemInformation.VerticalScrollBarWidth;
            _titleColumn.Width = Math.Max(availableWidth, MinimumTitleColumnWidth);
        }

        private void OnSearchTextBoxHandleCreated(object sender, EventArgs e)
        {
            // Shows grey placeholder text while the box is empty (Windows API call).
            SendMessage(_searchTextBox.Handle, EmSetCueBanner, (IntPtr)1, Localizer.Get("list.search.placeholder"));
        }

        private void OnSearchTextChanged(object sender, EventArgs e)
        {
            // Debounce: wait until the user pauses typing before filtering.
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void OnSearchTimerTick(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            ApplyFilter(false);
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            if (_isUpdatingFilters)
            {
                return;
            }

            ApplyFilter(false);
        }

        private void OnRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            // Called only for the rows currently visible on screen.
            if (e.ItemIndex < 0 || e.ItemIndex >= _visibleGames.Count)
            {
                // The list can briefly request a stale row while it is being updated.
                // A virtual ListView requires an item, so return an empty one.
                e.Item = new ListViewItem(string.Empty);
                return;
            }

            Game game = _visibleGames[e.ItemIndex];
            string favorite = game.IsFavorite ? FavoriteSymbol : string.Empty;
            string year = game.Year.HasValue ? game.Year.Value.ToString() : string.Empty;
            e.Item = new ListViewItem(new[] { favorite, game.DisplayTitle, year });
        }

        private void OnGameListSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isRebuilding)
            {
                return;
            }

            NotifySelectionChanged();
        }

        private void OnGameListItemActivate(object sender, EventArgs e)
        {
            Game game = SelectedGame;

            if (game != null)
            {
                GameActivated?.Invoke(this, new GameEventArgs(game));
            }
        }

        private void OnGameListResize(object sender, EventArgs e)
        {
            ResizeColumns();
        }

        /// <summary>An entry of the filter drop-down: a filter mode plus its display text.</summary>
        private sealed class FilterOption
        {
            private readonly string _displayName;

            public FilterOption(GameFilterMode mode, string displayName)
            {
                Mode = mode;
                _displayName = displayName;
            }

            public GameFilterMode Mode { get; }

            public override string ToString()
            {
                return _displayName;
            }
        }
    }
}
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Models;

namespace ZtarVega.UI
{
    /// <summary>Modal dialog to edit the emulator, ROMs folder and appearance settings.</summary>
    internal sealed class SettingsForm : ThemedForm
    {
        private readonly TextBox _emulatorPathTextBox;
        private readonly TextBox _argumentsTextBox;
        private readonly TextBox _romsFolderTextBox;
        private readonly ComboBox _themeComboBox;
        private readonly ComboBox _languageComboBox;

        public SettingsForm(AppSettings currentSettings)
        {
            if (currentSettings == null)
            {
                throw new ArgumentNullException(nameof(currentSettings));
            }

            // The dialog edits a copy, so cancelling leaves the real settings untouched.
            Settings = currentSettings.Clone();

            Text = Localizer.Get("settings.title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(580, 340);

            _emulatorPathTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = Settings.EmulatorPath };
            _argumentsTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = Settings.EmulatorArguments };
            _romsFolderTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = Settings.RomsFolder };

            _themeComboBox = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _themeComboBox.Items.Add(new ThemeOption(Localizer.Get("settings.theme.dark"), AppSettings.DarkTheme));
            _themeComboBox.Items.Add(new ThemeOption(Localizer.Get("settings.theme.light"), AppSettings.LightTheme));
            _themeComboBox.SelectedIndex =
                string.Equals(Settings.ThemeName, AppSettings.LightTheme, StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            _languageComboBox = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            foreach (LanguageInfo language in Localizer.DiscoverLanguages())
            {
                _languageComboBox.Items.Add(language);
            }

            SelectLanguage(Localizer.CurrentCode);

            Button emulatorBrowseButton = CreateBrowseButton();
            emulatorBrowseButton.Click += OnBrowseEmulatorClick;

            Button romsFolderBrowseButton = CreateBrowseButton();
            romsFolderBrowseButton.Click += OnBrowseRomsFolderClick;

            var argumentsHintLabel = CreateHintLabel(Localizer.Format("settings.argumentsHint", AppSettings.RomPlaceholder));
            var restartHintLabel = CreateHintLabel(Localizer.Get("settings.restartHint"));

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
                ColumnCount = 3,
                RowCount = 8,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            for (int row = 0; row < 7; row++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            AddRow(layout, 0, Localizer.Get("settings.emulator"), _emulatorPathTextBox, emulatorBrowseButton);
            AddRow(layout, 1, Localizer.Get("settings.arguments"), _argumentsTextBox, null);
            AddHint(layout, 2, argumentsHintLabel);
            AddRow(layout, 3, Localizer.Get("settings.romsFolder"), _romsFolderTextBox, romsFolderBrowseButton);
            AddRow(layout, 4, Localizer.Get("settings.theme"), _themeComboBox, null);
            AddRow(layout, 5, Localizer.Get("settings.language"), _languageComboBox, null);
            AddHint(layout, 6, restartHintLabel);

            layout.Controls.Add(buttonsPanel, 0, 7);
            layout.SetColumnSpan(buttonsPanel, 3);

            Controls.Add(layout);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        /// <summary>The edited settings. Only meaningful when the dialog was closed with OK.</summary>
        public AppSettings Settings { get; }

        private static Button CreateBrowseButton()
        {
            return new Button { Text = Localizer.Get("common.browse"), AutoSize = true, Anchor = AnchorStyles.Left };
        }

        private static Label CreateHintLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(440, 0),
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(3, 0, 3, 8)
            };
        }

        private static void AddRow(TableLayoutPanel layout, int row, string caption, Control field, Button browseButton)
        {
            var captionLabel = new Label
            {
                Text = caption,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 6, 12, 6)
            };

            layout.Controls.Add(captionLabel, 0, row);
            layout.Controls.Add(field, 1, row);

            if (browseButton != null)
            {
                layout.Controls.Add(browseButton, 2, row);
            }
            else
            {
                // Without a button, the field uses the space of the third column too.
                layout.SetColumnSpan(field, 2);
            }
        }

        private static void AddHint(TableLayoutPanel layout, int row, Label hint)
        {
            layout.Controls.Add(hint, 1, row);
            layout.SetColumnSpan(hint, 2);
        }

        private void SelectLanguage(string code)
        {
            for (int index = 0; index < _languageComboBox.Items.Count; index++)
            {
                var language = (LanguageInfo)_languageComboBox.Items[index];

                if (string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    _languageComboBox.SelectedIndex = index;
                    return;
                }
            }

            _languageComboBox.SelectedIndex = 0;
        }

        private void OnBrowseEmulatorClick(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog
            {
                Title = Localizer.Get("settings.browseEmulator.title"),
                Filter = Localizer.Get("settings.browseEmulator.filter"),
                CheckFileExists = true
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _emulatorPathTextBox.Text = dialog.FileName;
                }
            }
        }

        private void OnBrowseRomsFolderClick(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { Description = Localizer.Get("settings.browseRomsFolder.description") })
            {
                if (Directory.Exists(_romsFolderTextBox.Text))
                {
                    dialog.SelectedPath = _romsFolderTextBox.Text;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _romsFolderTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnOkButtonClick(object sender, EventArgs e)
        {
            string emulatorPath = _emulatorPathTextBox.Text.Trim();
            string arguments = _argumentsTextBox.Text.Trim();
            string romsFolder = _romsFolderTextBox.Text.Trim();

            // Empty values are allowed (not configured yet); filled values must be valid.
            if (emulatorPath.Length > 0 && !File.Exists(emulatorPath))
            {
                ShowValidationError(Localizer.Get("settings.error.emulatorMissing"), _emulatorPathTextBox);
                return;
            }

            if (arguments.Length > 0 && arguments.IndexOf(AppSettings.RomPlaceholder, StringComparison.Ordinal) < 0)
            {
                ShowValidationError(Localizer.Format("settings.error.argumentsPlaceholder", AppSettings.RomPlaceholder), _argumentsTextBox);
                return;
            }

            if (romsFolder.Length > 0 && !Directory.Exists(romsFolder))
            {
                ShowValidationError(Localizer.Get("settings.error.romsFolderMissing"), _romsFolderTextBox);
                return;
            }

            Settings.EmulatorPath = emulatorPath;
            Settings.EmulatorArguments = arguments.Length == 0 ? AppSettings.DefaultEmulatorArguments : arguments;
            Settings.RomsFolder = romsFolder;
            Settings.ThemeName = ((ThemeOption)_themeComboBox.SelectedItem).Name;
            Settings.LanguageCode = ((LanguageInfo)_languageComboBox.SelectedItem).Code;

            DialogResult = DialogResult.OK;
        }

        private void ShowValidationError(string message, Control controlToFocus)
        {
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            controlToFocus.Focus();
        }

        /// <summary>An entry of the theme drop-down: the text shown and the name stored in the settings.</summary>
        private sealed class ThemeOption
        {
            private readonly string _displayName;

            public ThemeOption(string displayName, string name)
            {
                _displayName = displayName;
                Name = name;
            }

            public string Name { get; }

            public override string ToString()
            {
                return _displayName;
            }
        }
    }
}
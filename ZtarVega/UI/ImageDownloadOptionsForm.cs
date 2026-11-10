using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Models;
using ZtarVega.Services;

namespace ZtarVega.UI
{
    /// <summary>Modal dialog where the user chooses which images to download, from which web address and for which games.</summary>
    internal sealed class ImageDownloadOptionsForm : ThemedForm
    {
        private const int TextMaximumWidth = 530;
        private const string ExampleAddress = "https://example.com/images/{filetitle}.png";

        private readonly AppSettings _settings;
        private readonly ImageDownloader _downloader;
        private readonly Game _testGame;
        private readonly CancellationTokenSource _testCancellation = new CancellationTokenSource();

        private readonly ComboBox _targetComboBox;
        private readonly TextBox _templateTextBox;
        private readonly RadioButton _availableRadioButton;
        private readonly RadioButton _allRadioButton;
        private readonly RadioButton _selectedRadioButton;
        private readonly RadioButton _replaceRadioButton;
        private readonly Button _testButton;
        private readonly TextBox _testResultTextBox;

        /// <param name="settings">The settings, which remember the address of each kind of image.</param>
        /// <param name="selectedGame">The game selected in the list, or null.</param>
        /// <param name="testGame">The game used by the test button, or null when there is none.</param>
        /// <param name="downloader">Used by the test button.</param>
        public ImageDownloadOptionsForm(AppSettings settings, Game selectedGame, Game testGame, ImageDownloader downloader)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
            _testGame = testGame;

            Text = Localizer.Get("download.title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(600, 660);

            var introLabel = CreateTextLabel(Localizer.Get("download.intro"), false);

            _targetComboBox = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            foreach (ImageTarget target in ImageTarget.All)
            {
                _targetComboBox.Items.Add(target);
            }

            _templateTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };

            var addressHintLabel = CreateTextLabel(
                Localizer.Format(
                    "download.addressHint",
                    ImageUrlTemplate.TitlePlaceholder,
                    ImageUrlTemplate.FileTitlePlaceholder,
                    ImageUrlTemplate.Crc32Placeholder,
                    ExampleAddress),
                true);

            _availableRadioButton = new RadioButton { Text = Localizer.Get("download.scope.available"), AutoSize = true, Checked = true };
            _allRadioButton = new RadioButton { Text = Localizer.Get("download.scope.all"), AutoSize = true };
            _selectedRadioButton = new RadioButton
            {
                Text = Localizer.Get("download.scope.selected"),
                AutoSize = true,
                Enabled = selectedGame != null
            };

            var fillEmptyRadioButton = new RadioButton { Text = Localizer.Get("download.mode.fillEmpty"), AutoSize = true, Checked = true };
            _replaceRadioButton = new RadioButton { Text = Localizer.Get("download.mode.replace"), AutoSize = true };

            _testButton = new Button { Text = Localizer.Get("download.test"), AutoSize = true, Margin = new Padding(3, 12, 3, 3) };
            _testButton.Click += OnTestButtonClick;

            _testResultTextBox = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Height = 70
            };

            var startButton = new Button { Text = Localizer.Get("download.start"), AutoSize = true, MinimumSize = new Size(90, 0) };
            startButton.Click += OnStartButtonClick;

            var cancelButton = new Button
            {
                Text = Localizer.Get("common.cancel"),
                AutoSize = true,
                MinimumSize = new Size(90, 0),
                DialogResult = DialogResult.Cancel
            };

            var buttonsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Anchor = AnchorStyles.Right
            };
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(startButton);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            AddSpanningRow(layout, introLabel);
            AddRow(layout, Localizer.Get("download.imageType"), _targetComboBox);
            AddRow(layout, Localizer.Get("download.address"), _templateTextBox);
            AddSpanningRow(layout, addressHintLabel);

            // Each group of radio buttons needs its own container, otherwise all of them would be one group.
            AddSpanningRow(layout, CreateRadioGroup(_availableRadioButton, _allRadioButton, _selectedRadioButton));
            AddSpanningRow(layout, CreateRadioGroup(fillEmptyRadioButton, _replaceRadioButton));
            AddSpanningRow(layout, _testButton);
            AddSpanningRow(layout, _testResultTextBox);

            // The last row takes the remaining space and holds the buttons.
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowCount = layout.RowStyles.Count;
            layout.Controls.Add(buttonsPanel, 0, layout.RowCount - 1);
            layout.SetColumnSpan(buttonsPanel, 2);

            Controls.Add(layout);

            AcceptButton = startButton;
            CancelButton = cancelButton;

            _targetComboBox.SelectedIndexChanged += OnTargetChanged;
            _targetComboBox.SelectedIndex = 0;
        }

        /// <summary>The chosen kind of image. Only meaningful when the dialog was closed with OK.</summary>
        public ImageTarget Target { get; private set; }

        /// <summary>The address template the user wrote.</summary>
        public string UrlTemplate { get; private set; }

        public ImageDownloadScope Scope { get; private set; }

        /// <summary>True when the images the games already have must be overwritten.</summary>
        public bool ReplaceExisting { get; private set; }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // A test that is still running is aborted.
            _testCancellation.Cancel();
            base.OnFormClosing(e);
        }

        private static Label CreateTextLabel(string text, bool isSecondary)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(TextMaximumWidth, 0),
                ForeColor = isSecondary ? SystemColors.GrayText : SystemColors.ControlText,
                Margin = new Padding(3, 6, 3, 6)
            };
        }

        private static FlowLayoutPanel CreateRadioGroup(params RadioButton[] buttons)
        {
            var panel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0, 6, 0, 6)
            };

            panel.Controls.AddRange(buttons);
            return panel;
        }

        private static void AddRow(TableLayoutPanel layout, string caption, Control field)
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
            layout.Controls.Add(field, 1, row);
        }

        private static void AddSpanningRow(TableLayoutPanel layout, Control control)
        {
            int row = layout.RowStyles.Count;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowCount = layout.RowStyles.Count;

            layout.Controls.Add(control, 0, row);
            layout.SetColumnSpan(control, 2);
        }

        private void OnTargetChanged(object sender, EventArgs e)
        {
            string saved = _settings.GetImageSource(((ImageTarget)_targetComboBox.SelectedItem).Key);

            // A kind of image without a saved address keeps the text that is already written, so usually
            // only the part that changes (for example, the folder name) has to be edited.
            if (saved.Length > 0)
            {
                _templateTextBox.Text = saved;
            }

            _testResultTextBox.Clear();
        }

        private async void OnTestButtonClick(object sender, EventArgs e)
        {
            if (!IsTemplateValid())
            {
                return;
            }

            if (_testGame == null)
            {
                _testResultTextBox.Text = Localizer.Get("download.test.noGame");
                return;
            }

            _testButton.Enabled = false;
            _testResultTextBox.Text = Localizer.Get("download.test.testing");

            ImageFetchResult result = await _downloader.TestAsync(_templateTextBox.Text, _testGame, _testCancellation.Token);

            // The dialog may have been closed while the request was in flight.
            if (IsDisposed)
            {
                return;
            }

            _testResultTextBox.Text = DescribeTestResult(result);
            _testButton.Enabled = true;
        }

        private void OnStartButtonClick(object sender, EventArgs e)
        {
            if (!IsTemplateValid())
            {
                return;
            }

            Target = (ImageTarget)_targetComboBox.SelectedItem;
            UrlTemplate = _templateTextBox.Text.Trim();
            ReplaceExisting = _replaceRadioButton.Checked;

            if (_allRadioButton.Checked)
            {
                Scope = ImageDownloadScope.AllGames;
            }
            else if (_selectedRadioButton.Checked)
            {
                Scope = ImageDownloadScope.SelectedGame;
            }
            else
            {
                Scope = ImageDownloadScope.AvailableGames;
            }

            DialogResult = DialogResult.OK;
        }

        private bool IsTemplateValid()
        {
            TemplateProblem problem = ImageUrlTemplate.Validate(_templateTextBox.Text);

            if (problem == TemplateProblem.None)
            {
                return true;
            }

            string message;

            switch (problem)
            {
                case TemplateProblem.Empty:
                    message = Localizer.Get("download.error.templateEmpty");
                    break;

                case TemplateProblem.UnsupportedScheme:
                    message = Localizer.Get("download.error.templateScheme");
                    break;

                default:
                    message = Localizer.Format(
                        "download.error.templatePlaceholder",
                        ImageUrlTemplate.TitlePlaceholder,
                        ImageUrlTemplate.FileTitlePlaceholder,
                        ImageUrlTemplate.Crc32Placeholder);
                    break;
            }

            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _templateTextBox.Focus();
            return false;
        }

        private static string DescribeTestResult(ImageFetchResult result)
        {
            string message;

            switch (result.Status)
            {
                case ImageFetchStatus.Success:
                    message = Localizer.Format("download.test.ok", Math.Max(1, result.Content.Length / 1024));
                    break;

                case ImageFetchStatus.NotFound:
                    message = Localizer.Get("download.test.notFound");
                    break;

                case ImageFetchStatus.NotAnImage:
                    message = Localizer.Get("download.test.notAnImage");
                    break;

                case ImageFetchStatus.TooLarge:
                    message = Localizer.Get("download.test.tooLarge");
                    break;

                case ImageFetchStatus.InvalidUrl:
                    message = Localizer.Get("download.test.invalidUrl");
                    break;

                case ImageFetchStatus.NoChecksum:
                    message = Localizer.Get("download.test.noChecksum");
                    break;

                case ImageFetchStatus.NetworkError:
                    message = Localizer.Format("download.test.networkError", result.Detail);
                    break;

                default:
                    message = string.Empty;
                    break;
            }

            return string.IsNullOrEmpty(result.Url)
                ? message
                : message + "\r\n" + Localizer.Get("download.test.address") + "\r\n" + result.Url;
        }
    }
}
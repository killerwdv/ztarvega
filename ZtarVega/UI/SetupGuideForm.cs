using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Services;
using ZtarVega.UI.Actions;

namespace ZtarVega.UI
{
    /// <summary>
    /// The "Getting started" window: a checklist with the steps to set ZtarVega up, in the right order. It is not
    /// modal: it stays next to the main window while the user does each step.
    /// </summary>
    internal sealed class SetupGuideForm : ThemedForm
    {
        private const string DoneSymbol = "\u2714";
        private const string PendingSymbol = "\u25CB";
        private const string NextSymbol = "\u25B6";
        private const int TextWidth = 460;

        private static readonly Color DoneColor = Color.FromArgb(76, 175, 80);
        private static readonly Color NextColor = Color.FromArgb(0, 120, 215);

        private readonly GuideActions _guide;
        private readonly Font _symbolFont;
        private readonly Font _titleFont;
        private readonly List<StepRow> _rows = new List<StepRow>();
        private readonly Label _readyLabel;
        private readonly CheckBox _showAtStartupCheckBox;
        private bool _isRunningStep;

        public SetupGuideForm(GuideActions guide)
        {
            _guide = guide ?? throw new ArgumentNullException(nameof(guide));

            Text = Localizer.Get("guide.title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 680);

            _symbolFont = new Font("Segoe UI Symbol", 14F);
            _titleFont = new Font(Font, FontStyle.Bold);

            var stepsLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                Location = Point.Empty,
                Padding = new Padding(12)
            };
            stepsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            stepsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            stepsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var introLabel = new Label
            {
                Text = Localizer.Get("guide.intro"),
                AutoSize = true,
                MaximumSize = new Size(TextWidth + 220, 0),
                Margin = new Padding(3, 0, 3, 12)
            };

            stepsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stepsLayout.RowCount = 1;
            stepsLayout.Controls.Add(introLabel, 0, 0);
            stepsLayout.SetColumnSpan(introLabel, 3);

            AddStep(stepsLayout, SetupStep.Settings, "settings",
                new StepAction("guide.step.settings.button", () => _guide.RunStepAsync(SetupStep.Settings)));
            AddStep(stepsLayout, SetupStep.Catalog, "catalog",
                new StepAction("guide.step.catalog.button", () => _guide.RunStepAsync(SetupStep.Catalog)));
            AddStep(stepsLayout, SetupStep.Scan, "scan",
                new StepAction("guide.step.scan.button", () => _guide.RunStepAsync(SetupStep.Scan)));
            AddStep(stepsLayout, SetupStep.Metadata, "metadata",
                new StepAction("guide.step.metadata.button", () => _guide.RunStepAsync(SetupStep.Metadata)));
            AddStep(stepsLayout, SetupStep.Images, "images",
                new StepAction("guide.step.images.importButton", () => _guide.ImportImagesFromFolderAsync()),
                new StepAction("guide.step.images.downloadButton", () => _guide.DownloadImagesAsync()));

            // The steps scroll when the window is too small for them.
            var scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            scrollPanel.Controls.Add(stepsLayout);

            _readyLabel = new Label
            {
                Text = Localizer.Get("guide.ready"),
                AutoSize = true,
                MaximumSize = new Size(TextWidth + 220, 0),
                Visible = false,
                Margin = new Padding(12, 8, 12, 4)
            };

            _showAtStartupCheckBox = new CheckBox
            {
                Text = Localizer.Get("guide.showAtStartup"),
                AutoSize = true,
                Checked = _guide.ShowAtStartup,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(12, 6, 3, 6)
            };
            _showAtStartupCheckBox.CheckedChanged += (sender, e) => _guide.ShowAtStartup = _showAtStartupCheckBox.Checked;

            var closeButton = new Button
            {
                Text = Localizer.Get("guide.close"),
                AutoSize = true,
                MinimumSize = new Size(90, 0),
                Anchor = AnchorStyles.Right,
                Margin = new Padding(3, 6, 12, 6)
            };
            closeButton.Click += (sender, e) => Close();

            var bottomLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2
            };
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottomLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bottomLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bottomLayout.Controls.Add(_readyLabel, 0, 0);
            bottomLayout.SetColumnSpan(_readyLabel, 2);
            bottomLayout.Controls.Add(_showAtStartupCheckBox, 0, 1);
            bottomLayout.Controls.Add(closeButton, 1, 1);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.Controls.Add(scrollPanel, 0, 0);
            mainLayout.Controls.Add(bottomLayout, 0, 1);
            Controls.Add(mainLayout);

            CancelButton = closeButton;
        }

        /// <summary>Reads the state of the steps again from the real data and updates the window.</summary>
        public void RefreshState()
        {
            SetupGuideStatus status = _guide.GetStatus();
            SetupStep? nextStep = status.NextRequiredStep;

            foreach (StepRow row in _rows)
            {
                bool isDone = status.IsDone(row.Step);
                bool isEnabled = status.IsEnabled(row.Step);
                bool isNext = nextStep.HasValue && nextStep.Value == row.Step;

                row.SymbolLabel.Text = isDone ? DoneSymbol : isNext ? NextSymbol : PendingSymbol;
                row.SymbolLabel.ForeColor = isDone ? DoneColor : isNext ? NextColor : ThemeManager.Current.Muted;

                SetupStep? missing = status.FirstMissingPrerequisite(row.Step);
                row.RequirementLabel.Visible = missing.HasValue;
                row.RequirementLabel.Text = missing.HasValue
                    ? Localizer.Format("guide.requires", (int)missing.Value)
                    : string.Empty;

                for (int index = 0; index < row.Buttons.Count; index++)
                {
                    Button button = row.Buttons[index];
                    button.Enabled = isEnabled;

                    // The main button of the step to do next is highlighted.
                    StyleButton(button, isNext && index == 0);
                }
            }

            _readyLabel.Visible = status.RequiredStepsDone;
        }

        protected override void OnLoad(EventArgs e)
        {
            // The theme is applied first (see ThemedForm), and then the colors of the state on top of it.
            base.OnLoad(e);
            RefreshState();
        }

        protected override void OnActivated(EventArgs e)
        {
            // Something may have been done from the menus of the main window in the meantime.
            base.OnActivated(e);
            RefreshState();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _symbolFont.Dispose();
                _titleFont.Dispose();
            }

            base.Dispose(disposing);
        }

        private static void StyleButton(Button button, bool isHighlighted)
        {
            if (isHighlighted)
            {
                button.Tag = ThemeManager.AccentButtonTag;
                ThemeManager.StyleAccentButton(button);
            }
            else
            {
                button.Tag = null;
                ThemeManager.Apply(button);
            }
        }

        private void AddStep(TableLayoutPanel layout, SetupStep step, string key, params StepAction[] actions)
        {
            int rowIndex = layout.RowStyles.Count;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowCount = layout.RowStyles.Count;

            var symbolLabel = new Label
            {
                AutoSize = true,
                Font = _symbolFont,
                Anchor = AnchorStyles.Top,
                Margin = new Padding(3, 6, 10, 3)
            };

            var titleLabel = new Label
            {
                Text = Localizer.Get("guide.step." + key + ".title"),
                AutoSize = true,
                Font = _titleFont,
                Margin = new Padding(3, 8, 3, 2)
            };

            var textLabel = new Label
            {
                Text = Localizer.Get("guide.step." + key + ".text"),
                AutoSize = true,
                MaximumSize = new Size(TextWidth, 0),
                Margin = new Padding(3, 0, 3, 2)
            };

            var requirementLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(TextWidth, 0),
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(3, 0, 3, 6),
                Visible = false
            };

            var contentPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0, 0, 12, 14)
            };
            contentPanel.Controls.Add(titleLabel);
            contentPanel.Controls.Add(textLabel);
            contentPanel.Controls.Add(requirementLabel);

            var buttonsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0, 6, 0, 14)
            };

            var buttons = new List<Button>();

            foreach (StepAction action in actions)
            {
                StepAction currentAction = action;

                var button = new Button
                {
                    Text = Localizer.Get(currentAction.TextKey),
                    AutoSize = true,
                    MinimumSize = new Size(210, 0)
                };
                button.Click += async (sender, e) => await RunAsync(currentAction.Run);

                buttons.Add(button);
                buttonsPanel.Controls.Add(button);
            }

            layout.Controls.Add(symbolLabel, 0, rowIndex);
            layout.Controls.Add(contentPanel, 1, rowIndex);
            layout.Controls.Add(buttonsPanel, 2, rowIndex);

            _rows.Add(new StepRow(step, symbolLabel, requirementLabel, buttons));
        }

        private async Task RunAsync(Func<Task> work)
        {
            if (_isRunningStep)
            {
                return;
            }

            _isRunningStep = true;

            try
            {
                await work();
            }
            finally
            {
                _isRunningStep = false;
            }

            // The state is read again from the real data, so what the window shows is always right.
            if (!IsDisposed)
            {
                RefreshState();
            }
        }

        /// <summary>What a button of a step does and the key of its text.</summary>
        private sealed class StepAction
        {
            public StepAction(string textKey, Func<Task> run)
            {
                TextKey = textKey;
                Run = run;
            }

            public string TextKey { get; }

            public Func<Task> Run { get; }
        }

        /// <summary>The controls of one step, which are updated when the state changes.</summary>
        private sealed class StepRow
        {
            public StepRow(SetupStep step, Label symbolLabel, Label requirementLabel, IReadOnlyList<Button> buttons)
            {
                Step = step;
                SymbolLabel = symbolLabel;
                RequirementLabel = requirementLabel;
                Buttons = buttons;
            }

            public SetupStep Step { get; }

            public Label SymbolLabel { get; }

            public Label RequirementLabel { get; }

            public IReadOnlyList<Button> Buttons { get; }
        }
    }
}
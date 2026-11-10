using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Services;

namespace ZtarVega.UI
{
    /// <summary>Modal dialog where the user picks the folder of images, the kind of image and what to do with existing images.</summary>
    internal sealed class ImageFolderImportOptionsForm : ThemedForm
    {
        private const int HintMaximumWidth = 500;

        private readonly TextBox _folderTextBox;
        private readonly ComboBox _targetComboBox;
        private readonly RadioButton _replaceRadioButton;
        private readonly CheckBox _includeModifiedCheckBox;

        public ImageFolderImportOptionsForm(string initialFolder)
        {
            Text = Localizer.Get("imageFolder.title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 500);

            _folderTextBox = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Text = initialFolder ?? string.Empty
            };

            var browseButton = new Button { Text = Localizer.Get("common.browse"), AutoSize = true };
            browseButton.Click += OnBrowseButtonClick;

            _targetComboBox = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            foreach (ImageTarget target in ImageTarget.All)
            {
                _targetComboBox.Items.Add(target);
            }

            _targetComboBox.SelectedIndex = 0;

            var introLabel = CreateHintLabel(Localizer.Format(
                "imageFolder.intro",
                Localizer.Format("images.slot.screenshot", 1),
                Localizer.Format("images.slot.screenshot", 2)));

            var fillEmptyRadioButton = new RadioButton
            {
                Text = Localizer.Get("imageFolder.fillEmpty"),
                AutoSize = true,
                Checked = true
            };

            var fillHintLabel = CreateHintLabel(Localizer.Get("imageFolder.fillEmptyHint"));

            _replaceRadioButton = new RadioButton
            {
                Text = Localizer.Get("imageFolder.replace"),
                AutoSize = true
            };

            var replaceHintLabel = CreateHintLabel(Localizer.Get("imageFolder.replaceHint"));

            _includeModifiedCheckBox = new CheckBox
            {
                Text = Localizer.Get("imageFolder.includeModified"),
                AutoSize = true,
                Checked = true,
                Margin = new Padding(3, 12, 3, 3)
            };

            var includeModifiedHintLabel = CreateHintLabel(Localizer.Get("imageFolder.includeModifiedHint"));

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
                Anchor = AnchorStyles.Right
            };
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(okButton);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            AddRow(layout, new Label { Text = Localizer.Get("imageFolder.folder"), AutoSize = true, Anchor = AnchorStyles.Left }, _folderTextBox, browseButton);
            AddRow(layout, new Label { Text = Localizer.Get("imageFolder.imageType"), AutoSize = true, Anchor = AnchorStyles.Left }, _targetComboBox, null);
            AddSpanningRow(layout, introLabel);
            AddSpanningRow(layout, fillEmptyRadioButton);
            AddSpanningRow(layout, fillHintLabel);
            AddSpanningRow(layout, _replaceRadioButton);
            AddSpanningRow(layout, replaceHintLabel);
            AddSpanningRow(layout, _includeModifiedCheckBox);
            AddSpanningRow(layout, includeModifiedHintLabel);

            // The last row takes the remaining space and holds the buttons.
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowCount = layout.RowStyles.Count;
            layout.Controls.Add(buttonsPanel, 0, layout.RowCount - 1);
            layout.SetColumnSpan(buttonsPanel, 3);

            Controls.Add(layout);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        /// <summary>The chosen folder. Only meaningful when the dialog was closed with OK.</summary>
        public string FolderPath { get; private set; }

        /// <summary>The chosen kind of image.</summary>
        public ImageTarget Target { get; private set; }

        /// <summary>True when the existing images must be overwritten.</summary>
        public bool ReplaceExisting { get; private set; }

        /// <summary>True when the images of hacks and translations must be imported too.</summary>
        public bool IncludeModifiedVersions { get; private set; }

        private static Label CreateHintLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(HintMaximumWidth, 0),
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(3, 6, 3, 6)
            };
        }

        private static void AddRow(TableLayoutPanel layout, Control caption, Control field, Control button)
        {
            int row = layout.RowStyles.Count;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowCount = layout.RowStyles.Count;

            layout.Controls.Add(caption, 0, row);
            layout.Controls.Add(field, 1, row);

            if (button != null)
            {
                layout.Controls.Add(button, 2, row);
            }
            else
            {
                layout.SetColumnSpan(field, 2);
            }
        }

        private static void AddSpanningRow(TableLayoutPanel layout, Control control)
        {
            int row = layout.RowStyles.Count;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowCount = layout.RowStyles.Count;

            layout.Controls.Add(control, 0, row);
            layout.SetColumnSpan(control, 3);
        }

        private void OnBrowseButtonClick(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { Description = Localizer.Get("imageFolder.browseDescription") })
            {
                if (Directory.Exists(_folderTextBox.Text))
                {
                    dialog.SelectedPath = _folderTextBox.Text;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _folderTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnOkButtonClick(object sender, EventArgs e)
        {
            string folder = _folderTextBox.Text.Trim();

            if (!Directory.Exists(folder))
            {
                MessageBox.Show(this, Localizer.Get("imageFolder.error.folderMissing"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _folderTextBox.Focus();
                return;
            }

            FolderPath = folder;
            Target = (ImageTarget)_targetComboBox.SelectedItem;
            ReplaceExisting = _replaceRadioButton.Checked;
            IncludeModifiedVersions = _includeModifiedCheckBox.Checked;
            DialogResult = DialogResult.OK;
        }
    }
}
using System.Drawing;
using System.Windows.Forms;
using ZtarVega.Localization;

namespace ZtarVega.UI
{
    /// <summary>Modal dialog that asks how the imported metadata must be applied to the games that already have data.</summary>
    internal sealed class MetadataImportOptionsForm : ThemedForm
    {
        private const int HintMaximumWidth = 420;

        private readonly RadioButton _fillEmptyRadioButton;
        private readonly RadioButton _replaceRadioButton;

        public MetadataImportOptionsForm(int fileCount)
        {
            Text = Localizer.Get("metadataOptions.title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(480, 290);

            var introLabel = new Label
            {
                Text = Localizer.Format("metadataOptions.intro", fileCount),
                AutoSize = true,
                MaximumSize = new Size(HintMaximumWidth + 20, 0),
                Margin = new Padding(0, 0, 0, 12)
            };

            _fillEmptyRadioButton = new RadioButton
            {
                Text = Localizer.Get("metadataOptions.fillEmpty"),
                AutoSize = true,
                Checked = true
            };

            var fillHintLabel = CreateHintLabel(Localizer.Get("metadataOptions.fillEmptyHint"));

            _replaceRadioButton = new RadioButton
            {
                Text = Localizer.Get("metadataOptions.replace"),
                AutoSize = true
            };

            var replaceHintLabel = CreateHintLabel(Localizer.Get("metadataOptions.replaceHint"));

            var okButton = new Button
            {
                Text = Localizer.Get("common.ok"),
                AutoSize = true,
                MinimumSize = new Size(80, 0),
                DialogResult = DialogResult.OK
            };

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
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            for (int row = 0; row < 5; row++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            layout.Controls.Add(introLabel, 0, 0);
            layout.Controls.Add(_fillEmptyRadioButton, 0, 1);
            layout.Controls.Add(fillHintLabel, 0, 2);
            layout.Controls.Add(_replaceRadioButton, 0, 3);
            layout.Controls.Add(replaceHintLabel, 0, 4);
            layout.Controls.Add(buttonsPanel, 0, 5);
            Controls.Add(layout);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        /// <summary>True when the values of the files must overwrite the existing ones. Only meaningful when the dialog was closed with OK.</summary>
        public bool ReplaceExisting
        {
            get { return _replaceRadioButton.Checked; }
        }

        private static Label CreateHintLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(HintMaximumWidth, 0),
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(20, 0, 0, 12)
            };
        }
    }
}
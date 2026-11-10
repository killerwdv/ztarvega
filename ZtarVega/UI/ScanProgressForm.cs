using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Services;

namespace ZtarVega.UI
{
    /// <summary>Modal dialog that shows the progress of a long operation on files and lets the user cancel it.</summary>
    internal sealed class ScanProgressForm : ThemedForm
    {
        private readonly CancellationTokenSource _cancellation;
        private readonly string _progressFormatKey;
        private readonly Label _statusLabel;
        private readonly ProgressBar _progressBar;
        private readonly Button _cancelButton;
        private bool _canClose;

        /// <param name="cancellation">Cancelled when the user presses Cancel.</param>
        /// <param name="titleKey">The text key of the window title.</param>
        /// <param name="progressFormatKey">The text key of the progress message, which receives {0} = current file number, {1} = total and {2} = file name.</param>
        public ScanProgressForm(CancellationTokenSource cancellation, string titleKey, string progressFormatKey)
        {
            _cancellation = cancellation ?? throw new ArgumentNullException(nameof(cancellation));
            _progressFormatKey = progressFormatKey ?? throw new ArgumentNullException(nameof(progressFormatKey));

            Text = Localizer.Get(titleKey);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 120);

            _statusLabel = new Label
            {
                Text = Localizer.Get("progress.preparing"),
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Margin = new Padding(0, 0, 0, 6)
            };

            // Until the first report arrives the total is unknown, so the bar just animates.
            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Style = ProgressBarStyle.Marquee,
                Margin = new Padding(0, 0, 0, 6)
            };

            _cancelButton = new Button
            {
                Text = Localizer.Get("common.cancel"),
                AutoSize = true,
                MinimumSize = new Size(80, 0),
                Anchor = AnchorStyles.Right
            };
            _cancelButton.Click += OnCancelButtonClick;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(_statusLabel, 0, 0);
            layout.Controls.Add(_progressBar, 0, 1);
            layout.Controls.Add(_cancelButton, 0, 2);
            Controls.Add(layout);

            CancelButton = _cancelButton;
        }

        /// <summary>Updates the dialog. Must be called from the UI thread.</summary>
        public void Report(ScanProgress progress)
        {
            // A report can arrive after the dialog was closed; ignore it.
            if (IsDisposed || progress == null || progress.TotalFiles <= 0 || !_cancelButton.Enabled)
            {
                return;
            }

            if (_progressBar.Style != ProgressBarStyle.Continuous)
            {
                _progressBar.Style = ProgressBarStyle.Continuous;
            }

            _progressBar.Maximum = progress.TotalFiles;
            _progressBar.Value = Math.Min(progress.ProcessedFiles, progress.TotalFiles);
            _statusLabel.Text = Localizer.Format(
                _progressFormatKey,
                progress.ProcessedFiles + 1,
                progress.TotalFiles,
                progress.CurrentFileName);
        }

        /// <summary>Closes the dialog. Called when the operation has ended, whatever its outcome.</summary>
        public void CloseWhenFinished()
        {
            _canClose = true;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // The dialog can only close when the operation ends; any other attempt means "cancel".
            if (!_canClose)
            {
                e.Cancel = true;
                RequestCancel();
            }

            base.OnFormClosing(e);
        }

        private void OnCancelButtonClick(object sender, EventArgs e)
        {
            RequestCancel();
        }

        private void RequestCancel()
        {
            _cancelButton.Enabled = false;
            _statusLabel.Text = Localizer.Get("progress.cancelling");
            _cancellation.Cancel();
        }
    }
}
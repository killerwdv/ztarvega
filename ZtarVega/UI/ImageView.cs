using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZtarVega.UI
{
    /// <summary>
    /// Draws an image scaled to fit the control while keeping its aspect ratio, or a placeholder
    /// text when there is no image. The control does not own the image and never disposes it.
    /// </summary>
    internal sealed class ImageView : Control
    {
        private const int HighlightBorderWidth = 2;

        private static readonly Color ViewBackColor = Color.FromArgb(32, 32, 32);

        private Image _image;
        private string _placeholderText = string.Empty;
        private bool _isHighlighted;

        public ImageView()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            BackColor = ViewBackColor;
            ForeColor = Color.Gray;
        }

        /// <summary>The image to draw, or null to show the placeholder text.</summary>
        public Image Image
        {
            get { return _image; }
            set
            {
                if (!ReferenceEquals(_image, value))
                {
                    _image = value;
                    Invalidate();
                }
            }
        }

        /// <summary>Text shown when there is no image.</summary>
        public string PlaceholderText
        {
            get { return _placeholderText; }
            set
            {
                _placeholderText = value ?? string.Empty;
                Invalidate();
            }
        }

        /// <summary>Draws a colored border around the control.</summary>
        public bool IsHighlighted
        {
            get { return _isHighlighted; }
            set
            {
                if (_isHighlighted != value)
                {
                    _isHighlighted = value;
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.Clear(BackColor);

            if (_image != null)
            {
                Rectangle target = CalculateFitRectangle(_image.Size, ClientSize);
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.DrawImage(_image, target);
            }
            else if (_placeholderText.Length > 0)
            {
                TextRenderer.DrawText(
                    graphics,
                    _placeholderText,
                    Font,
                    ClientRectangle,
                    ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
            }

            if (_isHighlighted)
            {
                using (var pen = new Pen(Color.DodgerBlue, HighlightBorderWidth))
                {
                    graphics.DrawRectangle(pen, 1, 1, Width - HighlightBorderWidth - 1, Height - HighlightBorderWidth - 1);
                }
            }

            base.OnPaint(e);
        }

        private static Rectangle CalculateFitRectangle(Size imageSize, Size bounds)
        {
            if (imageSize.Width <= 0 || imageSize.Height <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
            {
                return Rectangle.Empty;
            }

            double scale = Math.Min((double)bounds.Width / imageSize.Width, (double)bounds.Height / imageSize.Height);
            int width = Math.Max(1, (int)Math.Round(imageSize.Width * scale));
            int height = Math.Max(1, (int)Math.Round(imageSize.Height * scale));

            return new Rectangle((bounds.Width - width) / 2, (bounds.Height - height) / 2, width, height);
        }
    }
}
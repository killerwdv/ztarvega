using System.Drawing;

namespace ZtarVega.UI
{
    /// <summary>The colors of one visual theme.</summary>
    internal sealed class ThemePalette
    {
        public static readonly ThemePalette Dark = new ThemePalette(
            isDark: true,
            window: Color.FromArgb(30, 30, 30),
            panel: Color.FromArgb(37, 37, 38),
            input: Color.FromArgb(45, 45, 48),
            button: Color.FromArgb(58, 58, 62),
            hover: Color.FromArgb(70, 70, 75),
            border: Color.FromArgb(85, 85, 90),
            text: Color.FromArgb(241, 241, 241),
            muted: Color.FromArgb(153, 153, 153),
            imageBack: Color.FromArgb(20, 20, 20));

        // The light theme uses the colors of the Windows theme, as the application did before.
        public static readonly ThemePalette Light = new ThemePalette(
            isDark: false,
            window: SystemColors.Control,
            panel: SystemColors.Control,
            input: SystemColors.Window,
            button: SystemColors.Control,
            hover: Color.FromArgb(201, 222, 245),
            border: SystemColors.ControlDark,
            text: SystemColors.ControlText,
            muted: SystemColors.GrayText,
            imageBack: Color.FromArgb(32, 32, 32));

        private ThemePalette(
            bool isDark,
            Color window,
            Color panel,
            Color input,
            Color button,
            Color hover,
            Color border,
            Color text,
            Color muted,
            Color imageBack)
        {
            IsDark = isDark;
            Window = window;
            Panel = panel;
            Input = input;
            Button = button;
            Hover = hover;
            Border = border;
            Text = text;
            Muted = muted;
            ImageBack = imageBack;
        }

        public bool IsDark { get; }

        /// <summary>Background of windows and panels.</summary>
        public Color Window { get; }

        /// <summary>Background of menus and of read-only fields.</summary>
        public Color Panel { get; }

        /// <summary>Background of text boxes, lists and drop-downs.</summary>
        public Color Input { get; }

        public Color Button { get; }

        /// <summary>Background of a button or menu item under the mouse.</summary>
        public Color Hover { get; }

        public Color Border { get; }

        public Color Text { get; }

        /// <summary>Color of secondary text (hints, captions).</summary>
        public Color Muted { get; }

        /// <summary>Background of the areas that show images.</summary>
        public Color ImageBack { get; }

        /// <summary>Background of the main action button (Play).</summary>
        public Color Accent
        {
            get { return Color.FromArgb(0, 120, 215); }
        }

        public Color AccentText
        {
            get { return Color.White; }
        }
    }
}
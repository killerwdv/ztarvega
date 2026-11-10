using System;
using System.Drawing;
using System.Windows.Forms;
using ZtarVega.Models;

namespace ZtarVega.UI
{
    /// <summary>Holds the current visual theme and applies it to windows and controls.</summary>
    internal static class ThemeManager
    {
        /// <summary>Marks a button that uses the accent color (the Play button) instead of the regular button colors.</summary>
        public const string AccentButtonTag = "accent";

        private static ThemePalette _current = ThemePalette.Dark;

        public static ThemePalette Current
        {
            get { return _current; }
        }

        /// <summary>Chooses the theme by its name (<see cref="AppSettings.DarkTheme"/> or <see cref="AppSettings.LightTheme"/>).</summary>
        public static void SetTheme(string themeName)
        {
            _current = string.Equals(themeName, AppSettings.LightTheme, StringComparison.OrdinalIgnoreCase)
                ? ThemePalette.Light
                : ThemePalette.Dark;
        }

        /// <summary>Applies the current theme to a control and to everything it contains.</summary>
        public static void Apply(Control root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            ThemePalette palette = _current;
            ApplyToControl(root, palette);

            var form = root as Form;

            if (form != null && form.IsHandleCreated)
            {
                NativeTheme.SetDarkTitleBar(form.Handle, palette.IsDark);
            }
        }

        /// <summary>Gives a button the accent look. The color depends on whether the button is enabled.</summary>
        public static void StyleAccentButton(Button button)
        {
            ThemePalette palette = _current;

            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = button.Enabled ? palette.Accent : palette.Button;
            button.ForeColor = button.Enabled ? palette.AccentText : palette.Muted;
        }

        private static void ApplyToControl(Control control, ThemePalette palette)
        {
            if (control is MenuStrip menu)
            {
                ApplyToMenu(menu, palette);
                return;
            }

            if (control is ProgressBar)
            {
                // The progress bar keeps the Windows style.
                return;
            }

            if (control is ImageView imageView)
            {
                imageView.BackColor = palette.ImageBack;
                imageView.ForeColor = palette.Muted;
            }
            else if (control is ListView || control is ListBox)
            {
                control.BackColor = palette.Input;
                control.ForeColor = palette.Text;
                NativeTheme.ApplyControlTheme(control, palette.IsDark);
            }
            else if (control is ComboBox comboBox)
            {
                comboBox.FlatStyle = palette.IsDark ? FlatStyle.Flat : FlatStyle.Standard;
                comboBox.BackColor = palette.Input;
                comboBox.ForeColor = palette.Text;
            }
            else if (control is TextBoxBase textBox)
            {
                textBox.BackColor = textBox.ReadOnly ? palette.Panel : palette.Input;
                textBox.ForeColor = palette.Text;

                if (textBox.BorderStyle != BorderStyle.FixedSingle)
                {
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                }
            }
            else if (control is Button button)
            {
                ApplyToButton(button, palette);
            }
            else if (control is Label label)
            {
                // Secondary text (hints, captions) is created in the gray of the Windows theme.
                label.ForeColor = IsMuted(label.ForeColor) ? palette.Muted : palette.Text;
            }
            else if (control is ButtonBase)
            {
                // Check boxes and radio buttons.
                control.ForeColor = palette.Text;
            }
            else
            {
                // Windows, user controls and container panels.
                control.BackColor = palette.Window;
                control.ForeColor = palette.Text;

                var scrollable = control as ScrollableControl;

                if (scrollable != null && scrollable.AutoScroll)
                {
                    NativeTheme.ApplyControlTheme(control, palette.IsDark);
                }
            }

            foreach (Control child in control.Controls)
            {
                ApplyToControl(child, palette);
            }
        }

        private static void ApplyToButton(Button button, ThemePalette palette)
        {
            if (AccentButtonTag.Equals(button.Tag))
            {
                StyleAccentButton(button);
                return;
            }

            if (palette.IsDark)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.UseVisualStyleBackColor = false;
                button.BackColor = palette.Button;
                button.ForeColor = palette.Text;
                button.FlatAppearance.BorderColor = palette.Border;
                button.FlatAppearance.MouseOverBackColor = palette.Hover;
                button.FlatAppearance.MouseDownBackColor = palette.Border;
            }
            else
            {
                // The light theme goes back to the standard look of Windows.
                button.FlatStyle = FlatStyle.Standard;
                button.UseVisualStyleBackColor = true;
                button.ForeColor = SystemColors.ControlText;
            }
        }

        private static bool IsMuted(Color color)
        {
            return color == SystemColors.GrayText || color == ThemePalette.Dark.Muted;
        }

        private static void ApplyToMenu(MenuStrip menu, ThemePalette palette)
        {
            var renderer = new PaletteRenderer(palette);

            menu.Renderer = renderer;
            menu.BackColor = palette.Panel;
            menu.ForeColor = palette.Text;

            foreach (ToolStripItem item in menu.Items)
            {
                ApplyToMenuItem(item, palette, renderer);
            }
        }

        private static void ApplyToMenuItem(ToolStripItem item, ThemePalette palette, ToolStripRenderer renderer)
        {
            item.ForeColor = palette.Text;

            var dropDownItem = item as ToolStripDropDownItem;

            if (dropDownItem == null)
            {
                return;
            }

            // Submenus do not always take the renderer of their menu bar, so it is set on each one.
            dropDownItem.DropDown.Renderer = renderer;
            dropDownItem.DropDown.BackColor = palette.Panel;
            dropDownItem.DropDown.ForeColor = palette.Text;

            foreach (ToolStripItem child in dropDownItem.DropDownItems)
            {
                ApplyToMenuItem(child, palette, renderer);
            }
        }

        /// <summary>Draws menus with the colors of a palette.</summary>
        private sealed class PaletteRenderer : ToolStripProfessionalRenderer
        {
            private readonly ThemePalette _palette;

            public PaletteRenderer(ThemePalette palette)
                : base(new PaletteColorTable(palette))
            {
                _palette = palette;
                RoundedEdges = false;
            }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                e.ArrowColor = _palette.Text;
                base.OnRenderArrow(e);
            }
        }

        /// <summary>The colors the menu renderer asks for, taken from a palette.</summary>
        private sealed class PaletteColorTable : ProfessionalColorTable
        {
            private readonly ThemePalette _palette;

            public PaletteColorTable(ThemePalette palette)
            {
                _palette = palette;
                UseSystemColors = false;
            }

            public override Color MenuItemSelected => _palette.Hover;

            public override Color MenuItemSelectedGradientBegin => _palette.Hover;

            public override Color MenuItemSelectedGradientEnd => _palette.Hover;

            public override Color MenuItemBorder => _palette.Border;

            public override Color MenuBorder => _palette.Border;

            public override Color MenuItemPressedGradientBegin => _palette.Panel;

            public override Color MenuItemPressedGradientMiddle => _palette.Panel;

            public override Color MenuItemPressedGradientEnd => _palette.Panel;

            public override Color MenuStripGradientBegin => _palette.Panel;

            public override Color MenuStripGradientEnd => _palette.Panel;

            public override Color ToolStripDropDownBackground => _palette.Panel;

            public override Color ImageMarginGradientBegin => _palette.Panel;

            public override Color ImageMarginGradientMiddle => _palette.Panel;

            public override Color ImageMarginGradientEnd => _palette.Panel;

            public override Color SeparatorDark => _palette.Border;

            public override Color SeparatorLight => _palette.Border;
        }
    }
}
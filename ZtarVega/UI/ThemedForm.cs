using System;
using System.Windows.Forms;

namespace ZtarVega.UI
{
    /// <summary>Base class of the application windows: it applies the current theme when the window opens.</summary>
    internal class ThemedForm : Form
    {
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeTheme.SetDarkTitleBar(Handle, ThemeManager.Current.IsDark);
        }

        protected override void OnLoad(EventArgs e)
        {
            // By now every control has been created, so the whole window is themed at once.
            ThemeManager.Apply(this);
            base.OnLoad(e);
        }
    }
}
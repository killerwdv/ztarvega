using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZtarVega.UI
{
    /// <summary>
    /// Asks Windows to draw parts of the window (title bar, scroll bars, column headers) with its dark style.
    /// These features exist only in recent versions of Windows 10 and in Windows 11. Everywhere else the calls
    /// fail harmlessly and those parts keep the normal style.
    /// </summary>
    internal static class NativeTheme
    {
        private const string DarkExplorerStyle = "DarkMode_Explorer";

        // The attribute that switches the title bar to the dark style has a different number
        // in the first versions of Windows 10 that support it.
        private const int DwmUseImmersiveDarkMode = 20;
        private const int DwmUseImmersiveDarkModeBeforeWindows10Update2004 = 19;

        // Controls that already ask for the theme again whenever their handle is created.
        private static readonly ConditionalWeakTable<Control, object> TrackedControls = new ConditionalWeakTable<Control, object>();

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr windowHandle, string subAppName, string subIdList);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute, ref int value, int size);

        /// <summary>Switches the title bar of a window between the dark and the normal style.</summary>
        public static void SetDarkTitleBar(IntPtr formHandle, bool dark)
        {
            int value = dark ? 1 : 0;

            try
            {
                if (DwmSetWindowAttribute(formHandle, DwmUseImmersiveDarkMode, ref value, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(formHandle, DwmUseImmersiveDarkModeBeforeWindows10Update2004, ref value, sizeof(int));
                }
            }
            catch (DllNotFoundException)
            {
                // The system does not offer this feature.
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        /// <summary>
        /// Draws the scroll bars and column headers of a control with the dark style (or the normal one).
        /// WinForms can create the window of a control again, so the style is requested every time that happens.
        /// </summary>
        public static void ApplyControlTheme(Control control, bool dark)
        {
            if (control.IsHandleCreated)
            {
                SetControlStyle(control.Handle, dark);
            }

            object marker;

            if (!TrackedControls.TryGetValue(control, out marker))
            {
                TrackedControls.Add(control, new object());
                control.HandleCreated += (sender, e) => SetControlStyle(((Control)sender).Handle, ThemeManager.Current.IsDark);
            }
        }

        private static void SetControlStyle(IntPtr handle, bool dark)
        {
            try
            {
                SetWindowTheme(handle, dark ? DarkExplorerStyle : null, null);
            }
            catch (DllNotFoundException)
            {
                // The system does not offer this feature.
            }
            catch (EntryPointNotFoundException)
            {
            }
        }
    }
}
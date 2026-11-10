using System.Windows.Forms;

namespace ZtarVega.UI
{
    /// <summary>Shows the message boxes of the application, so all of them have the same title and look.</summary>
    internal static class MessageDialogs
    {
        public const string Caption = "ZtarVega";

        public static void ShowInformation(IWin32Window owner, string message)
        {
            MessageBox.Show(owner, message, Caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void ShowWarning(IWin32Window owner, string message)
        {
            MessageBox.Show(owner, message, Caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>Asks a yes/no question. Returns true when the user answers Yes.</summary>
        public static bool Ask(IWin32Window owner, string message, MessageBoxIcon icon, bool noIsDefault = false)
        {
            DialogResult answer = MessageBox.Show(
                owner,
                message,
                Caption,
                MessageBoxButtons.YesNo,
                icon,
                noIsDefault ? MessageBoxDefaultButton.Button2 : MessageBoxDefaultButton.Button1);

            return answer == DialogResult.Yes;
        }
    }
}
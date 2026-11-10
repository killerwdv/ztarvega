using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using ZtarVega.Localization;
using ZtarVega.Services;

namespace ZtarVega.UI.Actions
{
    /// <summary>The actions related to the settings: the settings window and the restart that applies the theme and the language.</summary>
    internal sealed class SettingsActions : ViewActions
    {
        private readonly SettingsSession _settings;

        public SettingsActions(SettingsSession settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void ShowSettingsDialog()
        {
            string previousTheme = _settings.Current.ThemeName;
            string previousLanguage = Localizer.CurrentCode;

            using (var dialog = new SettingsForm(_settings.Current))
            {
                if (dialog.ShowDialog(MainWindow) != DialogResult.OK)
                {
                    return;
                }

                _settings.Replace(dialog.Settings);
            }

            try
            {
                _settings.Save();
            }
            catch (IOException exception)
            {
                ShowSaveWarning(exception);
                return;
            }
            catch (UnauthorizedAccessException exception)
            {
                ShowSaveWarning(exception);
                return;
            }

            // The theme and the language are applied when the application starts, which keeps every window consistent.
            bool appearanceChanged =
                !string.Equals(previousTheme, _settings.Current.ThemeName, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(previousLanguage, _settings.Current.LanguageCode, StringComparison.OrdinalIgnoreCase);

            if (appearanceChanged)
            {
                OfferRestart();
            }
        }

        /// <summary>Tells the user something has to be configured and offers to open the settings window.</summary>
        public void AskToOpenSettings(string message)
        {
            if (MessageDialogs.Ask(MainWindow, Localizer.Format("main.askSettings", message), MessageBoxIcon.Information))
            {
                ShowSettingsDialog();
            }
        }

        private void OfferRestart()
        {
            if (MessageDialogs.Ask(MainWindow, Localizer.Get("main.restart.question"), MessageBoxIcon.Question))
            {
                RestartApplication();
            }
        }

        /// <summary>Starts a new copy of the application and closes this one. The settings are saved when the window closes.</summary>
        private void RestartApplication()
        {
            try
            {
                Process.Start(Application.ExecutablePath, Program.RestartArgument);
            }
            catch (Win32Exception)
            {
                MessageDialogs.ShowWarning(MainWindow, Localizer.Get("main.restart.failed"));
                return;
            }

            MainWindow.CloseWindow();
        }

        private void ShowSaveWarning(Exception exception)
        {
            MessageDialogs.ShowWarning(MainWindow, Localizer.Format("main.settingsSaveWarning", exception.Message));
        }
    }
}
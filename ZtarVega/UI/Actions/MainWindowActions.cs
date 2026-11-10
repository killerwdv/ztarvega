using System;

namespace ZtarVega.UI.Actions
{
    /// <summary>Every group of actions of the main window, so the window receives them as one piece.</summary>
    internal sealed class MainWindowActions
    {
        public MainWindowActions(
            GameActions games,
            ImportActions imports,
            DownloadActions downloads,
            SettingsActions settings,
            GuideActions guide)
        {
            Games = games ?? throw new ArgumentNullException(nameof(games));
            Imports = imports ?? throw new ArgumentNullException(nameof(imports));
            Downloads = downloads ?? throw new ArgumentNullException(nameof(downloads));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Guide = guide ?? throw new ArgumentNullException(nameof(guide));
        }

        public GameActions Games { get; }

        public ImportActions Imports { get; }

        public DownloadActions Downloads { get; }

        public SettingsActions Settings { get; }

        public GuideActions Guide { get; }

        /// <summary>Attaches every group of actions to the main window.</summary>
        public void Attach(IMainView mainWindow)
        {
            Games.Attach(mainWindow);
            Imports.Attach(mainWindow);
            Downloads.Attach(mainWindow);
            Settings.Attach(mainWindow);
            Guide.Attach(mainWindow);
        }
    }
}
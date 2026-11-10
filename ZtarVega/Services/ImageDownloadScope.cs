namespace ZtarVega.Services
{
    /// <summary>Which games the images are downloaded for.</summary>
    internal enum ImageDownloadScope
    {
        /// <summary>The games whose ROM the user has.</summary>
        AvailableGames,

        /// <summary>Every game of the catalog.</summary>
        AllGames,

        /// <summary>Only the game selected in the list.</summary>
        SelectedGame
    }
}
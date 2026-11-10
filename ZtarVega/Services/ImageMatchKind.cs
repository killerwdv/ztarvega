namespace ZtarVega.Services
{
    /// <summary>How an image file was matched to a game.</summary>
    internal enum ImageMatchKind
    {
        /// <summary>No game has that title.</summary>
        None,

        /// <summary>The file name is the name of one specific game (same region and version).</summary>
        Exact,

        /// <summary>Only the title matches, so the image goes to the main version of the game.</summary>
        Title,

        /// <summary>Several different games could own the image, so none is chosen.</summary>
        Ambiguous
    }
}
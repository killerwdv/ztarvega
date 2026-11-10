using System.Windows.Forms;
using ZtarVega.Models;

namespace ZtarVega.UI
{
    /// <summary>What the actions of the application need from the main window.</summary>
    internal interface IMainView : IWin32Window
    {
        /// <summary>The game selected in the list, or null.</summary>
        Game SelectedGame { get; }

        /// <summary>The filter mode chosen in the list.</summary>
        GameFilterMode FilterMode { get; }

        /// <summary>Rebuilds the list from the catalog, keeping the selected game selected.</summary>
        void RefreshGames();

        /// <summary>Rebuilds the list and selects the given game.</summary>
        void RefreshGames(int gameIdToSelect);

        /// <summary>Repaints the visible rows of the list.</summary>
        void RedrawGameRows();

        /// <summary>Updates the favorite button to the state of the selected game.</summary>
        void RefreshFavoriteState();

        /// <summary>
        /// Makes the panels let go of their images and show those of the selected game again. It has to be done
        /// before the image cache disposes images, because a control that shows a disposed image fails to repaint.
        /// </summary>
        void ReloadSelectedGameImages();

        /// <summary>Shows or hides the waiting cursor while a long operation runs.</summary>
        void SetBusy(bool isBusy);

        void CloseWindow();
    }
}
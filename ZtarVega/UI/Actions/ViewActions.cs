using System;

namespace ZtarVega.UI.Actions
{
    /// <summary>
    /// Base class of the groups of actions of the main window. The actions are created before the window, all
    /// together at the start of the application, and attached to it once the window exists.
    /// </summary>
    internal abstract class ViewActions
    {
        private IMainView _mainWindow;

        /// <summary>The main window. Only available after <see cref="Attach"/>.</summary>
        protected IMainView MainWindow
        {
            get
            {
                if (_mainWindow == null)
                {
                    throw new InvalidOperationException("The actions were used before being attached to the main window.");
                }

                return _mainWindow;
            }
        }

        public void Attach(IMainView mainWindow)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        }
    }
}
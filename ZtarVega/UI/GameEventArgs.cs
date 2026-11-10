using System;
using ZtarVega.Models;

namespace ZtarVega.UI
{
    /// <summary>Carries the game involved in a UI event. The game is null when the selection was cleared.</summary>
    internal sealed class GameEventArgs : EventArgs
    {
        public GameEventArgs(Game game)
        {
            Game = game;
        }

        public Game Game { get; }
    }
}
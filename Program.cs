// ============================================================================
// Program.cs — Entry Point
// Author: Mehdi Lakhouane
// Description: Application entry point. Creates and runs the Game1 instance.
// ============================================================================

using TheGame.Core;

namespace TheGame
{
    public static class Program
    {
        static void Main(string[] args)
        {
            using var game = new Game1();
            game.Run();
        }
    }
}

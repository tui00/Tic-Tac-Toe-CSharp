namespace TicTacToe.Core.Bots;

public class BestBot : IBot
{
    public int GetTurn(Game game, Random random)
    {
        if (game.TryWinAndBlock(out int priorityCell)) return priorityCell;
        return game.GetBestTurn();
    }
}

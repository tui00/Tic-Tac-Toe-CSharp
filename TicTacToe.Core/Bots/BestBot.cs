namespace TicTacToe.Core.Bots;

public class BestBot : IBot
{
    public int GetTurn(TicTacToe game, Random random)
    {
        if (game.TryWinAndBlock(out int priorityCell)) return priorityCell;
        return game.GetBestTurn();
    }
}

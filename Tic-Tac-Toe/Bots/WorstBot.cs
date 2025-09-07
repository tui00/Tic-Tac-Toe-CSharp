using static TicTacToe.TicTacToe;

namespace TicTacToe.Bots;

public class WorstBot : MiniMaxBot, IBot
{
    private static Dictionary<uint, (int cell, int score)> worstTranspositionTable = [];
    public new int GetTurn(TicTacToe game, Random random)
    {
        int I = game.ReadWhoseTurn();
        int Enemy = game.ReadWhoseTurn() ^ XO;
        (int result, int score) = Search(game, I, Enemy, ref worstTranspositionTable);
        return result == -1 ? game.GetWorstTurn() : result;
    }

    protected override bool TryEvaluate(TicTacToe game, int I, int depth, out int result)
    {
        uint winner = game.ReadWinner();
        result = 10 - depth;
        result = -result;
        if (winner == X) return true;
        else if (winner == O) { result = -result; return true; }
        else if (winner == XO) { result = 0; return true; }
        else if ((game.FindWinningMoves(I) > 1) && (I == X)) { result *= 2; return true; }
        else if ((game.FindWinningMoves(I) > 1) && (I == O)) { result *= -2; return true; }
        return false;
    }
}

using static TicTacToe.TicTacToe;

namespace TicTacToe;

public class DrawBot : MiniMaxBot, IBot
{
    protected static Dictionary<uint, (int cell, int score)> drawsTranspositionTable = [];
    public new int GetTurn(TicTacToe game, Random random)
    {
        int I = game.ReadWhoseTurn();
        int Enemy = game.ReadWhoseTurn() ^ XO;
        (int result, int score) = Search(game, I, Enemy, ref drawsTranspositionTable);
        return result == -1 ? game.GetBestTurn() : result;
    }

    protected override bool TryEvaluate(TicTacToe game, int I, int depth, out int result)
    {
        uint winner = game.ReadWinner();
        result = 10 - depth;
        int antiResult = -result;
        if (winner == XO) { result = (I == X) ? result : antiResult; return true; }
        else if (winner != EMPTY) { result = (I == X) ? antiResult : result; return true; }
        else if (game.FindWinningMoves(I) >= 2) { result = antiResult; return true; }
        else if (game.FindWinningMoves(I == X ? O : X) >= 2) { result = antiResult; return true; }
        return false;
    }
}
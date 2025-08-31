using static TicTacToe.TicTacToe;

namespace TicTacToe;

public class MiniMaxBot : IBot
{
    private static Dictionary<uint, (int cell, int score)> transpositionTable = [];
    private static readonly Lock tableLock = new();

    public int GetTurn(TicTacToe game, Random random)
    {
        if (game.TryWinAndBlock(out int cell)) return cell;
        int I = game.IsXTurn() ? X : O;
        int Enemy = game.IsXTurn() ? O : X;
        (int result, int score) = Search(game, I, Enemy, ref transpositionTable);
        return result == -1 ? game.GetBestTurn() : result;
    }

    protected (int cell, int score) Search(TicTacToe game, int I, int Enemy, ref Dictionary<uint, (int cell, int score)> transpositionTable, int depth = 0, int alpha = int.MinValue, int beta = int.MaxValue)
    {
        lock (tableLock)
        {
            if (transpositionTable.TryGetValue(game.ReadBoard(), out var cached))
                return cached;
        }

        int bestCell = -1;
        int bestScore = I == X ? int.MinValue : int.MaxValue;

        if (TryEvaluate(game, I, depth, out int evaluatedScore)) return (bestCell, evaluatedScore);

        for (int i = 0; i < 9; i++)
        {
            if (!game.IsLegalMove(i)) continue;

            game.TestTurnStart(i, I);
            (int _, int score) = Search(game, Enemy, I, ref transpositionTable, depth + 1, alpha, beta);
            game.TestTurnStop();

            if (I == X)
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = i;
                }
                alpha = Math.Max(alpha, bestScore);
                if (alpha >= beta) break;
            }
            else if (I == O)
            {
                if (score < bestScore)
                {
                    bestScore = score;
                    bestCell = i;
                }
                beta = Math.Min(beta, bestScore);
                if (alpha >= beta) break;
            }
        }

        lock (tableLock)
        {
            transpositionTable[game.ReadBoard()] = (bestCell, bestScore);
            if (transpositionTable.Count > 10000)
                transpositionTable.Remove(transpositionTable.Keys.First());
        }
        return (bestCell, bestScore);
    }

    protected virtual bool TryEvaluate(TicTacToe game, int I, int depth, out int result)
    {
        uint winner = game.ReadWinner();
        result = 10 - depth;
        if (winner == X) return true;
        else if (winner == O) { result = -result; return true; }
        else if (winner == XO) { result = 0; return true; }
        else if ((game.FindWinningMoves(I) > 1) && (I == X)) { result *= 2; return true; }
        else if ((game.FindWinningMoves(I) > 1) && (I == O)) { result *= -2; return true; }
        return false;
    }
}

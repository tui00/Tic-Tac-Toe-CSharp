using static TicTacToe.TicTacToe;

namespace TicTacToe.Bots;

public class XAndOSchemeBot : IBot
{
    protected bool enemyInCorner;
    protected bool enemyInFarCorner;
    protected bool enemyOnCenter;
    protected bool enemyOnEdge;

    public int GetTurn(TicTacToe game, Random random)
    {
        if (game.TryWinAndBlock(out int priorityCell)) return priorityCell;
        return game.ReadWhoseTurn() == X ? XScheme(game, random) : OScheme(game, random);
    }

    protected int XScheme(TicTacToe game, Random random)
    {
        uint round = game.ReadCurrentRound();

        if (round == 0) return 0;
        if (round == 1)
        {
            enemyInCorner = Corners.Any(corner => game.ReadCellType(corner) == O);
            enemyInFarCorner = game.ReadCellType(8) == O;
            enemyOnEdge = Edges.Any(side => game.ReadCellType(side) == O);
            enemyOnCenter = game.ReadCellType(4) == O;

            if (enemyOnCenter) return 8;
            if (enemyOnEdge || enemyInFarCorner) return 4;
            if (enemyInCorner)
            {
                if (game.ReadCellType(2) == O) return 3;
                if (game.ReadCellType(6) == O) return 1;
            }
        }
        if (round == 2)
        {
            if (enemyOnEdge || enemyOnCenter || enemyInFarCorner)
            {
                if (game.IsLegalMove(2)) return 2;
                if (game.IsLegalMove(6)) return 6;
            }
            if (enemyInCorner) if (game.IsLegalMove(4)) return 4;
        }

        return game.GetBestTurn();
    }
    protected static int OScheme(TicTacToe game, Random random)
    {
        if (game.IsLegalMove(4)) return 4;
        int OccupiedCorners = Corners.Count(corner => game.ReadCellType(corner) == X);
        if (OccupiedCorners >= 2)
        {
            int[] Edges = TicTacToe.Edges;
            random.Shuffle(Edges);
            foreach (int Edge in Edges) if (game.IsLegalMove(Edge)) return Edge;
        }
        return game.GetBestTurn();
    }
}

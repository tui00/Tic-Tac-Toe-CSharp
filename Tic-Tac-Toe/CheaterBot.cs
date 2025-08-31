using static TicTacToe.TicTacToe;

namespace TicTacToe;

public class CheaterBot : IBot
{
    public int GetTurn(TicTacToe game, Random random)
    {
        int I = game.IsXTurn() ? X : O;
        int Enemy = game.IsXTurn() ? O : X;
        if (TryWin(game, I, out int priorityCell)) return priorityCell;

        if (random.Next(3) == 0)
        {
            int chitedCell = -1;
            if (game.FindWinningMoves(Enemy) >= 2) return SwapXAndO(game, I);
            else if (game.FindWinningMoves(Enemy) == 1)
            {
                chitedCell = ReplaceWinningCell(game, I, Enemy);
            }
            if (game.ReadCurrentTurn() <= 7)
            {
                if (random.Next(2) == 0)
                {
                    game.state |= (uint)(I << (game.GetBestTurn() << 1)); // Сходим дважды
                }
                else
                {
                    game.state |= (uint)(Enemy << (game.GetWorstTurn() << 1)); // Сходить вражеской вигурой? Почему бы и нет
                    chitedCell = NoTurn(game, I);
                }
            }
            ReCountTurns(game);
            if (chitedCell != -1) return chitedCell;
        }

        return game.GetBestTurn();
    }
    protected static void ReCountTurns(TicTacToe game)
    {
        game.state &= ~(uint)(0b1111 << TURN_COUNTER);
        for (int i = 0; i < 9; i++)
        {
            if (game.ReadCellType(i) != EMPTY) game.state += 1u << TURN_COUNTER;
        }
    }
    protected static int NoTurn(TicTacToe game, int I) // Эта функция НЕ делает ход. Т. е. ничего не происходит
    {
        for (int i = 0; i < 9; i++)
        {
            if (game.ReadCellType(i) == I)
            {
                game.state &= ~(uint)(0b11 << (i << 1));
                return i;
            }
        }
        return game.GetBestTurn(); // Никогда до сюда не дойдет так что безопасно
    }

    protected static int SwapXAndO(TicTacToe game, int I)
    {
        uint state = game.state;

        for (int i = 0; i < 9; i++)
        {
            if (game.ReadCellType(i) == EMPTY) continue;
            state ^= (uint)(XO << (i << 1));
        }

        game.state = state;
        return NoTurn(game, I);
    }

    protected static int ReplaceWinningCell(TicTacToe game, int I, int Enemy)
    {
        uint original = game.state;
        for (int i = 0; i < 9; i++)
        {
            if (game.ReadCellType(i) == EMPTY) continue;
            game.state &= ~(uint)(0b11 << (i << 1));
            if (game.FindWinningMoves(Enemy) == 0)
            {
                break;
            }
            game.state = original;
        }
        return NoTurn(game, I);
    }

    protected static bool TryWin(TicTacToe game, int I, out int cell) => // But not Block 
        game.TryFindWinningMove(I, out cell);
}

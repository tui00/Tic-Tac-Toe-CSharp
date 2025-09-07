namespace TicTacToe.Core.Bots;

public class RandomBot : IBot
{
    public int GetTurn(TicTacToe game, Random random)
    {
        int suitableCell;
        do suitableCell = random.Next(9); while (!game.IsLegalMove(suitableCell));
        return suitableCell;
    }
}


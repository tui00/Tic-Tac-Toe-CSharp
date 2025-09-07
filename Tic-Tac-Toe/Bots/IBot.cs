namespace TicTacToe.Bots;

public interface IBot
{
    abstract int GetTurn(TicTacToe game, Random random);
}

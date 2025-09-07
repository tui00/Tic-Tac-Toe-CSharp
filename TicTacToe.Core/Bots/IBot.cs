namespace TicTacToe.Core.Bots;

public interface IBot
{
    abstract int GetTurn(Game game, Random random);
}

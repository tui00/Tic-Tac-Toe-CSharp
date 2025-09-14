using TicTacToe.Core;

namespace TicTacToe.Api;

internal record GameState(Game Game, int ConnectedPlayers)
{
    public int ConnectedPlayers = ConnectedPlayers;
}

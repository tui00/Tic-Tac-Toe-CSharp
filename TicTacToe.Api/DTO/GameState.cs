using TicTacToe.Core;

namespace TicTacToe.Api.Dto;

internal record GameState(Game Game, int ConnectedPlayers)
{
    public int ConnectedPlayers = ConnectedPlayers;
}

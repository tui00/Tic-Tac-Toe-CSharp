using TicTacToe.Core;

namespace TicTacToe.Api.Dto;

public record GameState(Game Game, int ConnectedPlayers)
{
    public int ConnectedPlayers = ConnectedPlayers;
}

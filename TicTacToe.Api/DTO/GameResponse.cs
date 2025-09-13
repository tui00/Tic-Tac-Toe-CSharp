namespace TicTacToe.Api.Dto;

public record GameResponse(string Board, int Turn, uint Winner, int ConnectedPlayers);

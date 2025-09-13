namespace TicTacToe.Cli;

class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await new Cli().Start();
    }
}

public record NewGameRequest(uint XLevel, uint OLevel);
public record NewGameResponse(Guid Id);

public record GameState(string Board, int Turn, uint Winner, int ConnectedPlayers);
public record MakeTurnRequest(int Cell);

public record ListGamesResponse(Guid[] Ids);

public record IsLegalResponse(bool IsLegal);

public record ConnectPlayerRequest(int Player);

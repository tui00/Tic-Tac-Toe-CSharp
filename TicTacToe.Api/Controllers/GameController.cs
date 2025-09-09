using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TicTacToe.Core;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController(IMemoryCache cache) : ControllerBase
{
    private readonly IMemoryCache _cache = cache;
    private static readonly ConcurrentBag<Guid> _usedGuids = [];

    // POST /api/game/new
    [HttpPost("new")]
    public IActionResult CreateGame([FromBody] NewGameRequest newGame)
    {
        Game game = new(newGame.XLevel, newGame.OLevel);
        Guid guid = Guid.NewGuid();
        _cache.Set(guid, new GameState(game, 0), TimeSpan.FromMinutes(30));
        _usedGuids.Add(guid);

        return Ok(new NewGameResponse(guid));
    }

    // GET /api/game/list
    [HttpGet("list")]
    public IActionResult ListGames()
    {
        Guid[] activeGuids = [.. _usedGuids.Where(id => _cache.TryGetValue(id, out var _))];
        return Ok(new ListGamesResponse(activeGuids));
    }

    // GET /api/game/{id}/isLegal/{cell}
    [HttpGet("{id}/isLegal/{cell}")]
    public IActionResult IsLegal(Guid id, int cell)
    {
        if (!_cache.TryGetValue(id, out GameState? state))
            return NotFound(new { error = "Game not found" });

        return Ok(new IsLegalResponse(state!.Game.IsLegalMove(cell)));
    }

    // GET /api/game/{id}
    [HttpGet("{id}")]
    public IActionResult GetGame(Guid id)
    {
        if (!_cache.TryGetValue(id, out GameState? state))
            return NotFound(new { error = "Game not found" });

        return Ok(new GameResponse(FormatBoard(state!), state!.Game.ReadWhoseTurn(), state.Game.ReadWinner(), state.ConnectedPlayers));
    }

    // POST /api/game/{id}
    [HttpPost("{id}")]
    public IActionResult MakeMove(Guid id, [FromBody] MakeTurnRequest request)
    {
        if (!_cache.TryGetValue(id, out GameState? state))
            return NotFound(new { error = "Game not found" });

        if (state!.Game.ReadWhoseTurn() == Game.X) state.ConnectedPlayers |= Game.X;
        else state.ConnectedPlayers |= Game.O;

        state.Game.MakeTurn(request.Cell);
        if (state.Game.ReadCurrentPlayerLevel() != Game.HUMAN) state.Game.MakeTurn(); // Сходить за бота

        _cache.Set(id, state, TimeSpan.FromMinutes(30));

        return GetGame(id);
    }

    private static string FormatBoard(GameState game)
    {
        string board = "";
        for (int i = 0; i < 9; i++)
        {
            uint cell = game.Game.ReadCellType(i);
            board += cell == Game.EMPTY ? '-' : cell == Game.X ? 'x' : cell == Game.O ? 'o' : '=';
        }
        return board;
    }
}


public record GameState(Game Game, int ConnectedPlayers)
{
    public int ConnectedPlayers = ConnectedPlayers;
}

public record NewGameRequest(uint XLevel, uint OLevel);
public record NewGameResponse(Guid Id);

public record GameResponse(string Board, int Turn, uint Winner, int ConnectedPlayers);
public record MakeTurnRequest(int Cell);

public record ListGamesResponse(Guid[] Ids);

public record IsLegalResponse(bool IsLegal);

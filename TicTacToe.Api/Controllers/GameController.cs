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
        Guid id = Guid.NewGuid();
        _cache.Set(id, game, TimeSpan.FromMinutes(30));
        _usedGuids.Add(id);

        return Ok(new NewGameResponse(id));
    }

    // GET /api/game/list
    [HttpGet("list")]
    public IActionResult ListGames()
    {
        Guid[] activeGuids = [.. _usedGuids.Where(id => _cache.TryGetValue(id, out var _))];
        return Ok(new ListGamesResponse(activeGuids));
    }

    // GET /api/game/{id}
    [HttpGet("{id}")]
    public IActionResult GetGame(Guid id)
    {
        if (!_cache.TryGetValue(id, out Game? game))
            return NotFound(new { error = "Game not found" });

        return Ok(new GameResponse(FormatBoard(game!), game!.ReadWhoseTurn(), game.ReadWinner()));
    }

    // POST /api/game/{id}
    [HttpPost("{id}")]
    public IActionResult MakeMove(Guid id, [FromBody] MakeTurnRequest request)
    {
        if (!_cache.TryGetValue(id, out Game? game))
            return NotFound(new { error = "Game not found" });

        game!.MakeTurn(request.Cell);
        if (game.ReadCurrentPlayerLevel() != Game.HUMAN) game.MakeTurn(); // Сходить за бота

        _cache.Set(id, game, TimeSpan.FromMinutes(30));

        return GetGame(id);
    }

    private static string FormatBoard(Game game)
    {
        string board = "";
        for (int i = 0; i < 9; i++)
        {
            uint cell = game.ReadCellType(i);
            board += cell == Game.EMPTY ? '-' : cell == Game.X ? 'x' : cell == Game.O ? 'o' : '=';
        }
        return board;
    }
}


public record NewGameRequest(uint XLevel, uint OLevel);
public record NewGameResponse(Guid Id);

public record GameResponse(string Board, int Turn, uint Winner);
public record MakeTurnRequest(int Cell);

public record ListGamesResponse(Guid[] Ids);

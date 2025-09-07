using Microsoft.AspNetCore.Mvc;
using TicTacToe.Core;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    // Храним игры в памяти (для теста)
    private static readonly Dictionary<Guid, Game> Games = [];

    // POST /api/game/new
    [HttpPost("new")]
    public IActionResult CreateGame([FromBody] NewGameRequest newGame)
    {
        Game game = new(newGame.XLevel, newGame.OLevel);
        Guid id = Guid.NewGuid();
        Games.Add(id, game);

        return Ok(new NewGameResponse(id));
    }

    // GET /api/game/{id}
    [HttpGet("{id}")]
    public IActionResult GetGame(Guid id)
    {
        if (!Games.TryGetValue(id, out Game? game))
            return NotFound(new { error = "Game not found" });

        return Ok(new GameResponse(FormatBoard(game), game.ReadWhoseTurn(), game.ReadWinner()));
    }

    // POST /api/game/{id}
    [HttpPost("{id}")]
    public IActionResult MakeMove(Guid id, [FromBody] MakeTurnRequest request)
    {
        if (!Games.TryGetValue(id, out Game? game))
            return NotFound(new { error = "Game not found" });

        game.MakeTurn(request.Cell);
        if (game.ReadCurrentPlayerLevel() != Game.HUMAN) game.MakeTurn(); // Сходить за бота

        Games[id] = game;

        var temp = GetGame(id);
        if (game.ReadWinner() != 0)
        {
            Games.Remove(id);
        }
        return temp;
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

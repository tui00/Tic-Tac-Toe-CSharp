using System.Net.Http.Json;
using System.Text.Json;
using TicTacToe.Dto;

namespace TicTacToe.Cli;

public class Cli : IDisposable
{
    private const int EMPTY = 0;
    private const int X = 1;
    private const int O = 2;
    private const int XO = 3;

    private enum Action
    {
        CreateGame,
        JoinToGame,
        Quit
    }

    private int _player;
    private readonly HttpClient _client;
    private Guid _gameId = Guid.Empty;

    public Cli()
    {
        _client = new();
    }

    ~Cli() => Dispose();

    public async Task<int> Start()
    {
        if (!TryConfigurate()) return 1;

        if (!await TryTestConnectionAsync()) return 1;

        try
        {
            Action userAction = GetUserAction();
            switch (userAction)
            {
                case Action.CreateGame: await CreateGameAsync(); break;
                case Action.JoinToGame: await JoinToGameAsync(); break;
                case Action.Quit: return 0;
            }
            if (_gameId == Guid.Empty)
            {
                Console.WriteLine("Выход из программы...");
                return 0;
            }
            Console.WriteLine("Начинаем игру...");
            await PlayAsync();
        }
        catch (HttpRequestException)
        {
            Console.WriteLine("Не удалось подключиться к серверу. Проверьте интернет-соединение и попробуйте снова.");
            return 1;
        }
        catch (JsonException)
        {
            Console.WriteLine("Сервер вернул не коректнный ответ.");
            return 1;
        }
        return 0;
    }

    private static Action GetUserAction()
    {
        Console.WriteLine("Привет! Введите:");
        Console.WriteLine("  (c)reate — создать игру");
        Console.WriteLine("  (j)oin  — присоединиться к игре");
        Console.WriteLine("  (q)uit  — выйти");
        while (true)
        {
            char input = Console.ReadKey(true).KeyChar;
            if (input is 'c' or 'j') Console.WriteLine("Обработка...");
            switch (input)
            {
                case 'c': return Action.CreateGame;
                case 'j': return Action.JoinToGame;
                case 'q': return Action.Quit;
                default: continue;
            }
        }
    }

    private async Task<bool> TryTestConnectionAsync()
    {
        // Тест подключения
        try
        {
            _ = await GetGamesListAsync();
        }
        catch (HttpRequestException)
        {
            return false;
        }
        return true;
    }

    private bool TryConfigurate()
    {
        string[] config;
        try
        {
            config = File.ReadAllText("config").Replace("\r", "").Split('\n');
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Файл конфигурации не найден. Убедитесь, что файл config существует в папке приложения.");
            return false;
        }
        _client.BaseAddress = new(config[0]);
        _client.Timeout = TimeSpan.FromSeconds(10);
        return true;
    }

    public async Task PlayAsync()
    {
        // Сообщить серверу о подключении
        GameResponse response = await PostAsync<GameResponse, ConnectPlayerRequest>($"game/{_gameId}/connect", new(_player));

        while (true)
        {
            // Очистка ввода
            while (Console.KeyAvailable)
                _ = Console.ReadKey(true);

            // Проверка победителя
            if (PrintGameBoardAndCheckWin(response)) break;

            string oldMessage = "";

            // POLLING
            while (response.Turn != _player || response.ConnectedPlayers != XO)
            {
                response = await _client.GetFromJsonAsync<GameResponse>($"game/{_gameId}") ?? throw new HttpRequestException();

                // Информационное сообщение        
                string message = $"Ожидание {(response.ConnectedPlayers != XO ? "подключения" : "хода")} {(_player == X ? 'O' : 'X')}...";

                if (message != oldMessage)
                {
                    Console.CursorLeft = 0;
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.CursorLeft = 0;
                    Console.Write(message);
                    oldMessage = message;
                }

                await Task.Delay(1000);
            }

            // Проверка победителя
            if (PrintGameBoardAndCheckWin(response)) break;

            // Отправка хода
            response = await PostAsync<GameResponse, MakeTurnRequest>($"game/{_gameId}", new(await GetValidInputAsync()));
        }
        Console.WriteLine($"Результат игры: {(response.Winner == X ? "X победил" : response.Winner == O ? "O победил" : "Ничья")}");
    }

    private async Task<int> GetValidInputAsync()
    {
        // Ввод клетки
        Console.WriteLine("Куда вы хотите сходить? Поле для игры это NumPad. Введите номер клетки:");
        int input;
        do
        {
            input = Console.ReadKey(true).KeyChar - '1';
            input += (input < 3) ? 6 : ((input > 5) ? -6 : 0);
        } while ((await _client.GetFromJsonAsync<IsLegalResponse>($"game/{_gameId}/isLegal/{input}"))?.IsLegal != true);
        return input;
    }

    private async Task<TOut> PostAsync<TOut, TBody>(string url, TBody body)
    {
        return await (await _client.PostAsJsonAsync(url, body)).Content.ReadFromJsonAsync<TOut>() ?? throw new HttpRequestException();
    }

    private bool PrintGameBoardAndCheckWin(GameResponse Game)
    {
        Console.Clear();
        string result = "";
        for (int y = 0; y < 9; y += 3)
        {
            char x1 = Game.Board[y + 0] == '-' ? ' ' : char.ToUpper(Game.Board[y + 0]);
            char x2 = Game.Board[y + 1] == '-' ? ' ' : char.ToUpper(Game.Board[y + 1]);
            char x3 = Game.Board[y + 2] == '-' ? ' ' : char.ToUpper(Game.Board[y + 2]);
            result += $"{x1} │ {x2} │ {x3}";

            if (y != 6) result += $"\n──┼───┼──\n";
        }
        Console.WriteLine(result);
        Console.WriteLine($"Код игры: {_gameId}");
        return Game.Winner != 0;
    }

    public async Task JoinToGameAsync()
    {
        Console.WriteLine("Какой код у игры к которой вы хотите присоединиться? Нажмите Escape что-бы выйти. Введите код:");

        string input = "";

        while (true)
        {
            Guid[] games = await GetGamesListAsync();
            Guid[] matchingGuids = [.. games.Where(guid => guid.ToString().StartsWith(input, StringComparison.OrdinalIgnoreCase))];

            if (matchingGuids.Length == 1)
            {
                GameResponse response = await _client.GetFromJsonAsync<GameResponse>($"game/{matchingGuids[0]}") ?? throw new HttpRequestException();
                if (response.ConnectedPlayers != XO)
                {
                    Console.WriteLine(matchingGuids[0].ToString()[input.Length..]);
                    _player = response.ConnectedPlayers ^ XO;
                    _gameId = matchingGuids[0];
                    return;
                }
            }

            char symbol = Console.ReadKey(true).KeyChar;
            if (!char.IsControl(symbol))
            {
                int len = input.Length;
                bool isValidSymbol = len switch
                {
                    < 36 when len is not (14 or 19 or 8 or 13 or 18 or 23) => char.IsAsciiHexDigit(symbol),
                    14 => symbol == '4',
                    19 => char.IsDigit(symbol),
                    8 or 13 or 18 or 23 => symbol == '-',
                    _ => false
                };
                if (isValidSymbol)
                {
                    Console.Write(symbol);
                    input += symbol;
                }
            }
            else if (symbol == '\b' && input.Length != 0)
            {
                Console.CursorLeft--;
                Console.Write(' ');
                Console.CursorLeft--;
                input = input[..Console.CursorLeft];
            }
            else if (symbol == '\x1b')
            {
                Console.WriteLine();
                return;
            }
        }
    }

    private async Task<Guid[]> GetGamesListAsync()
    {
        return (await _client.GetFromJsonAsync<ListGamesResponse>("game/list") ?? throw new HttpRequestException()).Ids;
    }
    private async Task<string[]> GetBotsListAsync()
    {
        return (await _client.GetFromJsonAsync<GetBotsResponse>("game/bots") ?? throw new HttpRequestException()).Names;
    }

    public async Task CreateGameAsync()
    {
        int I, Enemy;

        string[] bots = await GetBotsListAsync();

        Console.WriteLine(string.Join('\n', ["Уровни:", .. bots.Select((b, i) => $"{i}. {b}")]));

        Console.WriteLine("Введите уровень для X:");
        while ((!int.TryParse(Console.ReadLine(), out I)) || (!(I >= 0 && I < bots.Length))) ;

        Console.WriteLine("Введите уровень для O:");
        while (!int.TryParse(Console.ReadLine(), out Enemy) && !(Enemy >= 0 && Enemy < bots.Length)) ;

        NewGameResponse response = await PostAsync<NewGameResponse, NewGameRequest>("game/new", new((uint)I, (uint)Enemy));
        int player;
        if (I == 0 && Enemy == 0)
        {
            Console.WriteLine("X играет на этом устройстве? Введите (y)es или (n)o:");
            char input;
            do
            {
                input = Console.ReadKey(true).KeyChar;
            }
            while (!(input == 'y' || input == 'n') || !(Enemy >= 0 && Enemy < bots.Length));

            player = input == 'y' ? X : O;
        }
        else
        {
            if (I == 0 || Enemy == 0)
            {
                player = I == 0 ? X : O;
            }
            else
            {
                player = EMPTY;
            }
        }

        this._player = player;
        _gameId = response.Id;
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}

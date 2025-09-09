using System.Net.Http.Json;
using System.Text.Json;
using TicTacToe.Core;

namespace TicTacToe.Cli;

class Program
{
    internal static async Task<int> Main(string[] args)
    {
        using HttpClient client = new();
        string[] config = [];
        try
        {
            config = File.ReadAllText("config").Replace("\r", "").Split('\n');
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Файл конфигурации не найден. Убедитесь, что файл config существует в папке приложения.");
            return 1;
        }
        client.BaseAddress = new(config[0]);
        client.Timeout = TimeSpan.FromSeconds(10);

        Console.WriteLine("Привет! Введите (c)reate что-бы создать игру, (j)oin что-бы присоединиться, (q)uit для выхода: ");
        Guid? joinCode = null;

        try
        {
            while (joinCode == null)
            {
                char input = Console.ReadKey(true).KeyChar;
                if (input is 'c' or 'j') Console.WriteLine("Обработка...");
                switch (input)
                {
                    case 'c': joinCode = await CreateAsync(client); break;
                    case 'j': joinCode = await JoinAsync(client); break;
                    case 'q': return 0;
                    default: continue;
                }
                break;
            }
            Console.WriteLine("Начинаем игру...");
            await PlayAsync((Guid)joinCode, client);
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

    internal static async Task PlayAsync(Guid joinCode, HttpClient client)
    {
        GameResponse response = await client.GetFromJsonAsync<GameResponse>($"game/{joinCode}") ?? throw new HttpRequestException();

        int I;

        if (response.ConnectedPlayers != Game.EMPTY)
        {
            I = response.ConnectedPlayers ^ Game.XO;
        }
        else
        {
            Console.WriteLine("Вы X или O? (введите x или o): ");
            do { I = Console.ReadKey(true).KeyChar; } while (!(I == 'x' || I == 'o'));
            I = I == 'x' ? 1 : 2;
            Console.WriteLine();
        }

        // Сообщить серверу о подключении
        response = (await (await client.PostAsJsonAsync<ConnectPlayerRequest>($"game/{joinCode}/connect", new(I))).Content.ReadFromJsonAsync<GameResponse>()) ?? throw new HttpRequestException();

        // Очистка ввода
        while (Console.KeyAvailable)
            _ = Console.ReadKey(true);


        while (true)
        {
            // Проверка победителя
            if (PrintGameAndCheckWin(response, joinCode)) break;

            string oldMessage = "";

            // POLLING
            while (response.Turn != I || response.ConnectedPlayers != Game.XO)
            {
                response = await client.GetFromJsonAsync<GameResponse>($"game/{joinCode}") ?? throw new HttpRequestException("Сервер не вернул состояние игры. Проверьте подключение.");

                // Информационное сообщение        
                string message = GetInfoMessage(response.ConnectedPlayers, I);

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
            if (PrintGameAndCheckWin(response, joinCode)) break;

            // Ввод клетки
            Console.WriteLine("Введите клетку, куда вы хотите сходить, с NumPad:");
            int input;
            bool? isLegal;
            do
            {
                input = Console.ReadKey(true).KeyChar;
                input -= '1';
                input += (input < 3) ? 6 : ((input > 5) ? -6 : 0);
                isLegal = (await client.GetFromJsonAsync<IsLegalResponse>($"game/{joinCode}/isLegal/{input}"))?.IsLegal;
            } while (isLegal != true);

            // Отправка хода
            response = (await (await client.PostAsJsonAsync<MakeTurnRequest>($"game/{joinCode}", new(input))).Content.ReadFromJsonAsync<GameResponse>()) ?? throw new HttpRequestException();
        }
        Console.WriteLine($"Результат игры: {(response.Winner == Game.X ? "X победил" : response.Winner == Game.O ? "O победил" : "Ничья")}");
    }

    private static string GetInfoMessage(int player, int I) => $"Ожидание {(player != Game.XO ? "подключения" : "хода")} {(I == Game.X ? 'O' : 'X')}...";

    private static bool PrintGameAndCheckWin(GameResponse response, Guid joinCode)
    {
        Console.Clear();
        Console.WriteLine(ReadVisualBoard(response.Board));
        Console.WriteLine($"Код игры: {joinCode}");
        return response.Winner != 0;
    }


    internal static async Task<Guid> JoinAsync(HttpClient client)
    {
        Console.WriteLine("При использовании автодополнения клавиши редактирования (стрелки, Backspace, Enter и др.) будут недоступны");
        Console.WriteLine("Использовать автодополнение? (y - да, n - нет)");
        char autoComplete;
        do
        {
            autoComplete = Console.ReadKey(true).KeyChar;
        }
        while (!(autoComplete == 'n' || autoComplete == 'y'));

        if (autoComplete == 'y')
        {
            Console.WriteLine("Нажмите Backspace для очистки строки");
            Console.WriteLine("Введите код игры к которой вы хотите присоединиться (36 символов в формате \"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\")");

            string input = "";
            int oldCursorPos = Console.CursorLeft;

            while (true)
            {
                AutocompleteResult result = await AutocompleteGuidAsync(input, client);
                if (result.Success)
                {
                    Console.WriteLine(result.RemainingInput);
                    return result.Guid;
                }

                char symbol = Console.ReadKey(false).KeyChar;

                if (!char.IsControl(symbol))
                {
                    input += symbol;
                }
                else if (symbol == '\b')
                {
                    Console.CursorLeft = oldCursorPos;
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.CursorLeft = oldCursorPos;
                    input = "";
                }
            }
        }
        else
        {
            Console.WriteLine("Введите код игры к которой вы хотите присоединиться (36 символов в формате \"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\")");
            Guid guid;
            do
            {
                while (!Guid.TryParse(Console.ReadLine(), out guid)) Console.WriteLine("Введён неверный код игры. Повторите ввод: ");
                if (await GetGameIsFullAsync(guid, client)) Console.WriteLine("Игра уже началась. Повторите ввод: ");
            } while (!(await GetGamesListAsync(client)).Contains(guid));
            return guid;
        }
    }

    private static async Task<bool> GetGameIsFullAsync(Guid guid, HttpClient client)
    {
        return (await client.GetFromJsonAsync<GameResponse>($"game/{guid}") ?? throw new HttpRequestException()).ConnectedPlayers == Game.XO;
    }

    private static async Task<AutocompleteResult> AutocompleteGuidAsync(string input, HttpClient client)
    {
        Guid[] matchingGuids = [.. (await GetGamesListAsync(client)).Where(guid => guid.ToString().StartsWith(input, StringComparison.OrdinalIgnoreCase))];
        return matchingGuids.Length == 1
            ? new(true, matchingGuids[0], matchingGuids[0].ToString()[input.Length..])
            : new(false, Guid.Empty, "");
    }

    private static async Task<Guid[]> GetGamesListAsync(HttpClient client)
    {
        return (await client.GetFromJsonAsync<ListGamesResponse>("game/list") ?? throw new HttpRequestException()).Ids;
    }

    internal static async Task<Guid> CreateAsync(HttpClient client)
    {
        using HttpResponseMessage responseMessage = await client.PostAsJsonAsync<NewGameRequest>("game/new", new(0, 0));
        if (responseMessage != null && responseMessage.IsSuccessStatusCode)
        {
            NewGameResponse? response = await responseMessage.Content.ReadFromJsonAsync<NewGameResponse>();
            if (response != null)
            {
                return response.Id;
            }
        }
        throw new HttpRequestException();
    }

    internal static string ReadVisualBoard(string board)
    {
        string result = "";
        for (int y = 0; y < 9; y += 3)
        {
            char x1 = board[y + 0] == '-' ? ' ' : char.ToUpper(board[y + 0]);
            char x2 = board[y + 1] == '-' ? ' ' : char.ToUpper(board[y + 1]);
            char x3 = board[y + 2] == '-' ? ' ' : char.ToUpper(board[y + 2]);
            result += $"{x1} │ {x2} │ {x3}";

            if (y != 6) result += $"\n──┼───┼──\n";
        }
        return result;
    }
}

public record AutocompleteResult(bool Success, Guid Guid, string RemainingInput);

public record NewGameRequest(uint XLevel, uint OLevel);
public record NewGameResponse(Guid Id);

public record GameResponse(string Board, int Turn, uint Winner, int ConnectedPlayers);
public record MakeTurnRequest(int Cell);

public record ListGamesResponse(Guid[] Ids);

public record IsLegalResponse(bool IsLegal);

public record ConnectPlayerRequest(int Player);

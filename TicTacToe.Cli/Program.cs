using System.Net.Http.Json;
using System.Threading.Tasks;
using TicTacToe.Core;

namespace TicTacToe.Cli;

class Program
{
    internal static async Task Main(string[] args)
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
                Console.WriteLine("Обработка...");
                switch (input)
                {
                    case 'c': joinCode = await CreateAsync(client); break;
                    case 'j': joinCode = await JoinAsync(client); break;
                    case 'q': return;
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
            throw;
        }
        catch
        {
            throw;
        }
    }

    internal static async Task PlayAsync(Guid joinCode, HttpClient client)
    {
        GameResponse response = await client.GetFromJsonAsync<GameResponse>($"game/{joinCode}") ?? throw new HttpRequestException("Сервер не вернул состояние игры. Проверьте подключение.");

        int I;

        if (response.Board != "---------")
        {
            I = response.Turn;
        }
        else
        {
            Console.WriteLine("Вы X или O? (введите x или o): ");
            do { I = Console.ReadKey(false).KeyChar; } while (!(I == 'x' || I == 'o'));
            I = I == 'x' ? 1 : 2;
            Console.WriteLine();
        }

        while (true)
        {
            // POLLING
            while (response.Turn != I)
            {
                response = await client.GetFromJsonAsync<GameResponse>($"game/{joinCode}") ?? throw new HttpRequestException("Сервер не вернул состояние игры. Проверьте подключение.");
                await Task.Delay(1000);
            }

            // Проверка победителя
            if (PrintGameAndCheckWin(response, joinCode)) break;

            // Ввод клетки
            Console.WriteLine("Введите клетку, куда вы хотите сходить, с NumPad:");
            int input;
            do { input = Console.ReadKey(true).KeyChar; } while (input > '9' && input < '1');
            input -= '1';
            input += (input < 3) ? 6 : ((input > 5) ? -6 : 0);

            // Отправка хода
            HttpResponseMessage responseMessage = await client.PostAsJsonAsync<MakeTurnRequest>($"game/{joinCode}", new(input));
            GameResponse? tempResponse = await responseMessage.Content.ReadFromJsonAsync<GameResponse>();
            if (!responseMessage.IsSuccessStatusCode || tempResponse == null)
                throw new HttpRequestException();
            response = tempResponse;

            // Проверка победителя
            if (PrintGameAndCheckWin(response, joinCode)) break;

            // Информационное сообщение
            Console.WriteLine($"Ожиданние хода {(I == Game.X ? 'O' : 'X')}...");
        }
        Console.WriteLine($"Результат игры: {(response.Winner == Game.X ? "X победил" : response.Winner == Game.O ? "O победил" : "Ничья")}");
    }

    private static bool PrintGameAndCheckWin(GameResponse response, Guid joinCode)
    {
        Console.Clear();
        Console.WriteLine(ReadVisualBoard(response.Board));
        Console.WriteLine($"Код игры: {joinCode}");
        return response.Winner != 0;
    }


    internal static async Task<Guid> JoinAsync(HttpClient client)
    {
        Console.WriteLine("Введите код игры: ");
        Guid id;
        ListGamesResponse? response;
        do
        {
            response = await client.GetFromJsonAsync<ListGamesResponse>("game/list") ?? throw new HttpRequestException();
            while (!Guid.TryParse(Console.ReadLine(), out id)) Console.WriteLine("Введён неверный код игры. Повторите ввод: ");
        } while (!response.Ids.Contains(id));
        return id;
    }

    internal static async Task<Guid> CreateAsync(HttpClient client)
    {
        HttpResponseMessage responseMessage = await client.PostAsJsonAsync<NewGameRequest>("game/new", new(0, 0));
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

public record NewGameRequest(uint XLevel, uint OLevel);
public record NewGameResponse(Guid Id);

public record GameResponse(string Board, int Turn, uint Winner);
public record MakeTurnRequest(int Cell);

public record ListGamesResponse(Guid[] Ids);

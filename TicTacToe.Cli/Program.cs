using System.Net.Http.Json;
using System.Threading.Tasks;

namespace TicTacToe.Cli;

class Program
{
    internal static async Task Main(string[] args)
    {
        using HttpClient client = new();
        string[] config = File.ReadAllText("config").Replace("\r", "").Split('\n');
        client.BaseAddress = new(config[0]);

        Console.WriteLine("Привет! Введите (c)reate что-бы создать игру, (j)oin что-бы присоединиться, (q)uit для выхода: ");
        Guid? joinCode = null;
        while (joinCode == null)
        {
            switch (Console.ReadKey(true).KeyChar)
            {
                case 'c': joinCode = await CreateAsync(client); break;
                case 'j': joinCode = await JoinAsync(client); break;
                case 'q': return;
                default: continue;
            }
            break;
        }
        Console.WriteLine(joinCode);
    }

    internal static async Task<Guid> JoinAsync(HttpClient client)
    {
        Console.WriteLine("Введите код игры: ");
        Guid id;
        ListGamesResponse? response;
        do
        {
            response = await client.GetFromJsonAsync<ListGamesResponse>("game/list") ?? throw new HttpRequestException("Сервер не вернул список игр. Проверьте подключение."); ;
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
        throw new HttpRequestException("Не удалось создать новую игру. Сервер вернул ошибку или некорректный ответ.");
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

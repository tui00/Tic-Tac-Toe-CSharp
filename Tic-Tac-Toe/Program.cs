namespace TicTacToe;

class Program
{
    static bool NumPadMode = true;
    static readonly Stack<uint> history = new(9);

    static uint xWins, oWins, draws, initalState, games = 0;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Вы хотите режим супер-битвы(y - да):");
        if (Console.ReadLine() == "y") { await FightAll(); return; }
        Console.WriteLine("Вы хотите режим битвы(y - да):");
        if (Console.ReadLine() == "y") { await Fight(); return; }
        Console.WriteLine("q - Выход\nn - Переключить NumPad режим\nu - Отмена хода\nr - Заново\n\nСколько игр вы хотите:");
        while (!uint.TryParse(Console.ReadLine(), out games)) ;
        byte xLevel;
        byte oLevel;
        Console.WriteLine("Уровни:");
        for (int i = 0; i < TicTacToe.bots.Length; i++) Console.WriteLine($"{i}. {TicTacToe.bots[i].Item1}");
        int levels = TicTacToe.bots.Length;
        Console.WriteLine("Введите уровень X:");
        while ((!byte.TryParse(Console.ReadLine(), out xLevel)) || (xLevel > levels)) ;
        Console.WriteLine("Введите уровень O:");
        while ((!byte.TryParse(Console.ReadLine(), out oLevel)) || (oLevel > levels)) ;

        DateTime time = DateTime.Now;
        TicTacToe game = new(xLevel, oLevel);
        initalState = game.state;
        while (games >= 1)
        {
            Console.Clear();
            if ((game.ReadWinner() == 0) && (games != 0)) // Если победителя нет (0b00(Игра идет)), делаем ход
            {
                Console.WriteLine($"Ходит {(game.IsXTurn() ? "X" : "O")}. NumPud режим {(NumPadMode ? "включен" : "выключен")}. Ведите номер клетки: ");
                PrintState(game); // Вывести поле игры
            }
            else
            {
                switch (game.ReadWinner())
                {
                    case 0b00: break;
                    case 0b01: xWins++; break; // X
                    case 0b10: oWins++; break; // O
                    case 0b11: draws++; break; // XO
                }
                PrintState(game); // Вывести поле игры
                games--;
                game.state = initalState; // Очистить состояние игры
                history.Clear();
                Thread.Sleep(2000);
                continue;
            }
            history.Push(game.state); // Сохранить прошлое состояние

            if (game.ReadCurrentPlayerLevel() != TicTacToe.HUMAN) { game.MakeTurn(); Thread.Sleep(600); continue; }

            char input = Console.ReadKey(true).KeyChar;

            if (input == 'q') break; // Выйти если нажали q
            else if (input == 'u') { if (history.TryPop(out uint tempState)) game.state = tempState; } // Востоновить состояние
            else if (input == 'n') NumPadMode = !NumPadMode; // Переключить NumPad режим
            else if (input == 'r') games = 0; // начать заново

            if (game.IsLegalMove(InputOverwrite(input - '1')))
            {
                int cellInput = input - '1'; // Преобразование char в int 1-8 в одной строке (сивол - 0x31(1)), пример: 0x35 - 0x31 = 4)
                game.MakeTurn(InputOverwrite(cellInput)); // Сделать ход
            }
        }
        Console.Clear();
        PrintState(game); // Вывести поле
        Console.WriteLine($"{DateTime.Now - time}. {TicTacToe.bots[xLevel].Item1} (X) vs {TicTacToe.bots[oLevel].Item1} (O). Результат: X = {xWins}, O = {oWins}, Ничьи = {draws}");
    }
    static async Task FightAll()
    {
        uint tempGames;
        Console.WriteLine("Сколько игр вы хотите:");
        while (!uint.TryParse(Console.ReadLine(), out tempGames)) ;

        var bots = TicTacToe.bots;
        var tasks = new List<Task>();
        for (int i = 1; i < bots.Length; i++)
        {
            int tempGamesCopy = (int)tempGames;
            int iCopy = i;
            tasks.Add(Fight(iCopy, tempGamesCopy));
        }

        await Task.WhenAll(tasks);
    }
    static async Task Fight(int levelP = -1, int tempGamesP = -1)
    {
        byte level = (byte)levelP;
        var bots = TicTacToe.bots;
        if (levelP == -1)
        {
            Console.WriteLine("Уровни:");
            for (int i = 1; i < bots.Length; i++) Console.WriteLine($"{i}. {bots[i].Item1}");

            Console.WriteLine("Введите уровень:");
            while ((!byte.TryParse(Console.ReadLine(), out level)) || (level > bots.Length) || (level == 0)) ;
        }

        uint tempGames = (uint)tempGamesP;
        if (tempGamesP == -1)
        {
            Console.WriteLine("Сколько игр вы хотите:");
            while (!uint.TryParse(Console.ReadLine(), out tempGames)) ;
        }

        DateTime time = DateTime.Now;
        Console.Clear();

        var tasks = new List<Task<string>>();

        byte enemy = 2;
        while (enemy < (bots.Length * 2))
        {
            byte enemyCopy = enemy; // Копия для замыкания
            tasks.Add(Task.Run(() =>
            {
                uint xWins = 0, oWins = 0, draws = 0;
                uint games = tempGames; // Локальная копия
                TicTacToe game;
                if (enemyCopy % 2 == 0)
                {
                    game = new(level, (uint)(enemyCopy / 2));
                }
                else
                {
                    game = new((uint)(enemyCopy / 2), level);
                }
                uint initalState = game.state;

                while (games > 0)
                {
                    if (game.ReadWinner() != 0)
                    {
                        switch (game.ReadWinner())
                        {
                            case 0b01: xWins++; break;
                            case 0b10: oWins++; break;
                            case 0b11: draws++; break;
                        }
                        games--;
                        game.state = initalState;
                        continue;
                    }
                    game.MakeTurn();
                }

                return $"{DateTime.Now - time}. {TicTacToe.bots[game.ReadPlayerLevel(0b01)].Item1} (X) vs {TicTacToe.bots[game.ReadPlayerLevel(0b10)].Item1} (O). Результат: X = {xWins}, O = {oWins}, Ничьи = {draws}";
            }));
            enemy++;
        }

        var results = await Task.WhenAll(tasks);
        foreach (var result in results)
        {
            Console.WriteLine(result);
        }
    }


    static int InputOverwrite(int number) => NumPadMode ? ((2 - number / 3) * 3 + (number % 3)) : number; // Переворот ввода 

    static void PrintState(TicTacToe game)
    {
        for (int i = 0; i < 9; i++)
        {
            uint cell = game.ReadCellType(i); // Тип клетки: 0b00(Пусто), 0b01(X), 0b10(O), 0b11(Оба игрока в одной клетке(Ничья))
            Console.Write($"{(cell == 0 ? "_" : (cell == 1 ? "X" : (cell == 2 ? "O" : "-")))} ");
            if ((i + 1) % 3 == 0) Console.WriteLine();
            else Console.Write("|");
        }
    }
}

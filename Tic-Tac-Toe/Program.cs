using static TicTacToe.TicTacToe;

namespace TicTacToe;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Введите (f)ight для битвы, (s)uper fight для супер-битвы, (q)uit для выхода или (n)ormal mode для обычного режима:");
        while (true)
        {
            char input = Console.ReadKey(true).KeyChar;
            switch (input)
            {
                case 'f': await Fight(); return;
                case 's': await SuperFight(); return;
                case 'q': return;
                case 'n': NormalMode(); return;
            }
        }
    }
    static void NormalMode()
    {
        var bots = TicTacToe.bots;

        Console.WriteLine("Уровни:");
        for (int i = 1; i < bots.Length; i++) Console.WriteLine($"{i}. {bots[i].Item1}");

        uint XLevel;
        Console.WriteLine("Введите уровень для X:");
        while ((!uint.TryParse(Console.ReadLine(), out XLevel)) || (XLevel >= bots.Length)) ;
        uint OLevel;
        Console.WriteLine("Введите уровень для O:");
        while ((!uint.TryParse(Console.ReadLine(), out OLevel)) || (OLevel >= bots.Length)) ;

        DateTime time = DateTime.Now;

        uint xWins = 0, oWins = 0, draws = 0;

        uint games;
        Console.WriteLine("Введите кол-во игр (меньше 100):");
        while ((!uint.TryParse(Console.ReadLine(), out games)) || (games > 100)) ;

        TicTacToe game = new(XLevel, OLevel);

        uint initalState = game.state;
        while (games > 0)
        {
            Console.Clear();

            Console.WriteLine($"Ходит {(game.IsXTurn() ? "X" : "O")}. NumPad это поле игры. (Q)uit для выхода");
            for (int i = 0; i < 9; i++)
            {
                uint cell = game.ReadCellType(i); // Тип клетки: 0b00(Пусто), 0b01(X), 0b10(O), 0b11(Оба игрока в одной клетке(Ничья))
                Console.Write($"{(cell == EMPTY ? "_" : (cell == X ? "X" : (cell == O ? "O" : "-")))}");
                if ((i + 1) % 3 == 0) Console.WriteLine();
                else Console.Write(" | ");
            }

            if (game.ReadWinner() != EMPTY)
            {
                switch (game.ReadWinner())
                {
                    case 0b01: xWins++; break;
                    case 0b10: oWins++; break;
                    case 0b11: draws++; break;
                }
                games--;
                game.state = initalState;
                Thread.Sleep(1000);
                continue;
            }
            if (game.ReadCurrentPlayerLevel() != HUMAN)
            {
                game.MakeTurn();
            }
            else
            {
                while (true)
                {
                    char input = Console.ReadKey().KeyChar;

                    if (input == 'Q') return;

                    if (game.IsLegalMove(input - '1'))
                    {
                        game.MakeTurn(input - '1');
                        break;
                    }
                }
            }
            Thread.Sleep(500);
        }

        Console.WriteLine(GetStatistics(xWins, oWins, draws, time, game));
    }
    static async Task SuperFight()
    {
        var bots = TicTacToe.bots;
        var tasks = new List<Task>();
        for (int i = 1; i < bots.Length; i++)
        {
            int iCopy = i;
            tasks.Add(Fight(iCopy));
        }

        await Task.WhenAll(tasks);
    }
    static async Task Fight(int I = -1)
    {
        var bots = TicTacToe.bots;
        if (I == -1)
        {
            Console.WriteLine("Уровни:");
            for (int i = 1; i < bots.Length; i++) Console.WriteLine($"{i}. {bots[i].Item1}");
            Console.WriteLine("Введите уровень:");
            while ((!int.TryParse(Console.ReadLine(), out I)) || (I > bots.Length) || (I == 0)) ;
        }

        DateTime time = DateTime.Now;
        Console.Clear();

        var tasks = new List<Task<string>>();

        uint enemy = 1;
        bool swap = false;
        while (enemy < bots.Length)
        {
            uint enemyCopy = enemy;
            bool swapCopy = swap;
            tasks.Add(Task.Run(() =>
            {
                uint xWins = 0, oWins = 0, draws = 0;
                int games = 1000;

                TicTacToe game = new(swapCopy ? enemyCopy : (uint)I, swapCopy ? (uint)I : enemyCopy);

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

                return GetStatistics(xWins, oWins, draws, time, game);
            }));
            swap = !swap;
            if (!swap) enemy++;
        }

        var results = await Task.WhenAll(tasks);
        foreach (var result in results) Console.WriteLine(result);
    }

    static string GetStatistics(uint xWins, uint oWins, uint draws, DateTime startTime, TicTacToe game)
    {
        TimeSpan duration = DateTime.Now - startTime;
        string X = bots[game.ReadPlayerLevel(TicTacToe.X)].Item1;
        string O = bots[game.ReadPlayerLevel(TicTacToe.O)].Item1;
        return $"{duration.TotalSeconds:F2}. {X} (X) vs {O} (O). Результат: X = {xWins}, O = {oWins}, Ничьи = {draws}";
    }
}

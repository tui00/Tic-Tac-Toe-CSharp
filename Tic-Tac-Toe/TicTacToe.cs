namespace TicTacToe;
// lk означает Для Алисы. БЕЗ КОДА

public partial class TicTacToe
{
    public const int NEXT_TURN = 48; // Кто ходит следуший
    public const int WINNER = 49; // Кто победил
    public const int ROUND_COUNTER = 51; // Сколько раундов сделано
    public const int X_LEVEL = 54; // Уровень X
    public const int O_LEVEL = 58; // Уровень O
    public const int X = 1;
    public const int O = 2;
    public const int XO = 3;
    public const int EMPTY = 0;
    public const int HUMAN = 0;
    public const int AILevel = 1;

    public const int CENTER = 4;
    public readonly static int[] Corners = [0, 2, 6, 8];
    public readonly static int[] Edges = [1, 3, 5, 7];

    public static readonly (string, Type?)[] bots = [("Человек", null),
        ("Худший бот", typeof(WorstBot)),
        ("Случайный бот", typeof(RandomBot)),
        ("Драматург", typeof(DramatistBot)),
        ("Стандартный бот", typeof(BestBot)),
        ("X-Схема", typeof(XAndOSchemeBot)),
        ("MiniMax бот", typeof(MiniMaxBot)),
    ];

// Состояние игры "Крестики-нолики" — ulong (64 бита)
    // Биты нумеруются от 63 (старший) до 0 (младший)
    //
    // Формат: 
    //   63                                                             0
    //   HGGGGFFFFEEEEDDCBBBBBBBBBBBBBBBBAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
    //
    // Где:
    //   A (биты 0-31)  : Поле (9(16) клеток × 2 бита) — младшие биты
    //                    Клетки: 0–8(15) (слева направо, сверху вниз)
    //                      3x3: Так:
    //                           012
    //                           345
    //                           678
    //                      4x4: Так:
    //                           0,1,2,3
    //                           4,5,6,7
    //                           8,9,10,11
    //                           12,13,14,15
    //   B (биты 32-47) : CRC-16
    //   C (биты 48)    : Следующий ходит O? (1 — да, 0 — X)
    //   D (биты 49–50) : Результат
    //   E (биты 51-54) : Счётчик раундов(пара X и O) (0–8)
    //   F (биты 55-58) : Уровень X
    //   G (биты 59-62) : Уровень O
    //   H (бит 63)     : Режим 4x4
    //
    //   0b00 = пусто, 0b01 = X, 0b10 = O, 0b11 = XO
    // Уровни:
    //   0 - Человек
    //   1 - ИИ 1
    //   2 - ИИ 2
    //   3 - ИИ 3
    //   ...
    public ulong state;
    protected Stack<ulong> oldStates = new(9);

    protected Random random = new();

    protected IBot? XBot;
    protected IBot? OBot;

    public TicTacToe(int xLevel, int oLevel)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(xLevel, bots.Length, nameof(xLevel));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(oLevel, bots.Length, nameof(oLevel));
        XBot = (IBot?)((bots[xLevel].Item2 == null) ? null : Activator.CreateInstance(bots[xLevel].Item2!));
        OBot = (IBot?)((bots[oLevel].Item2 == null) ? null : Activator.CreateInstance(bots[oLevel].Item2!));
        state = (((uint)xLevel) << X_LEVEL) | (((uint)oLevel) << O_LEVEL);
    }

    // Получить победителя
    public ulong ReadWinner() => (state >> WINNER) & 0b11;
    // Кто ходит следущий
    public int ReadWhoseTurn() => X << ((int)state >> NEXT_TURN & 1);
    // Узнать тип клетки
    public int ReadCellType(int cell) => (int)((state >> (cell * 2)) & 0b11u);
    public ulong ReadBoard() => state & 0x3FFFF;
    // Получить уровень текущего игрока
    public ulong ReadPlayerLevel(int player) => (state >> (player == X ? X_LEVEL : O_LEVEL)) & 0b1111;
    public ulong ReadCurrentPlayerLevel() => ReadPlayerLevel(ReadWhoseTurn());

    public ulong ReadCurrentRound() => (state >> ROUND_COUNTER) & 0b111;
    public ulong ReadCurrentTurn() => (ReadCurrentRound() * 2) + ((ulong)ReadWhoseTurn() - 1);

    // Проверить ход на соответствие правилам
    public bool IsLegalMove(int cell) => (cell <= 8) && (cell >= 0) && (ReadCellType(cell) == 0);
    public void ValidateMove(int cell, int player) { if (!IsLegalMove(cell)) throw new InvalidTurnException(cell, player, ReadPlayerLevel(X), ReadPlayerLevel(O), state); }

    public void MakeTurn(int cell = -1)
    {
        ulong level = ReadCurrentPlayerLevel();
        if (level != HUMAN)
        {
            cell = (ReadWhoseTurn() == X ? XBot : OBot)!.GetTurn(this, random);
        }

        oldStates.Push(state);
        ApplyTurn(cell);
    }
    protected int ApplyTurn(int cell)
    {
        ValidateMove(cell, ReadWhoseTurn());
        state |= 1u << (int)((state >> NEXT_TURN) & 1) << (cell << 1); // Устоновить X или O
        state += ((state >> NEXT_TURN) & 1) << ROUND_COUNTER; // Увеличеть счетчик, если ходил нолик
        state ^= 1 << NEXT_TURN; // Сменить очередь
        return SetWinner();
    }
    public int TestTurnStart(int cell, int player)
    {
        oldStates.Push(state);
        state &= ~(ulong)(1 << NEXT_TURN);
        state |= ((player == X) ? 0u : 1u) << NEXT_TURN;
        int result = ApplyTurn(cell);
        return result;
    }
    public void TestTurnStop() => Undo();
    public int TestTurn(int cell, int player)
    {
        int result = TestTurnStart(cell, player);
        TestTurnStop();
        return result;
    }
    public void Undo() => state = oldStates.TryPop(out ulong oldState) ? oldState : state;

    public int GetBestTurn()
    {
        if (TryWinAndBlock(out int priorityCell)) return priorityCell;

        if (IsLegalMove(4)) return 4;

        int[] angles = Corners;
        random.Shuffle(angles);
        foreach (int turn in angles) if (IsLegalMove(turn)) return turn;

        int[] sides = Edges;
        random.Shuffle(sides);
        foreach (int turn in sides) if (IsLegalMove(turn)) return turn;

        int suitableCell = -1;
        while (!IsLegalMove(++suitableCell)) if (suitableCell >= 9) break; ;
        return suitableCell;
    }
    public int GetWorstTurn()
    {
        int[] sides = Edges;
        random.Shuffle(sides);
        foreach (int turn in sides) if (IsLegalMove(turn)) return turn;

        int[] angles = Corners;
        random.Shuffle(angles);
        foreach (int turn in angles) if (IsLegalMove(turn)) return turn;

        if (IsLegalMove(4)) return 4;

        int suitableCell = -1;
        while (!IsLegalMove(++suitableCell)) ;
        return suitableCell;
    }

    public bool TryWinAndBlock(out int cell)
    {
        int I = ReadWhoseTurn();
        int Enemy = ReadWhoseTurn() ^ XO;
        if (TryFindWinningMove(I, out int atackCell)) { cell = atackCell; return true; }
        if (TryFindWinningMove(Enemy, out int blockCell)) { cell = blockCell; return true; }
        cell = -1;
        return false;
    }
    public bool TryFindWinningMove(int player, out int cell)
    {
        for (int i = 0; i < 9; i++)
        {
            if (!IsLegalMove(i)) continue;
            if (TestTurn(i, player) == player) { cell = i; return true; }
        }
        cell = -1;
        return false;
    }
    public int FindWinningMoves(int player)
    {
        int counter = 0;
        for (int i = 0; i < 9; i++)
        {
            if (!IsLegalMove(i)) continue;
            if (TestTurn(i, player) == player) counter++;
        }
        return counter;
    }

    protected int SetWinner()
    {
        ulong winner = 0;
        foreach (ulong mask in new ulong[] { 0x3F, 0xFC0, 0x3F000, 0x30C3, 0xC30C, 0x30C30, 0x30303, 0x3330, }) // Перебор победных комбинаций
        {
            if ((state & mask & 0x15555) == (mask & 0x15555)) winner |= 1; // Среди крестиков
            if ((state & mask & 0x2AAAA) == (mask & 0x2AAAA)) winner |= 2; // и ноликов
        }
        if (winner == 0 && ReadCurrentTurn() == 9) winner = XO; // Ничья

        state |= winner << WINNER;
        return (int)winner;
    }
}

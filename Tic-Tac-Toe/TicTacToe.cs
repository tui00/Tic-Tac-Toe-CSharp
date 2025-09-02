namespace TicTacToe;
// lk означает Для Алисы. БЕЗ КОДА

public partial class TicTacToe
{
    public const int NEXT_TURN = 18; // Кто ходит следуший
    public const int WINNER = 19; // Кто победил
    public const int ROUND_COUNTER = 21; // Сколько раундов сделано
    public const int X_LEVEL = 24; // Уровень X
    public const int O_LEVEL = 28; // Уровень O
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

    // Состояние игры "Крестики-нолики" — uint (32 бита)
    // Биты нумеруются от 31 (старший) до 0 (младший)
    //
    // Формат: 
    //   31                                       0
    //   FFFF  EEEE  DDD  CC  B  AAAAAAAAAAAAAAAAAA
    //
    // Где:
    //   A (биты 0–17)  : Поле (9 клеток × 2 бита) — младшие биты
    //                    Клетки: 0–8 (слева направо, сверху вниз)
    //                    Так:
    //                    012
    //                    345
    //                    678
    //   B (бит 18)     : Следующий ходит O? (1 — да, 0 — X)
    //   C (биты 19–20) : Результат
    //   D (биты 21–23) : Счётчик раундов(пара X и O, на последнем раунде только X) (0–5)
    //   E (биты 24-27) : Уровень X
    //   F (биты 28-31) : Уровень O
    //
    //   0b00 = пусто, 0b01 = X, 0b10 = O, 0b11 = XO
    // Уровни:
    //   0 - Человек
    //   1 - ИИ 1
    //   2 - ИИ 2
    //   3 - ИИ 3
    //   ...
    public uint state;
    protected Stack<uint> oldStates = new(9);

    protected Random random = new();

    protected IBot? XBot;
    protected IBot? OBot;

    public TicTacToe(uint xLevel, uint oLevel)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(xLevel, (uint)bots.Length, nameof(xLevel));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(oLevel, (uint)bots.Length, nameof(oLevel));
        XBot = (IBot?)((bots[xLevel].Item2 == null) ? null : Activator.CreateInstance(bots[xLevel].Item2!));
        OBot = (IBot?)((bots[oLevel].Item2 == null) ? null : Activator.CreateInstance(bots[oLevel].Item2!));
        state = (xLevel << X_LEVEL) | (oLevel << O_LEVEL);
    }

    // Получить победителя
    public uint ReadWinner() => (state >> WINNER) & 0b11;
    // Следущий ходит X
    public bool IsXTurn() => ((state >> NEXT_TURN) & 1) == 0;
    // Узнать тип клетки
    public uint ReadCellType(int cell) => (state >> (cell * 2)) & 0b11u;
    public uint ReadBoard() => state & 0x3FFFF;
    // Получить уровень текущего игрока
    public uint ReadPlayerLevel(int player) => (state >> (player == X ? X_LEVEL : O_LEVEL)) & 0b1111;
    public uint ReadCurrentPlayerLevel() => ReadPlayerLevel(IsXTurn() ? X : O);

    public uint ReadCurrentRound() => (state >> ROUND_COUNTER) & 0b111;
    public uint ReadCurrentTurn() => (ReadCurrentRound() * 2) + (IsXTurn() ? 0u : 1u);

    // Проверить ход на соответствие правилам
    public bool IsLegalMove(int cell) => (cell <= 8) && (cell >= 0) && (ReadCellType(cell) == 0);
    public void ValidateMove(int cell, int player) { if (!IsLegalMove(cell)) throw new InvalidTurnException(cell, player, ReadPlayerLevel(X), ReadPlayerLevel(O), state); }

    public void MakeTurn(int cell = -1)
    {
        uint level = ReadCurrentPlayerLevel();
        if (level != HUMAN)
        {
            cell = (IsXTurn() ? XBot : OBot)!.GetTurn(this, random);
        }

        oldStates.Push(state);
        ApplyTurn(cell);
    }
    protected uint ApplyTurn(int cell)
    {
        ValidateMove(cell, IsXTurn() ? X : O);
        state |= 1u << (int)((state >> NEXT_TURN) & 1) << (cell << 1); // Устоновить X или O
        state += ((state >> NEXT_TURN) & 1) << ROUND_COUNTER; // Увеличеть счетчик, если ходил нолик
        state ^= 1 << NEXT_TURN; // Сменить очередь
        return SetWinner();
    }
    public uint TestTurnStart(int cell, int player)
    {
        oldStates.Push(state);
        state &= ~(uint)(1 << NEXT_TURN);
        state |= (uint)(((player == X) ? 0 : 1) << NEXT_TURN);
        uint result = ApplyTurn(cell);
        return result;
    }
    public void TestTurnStop() => Undo();
    public uint TestTurn(int cell, int player)
    {
        uint result = TestTurnStart(cell, player);
        TestTurnStop();
        return result;
    }
    public void Undo() => state = oldStates.TryPop(out uint oldState) ? oldState : state;

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
        int I = IsXTurn() ? X : O;
        int Enemy = IsXTurn() ? O : X;
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

    protected uint SetWinner()
    {
        uint winner = 0;
        foreach (uint mask in new uint[] { 0x3F, 0xFC0, 0x3F000, 0x30C3, 0xC30C, 0x30C30, 0x30303, 0x3330, }) // Перебор победных комбинаций
        {
            if ((state & mask & 0x15555) == (mask & 0x15555)) winner |= 1; // Среди крестиков
            if ((state & mask & 0x2AAAA) == (mask & 0x2AAAA)) winner |= 2; // и ноликов
        }
        if (winner == 0 && ReadCurrentTurn() == 9) winner = XO; // Ничья

        state |= winner << WINNER;
        return winner;
    }
}

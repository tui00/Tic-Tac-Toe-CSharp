using static TicTacToe.TicTacToe;

namespace TicTacToe;

public class DramatistBot : IBot
{
    // 🎭 Акты пьесы
    private const int EXPOSITION = 0; // 1–2 ход: иллюзия контроля
    private const int CONFLICT   = 1; // 3–4 ход: напряжение
    private const int CLIMAX     = 2; // 5+ ход: кульминация

    public int GetTurn(TicTacToe game, Random random)
    {
        // 🛑 1. ВСЕГДА СНАЧАЛА — победа или блокировка
        if (game.TryWinAndBlock(out int priorityCell))
            return HandlePriorityMove(game, priorityCell, random);

        // 🎬 2. ИГРАЕМ ПО СЦЕНАРИЮ
        return PlayAct(game, random);
    }

    private int HandlePriorityMove(TicTacToe game, int cell, Random random)
    {
        uint turn = game.ReadCurrentTurn();

        // В экспозиции — иногда пропускаем выгодный ход ради драмы (30%)
        if (GetAct(turn) == EXPOSITION && random.Next(100) < 30)
        {
            int fallback = game.GetBestTurn();
            return fallback; // Всё равно делаем *хороший*, но не *очевидный* ход
        }

        // В конфликте и кульминации — побеждаем. Это не обсуждается.
        return cell;
    }

    private int PlayAct(TicTacToe game, Random random)
    {
        uint turn = game.ReadCurrentTurn();
        int player = game.ReadWhoseTurn();
        int opponent = game.ReadWhoseTurn() ^ XO;

        return GetAct(turn) switch
        {
            EXPOSITION => ExpositionStrategy(game, random),
            CONFLICT   => ConflictStrategy(game, random, player, opponent),
            CLIMAX     => ClimaxStrategy(game, player, random),
            _          => game.GetBestTurn()
        };
    }

    private static int GetAct(uint turn) => turn switch
    {
        <= 2 => EXPOSITION,
        <= 4 => CONFLICT,
        _    => CLIMAX
    };

    // 🎭 Акт 1: Экспозиция — «Ты герой. Пока что.»
    private static int ExpositionStrategy(TicTacToe game, Random random)
    {
        // В 70% — уступаем центральному напору
        if (game.IsLegalMove(CENTER) && random.Next(100) < 70)
            return random.Next(100) < 50 
                ? PickRandomCorner(game, random) 
                : PickRandomEdge(game, random);

        // Иначе — делаем ход в угол (но не в центр!)
        return PickRandomCorner(game, random);
    }

    // ⚔️ Акт 2: Конфликт — «Ты думал, что контролируешь?»
    private static int ConflictStrategy(TicTacToe game, Random random, int player, int opponent)
    {
        // Если мы в центре — контратакуем по диагонали
        if (game.ReadCellType(CENTER) == player)
        {
            int counter = FindCounterCorner(game, opponent);
            if (counter != -1) return counter;
        }

        // Или просто занимаем угол
        int corner = PickRandomCorner(game, random);
        return corner != -1 ? corner : game.GetBestTurn();
    }

    // 🔥 Акт 3: Кульминация — «Моя победа. Мой театр.»
    private static int ClimaxStrategy(TicTacToe game, int player, Random random)
    {
        // В 40% — делаем неочевидный, но безопасный ход
        if (random.Next(100) < 40)
            return game.GetBestTurn();

        // В 60% — просто побеждаем, как положено
        return game.GetBestTurn();
    }

    // 🎲 Случайный угол
    private static int PickRandomCorner(TicTacToe game, Random random)
    {
        var corners = Corners;
        random.Shuffle(corners);
        return corners.FirstOrDefault(game.IsLegalMove, game.GetBestTurn());
    }

    // 🎲 Случайное ребро
    private static int PickRandomEdge(TicTacToe game, Random random)
    {
        var edges = Edges;
        random.Shuffle(edges);
        return edges.FirstOrDefault(game.IsLegalMove, game.GetBestTurn());
    }

    // 🔮 Противоположный угол от последнего хода противника
    private static int FindCounterCorner(TicTacToe game, int opponent)
    {
        var opposite = new Dictionary<int, int> { { 0, 8 }, { 2, 6 }, { 6, 2 }, { 8, 0 } };
        for (int cell = 0; cell < 9; cell++)
        {
            if (game.ReadCellType(cell) == opponent && 
                opposite.TryGetValue(cell, out int counter) && 
                game.IsLegalMove(counter))
                return counter;
        }
        return game.GetBestTurn(); // fallback — лучший ход
    }
}

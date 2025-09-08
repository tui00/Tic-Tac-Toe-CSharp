namespace TicTacToe.Cli;

class Program
{
    internal static void Main(string[] args)
    {
        Console.WriteLine(ReadVisualBoard("xo-xo-x--"));
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
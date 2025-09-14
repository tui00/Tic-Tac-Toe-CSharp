namespace TicTacToe.Cli;

class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await new Cli().Start();
    }
}

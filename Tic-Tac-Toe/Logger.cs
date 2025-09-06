namespace TicTacToe;

public class Logger
{
    public string log = "";
    public void Log(string log) => this.log += log.Replace("\r", "") + "\n";
}

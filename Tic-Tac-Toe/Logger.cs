namespace TicTacToe;

public class Logger
{
    public string log = "";
    public void AddColumn(string column) => log = log.FormatToTable(column);
    public void Log(string log) => this.log += log;
}

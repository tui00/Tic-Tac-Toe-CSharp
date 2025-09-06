namespace TicTacToe;

public class Logger
{
    public string log = "";
    public void AddColumn(string column) => log = log.FormatToTable(column.Replace("\r", ""));
    public void Log(string log) => this.log += log.Replace("\r", "") + "\n";
}

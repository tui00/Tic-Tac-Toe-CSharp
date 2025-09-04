namespace TicTacToe;

internal static class StringArrayExtensions
{
    public static string FormatToTable(this string[] columns, string separator = "   ")
    {
        string[][] columnsLines = [.. columns.Select(column => column.Split('\n'))];
        int[] columnsWidths = [.. columnsLines.Select(lines => lines.Max(line => line.Length))];
        int linesCount = columnsLines.Max(lines => lines.Length);

        List<string> resultLines = [];
        for (int i = 0; i < linesCount; i++)
        {
            resultLines.Add(string.Join(separator,
                columnsLines.Select((columnLines, column) =>
                    {
                        string cell = i < columnLines.Length ? columnLines[i] : "";
                        return cell.PadRight(columnsWidths[column], ' ');
                    }
                )
            ));
        }

        return string.Join("\n", resultLines);
    }

    public static string FormatToTable(this string leftText, string rightText, string separator = "   ") => (new string[] { leftText, rightText }).FormatToTable(separator);
}

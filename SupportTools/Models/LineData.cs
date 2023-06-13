namespace SupportTools.Models;

public sealed class LineData
{
    public LineData(string line, string trimLine, string dryLine, int indent, bool isEndOfFile)
    {
        Line = line;
        TrimLine = trimLine;
        DryLine = dryLine;
        Indent = indent;
        IsEndOfFile = isEndOfFile;
    }

    public string Line { get; set; }
    public string TrimLine { get; set; }
    public string DryLine { get; set; }
    public int Indent { get; set; }
    public bool IsEndOfFile { get; set; }

    public static LineData CreateEndOfFile()
    {
        return new LineData("", "", "", 0, true);
    }

    public static LineData Create(string line)
    {
        var indent = CountIndent(line);
        var trimLine = line.Trim();
        var dryLine = trimLine.Replace(" ", "");
        return new LineData(line, trimLine, dryLine, indent, false);
    }

    private static int CountIndent(string line)
    {
        var indent = 0;
        foreach (var c in line)
            if (c == ' ')
                indent++;
            else
                break;

        return indent / 4;
    }
}
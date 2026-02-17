namespace SupportTools.Models;

public sealed class LineData
{
    // ReSharper disable once ConvertToPrimaryConstructor
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
        return new LineData(string.Empty, string.Empty, string.Empty, 0, true);
    }

    public static LineData Create(string line)
    {
        int indent = CountIndent(line);
        string trimLine = line.Trim();
        string dryLine = trimLine.Replace(" ", string.Empty);
        return new LineData(line, trimLine, dryLine, indent, false);
    }

    private static int CountIndent(string line)
    {
        int indent = 0;
        foreach (char c in line)
        {
            if (c == ' ')
            {
                indent++;
            }
            else
            {
                break;
            }
        }

        return indent / 4;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LibGitWork.SyncV3;

//პარალელური სამუშაოების ჩვენება dotnet build-ის მსგავსად: კონსოლის ბოლოში სათაური და თითო სტრიქონი თითო მიმდინარე
//სამუშაოსთვის, რომელიც თავის ადგილზე ახლდება; დასრულებული შედეგები (WriteLine) დაფის ზემოთ ჩვეულებრივ სტრიქონებად
//რჩება. დაფის სტრიქონი ფანჯრის სიგანეზე იჭრება, რომ ერთ ხაზზე დარჩეს: ასე დაფის დასაწყისი კურსორის მიმდინარე
//მდებარეობიდან ითვლება, ეკრანი რომც გადაიწიოს. console=null (გადამისამართებული გამოტანა, ფაილი, ტესტები): დაფა არ
//იხატება, მხოლოდ WriteLine-ის სტრიქონები იწერება
internal sealed class ParallelProgressBoard : IDisposable
{
    private const string Ellipsis = "...";

    private readonly IProgressConsole? _console;
    private readonly List<ProgressBoardLine> _lines = [];
    private readonly Lock _lock = new();
    private string? _header;
    private int _renderedLineCount;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ParallelProgressBoard(IProgressConsole? console)
    {
        _console = console;
        _console?.SetCursorVisible(false);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_console is null)
            {
                return;
            }

            _header = null;
            _lines.Clear();
            Redraw();
            _console.SetCursorVisible(true);
        }
    }

    public void SetHeader(string header)
    {
        lock (_lock)
        {
            if (_header == header)
            {
                return;
            }

            _header = header;
            Redraw();
        }
    }

    public ProgressBoardLine StartLine(string text)
    {
        lock (_lock)
        {
            var line = new ProgressBoardLine(this, text);
            _lines.Add(line);
            Redraw();
            return line;
        }
    }

    //სამუდამო სტრიქონი დაფის ზემოთ
    public void WriteLine(string text, ConsoleColor? color = null)
    {
        lock (_lock)
        {
            if (_console is null)
            {
                Console.WriteLine(text);
                return;
            }

            ClearRenderedLines();
            _console.Write(text, color);
            _console.Write(Environment.NewLine, null);
            Redraw();
        }
    }

    internal void UpdateLine(ProgressBoardLine line, string text)
    {
        lock (_lock)
        {
            if (line.Text == text)
            {
                return;
            }

            line.Text = text;
            Redraw();
        }
    }

    internal void RemoveLine(ProgressBoardLine line)
    {
        lock (_lock)
        {
            if (_lines.Remove(line))
            {
                Redraw();
            }
        }
    }

    //ტექსტი ზუსტად width სიმბოლოა: გრძელი იჭრება, მოკლე ჰარებით ივსება, რომ ძველი ტექსტის ნაშთი წაიშალოს.
    //მართვის სიმბოლოები (ახალი ხაზი, ტაბი) ჰარით იცვლება, რომ სტრიქონი ერთ ხაზზე დარჩეს
    internal static string Fit(string text, int width)
    {
        string singleLine = new([.. text.Select(c => char.IsControl(c) ? ' ' : c)]);
        if (singleLine.Length <= width)
        {
            return singleLine.PadRight(width);
        }

        return width <= Ellipsis.Length ? singleLine[..width] : $"{singleLine[..(width - Ellipsis.Length)]}{Ellipsis}";
    }

    private int LineWidth()
    {
        //ბოლო სვეტში ჩაწერილი სიმბოლო ზოგ კონსოლში კურსორს შემდეგ ხაზზე გადაიტანდა
        return Math.Max(1, _console!.Width - 1);
    }

    private void Redraw()
    {
        if (_console is null)
        {
            return;
        }

        int width = LineWidth();
        _console.SetCursorPosition(0, Math.Max(0, _console.CursorTop - _renderedLineCount));
        List<string> texts = [.. _lines.Select(x => x.Text)];
        if (_header is not null)
        {
            texts.Insert(0, _header);
        }

        foreach (string text in texts)
        {
            _console.Write(Fit(text, width) + Environment.NewLine, null);
        }

        //წინა დაფის ზედმეტი სტრიქონები იშლება, კურსორი კი ახალი დაფის ბოლოს ბრუნდება
        int extraLineCount = _renderedLineCount - texts.Count;
        for (int i = 0; i < extraLineCount; i++)
        {
            _console.Write(new string(' ', width) + Environment.NewLine, null);
        }

        if (extraLineCount > 0)
        {
            _console.SetCursorPosition(0, Math.Max(0, _console.CursorTop - extraLineCount));
        }

        _renderedLineCount = texts.Count;
    }

    private void ClearRenderedLines()
    {
        if (_console is null || _renderedLineCount == 0)
        {
            return;
        }

        int width = LineWidth();
        int top = Math.Max(0, _console.CursorTop - _renderedLineCount);
        _console.SetCursorPosition(0, top);
        for (int i = 0; i < _renderedLineCount; i++)
        {
            _console.Write(new string(' ', width) + Environment.NewLine, null);
        }

        _console.SetCursorPosition(0, top);
        _renderedLineCount = 0;
    }
}

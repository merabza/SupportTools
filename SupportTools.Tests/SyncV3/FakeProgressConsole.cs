using System;
using System.Collections.Generic;
using System.Linq;
using LibGitWork.SyncV3;

namespace SupportTools.Tests.SyncV3;

//a console screen of a fixed size: a new line at the last row scrolls the screen up, a character in the last column
//moves the cursor to the next row, like the real console does
internal sealed class FakeProgressConsole : IProgressConsole
{
    private readonly int _height;
    private readonly List<char[]> _rows;
    private int _column;

    public FakeProgressConsole(int width, int height)
    {
        Width = width;
        _height = height;
        _rows = [.. Enumerable.Range(0, height).Select(_ => NewRow())];
    }

    public bool CursorVisible { get; private set; } = true;
    public int ScrolledRowCount { get; private set; }

    //every Write with its color, in order, so a test can see the board lines that were drawn and erased again
    public List<(string Text, ConsoleColor? Color)> Writes { get; } = [];

    //the screen rows without the trailing spaces and without the empty rows at the end
    public List<string> Lines
    {
        get
        {
            List<string> lines = [.. _rows.Select(x => new string(x).TrimEnd())];
            while (lines.Count > 0 && lines[^1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return lines;
        }
    }

    public int Width { get; }

    public int CursorTop { get; private set; }

    public void SetCursorPosition(int left, int top)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(left);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(left, Width);
        ArgumentOutOfRangeException.ThrowIfNegative(top);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(top, _height);
        _column = left;
        CursorTop = top;
    }

    public void Write(string text, ConsoleColor? color)
    {
        Writes.Add((text, color));
        foreach (char c in text)
        {
            switch (c)
            {
                case '\r':
                    _column = 0;
                    break;
                case '\n':
                    NextRow();
                    break;
                default:
                    _rows[CursorTop][_column] = c;
                    _column++;
                    if (_column == Width)
                    {
                        NextRow();
                    }

                    break;
            }
        }
    }

    public void SetCursorVisible(bool visible)
    {
        CursorVisible = visible;
    }

    public void WriteLine(string text)
    {
        Write(text + Environment.NewLine, null);
    }

    private void NextRow()
    {
        _column = 0;
        if (CursorTop < _height - 1)
        {
            CursorTop++;
            return;
        }

        _rows.RemoveAt(0);
        _rows.Add(NewRow());
        ScrolledRowCount++;
    }

    private char[] NewRow()
    {
        return [.. Enumerable.Repeat(' ', Width)];
    }
}

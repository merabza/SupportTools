using System;

namespace LibGitWork.SyncV3;

//დაფის ერთი სტრიქონი: SetText მას ადგილზე ცვლის, Dispose დაფიდან აშორებს
internal sealed class ProgressBoardLine : IDisposable
{
    private readonly ParallelProgressBoard _board;

    // ReSharper disable once ConvertToPrimaryConstructor
    internal ProgressBoardLine(ParallelProgressBoard board, string text)
    {
        _board = board;
        Text = text;
    }

    public string Text { get; internal set; }

    public void Dispose()
    {
        _board.RemoveLine(this);
    }

    public void SetText(string text)
    {
        _board.UpdateLine(this, text);
    }
}

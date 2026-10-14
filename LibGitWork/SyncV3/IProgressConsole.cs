using System;

namespace LibGitWork.SyncV3;

//კონსოლის ის ოპერაციები, რომლებიც პარალელური სამუშაოების დაფას სჭირდება (ტესტებში ეკრანის იმიტაციაა)
internal interface IProgressConsole
{
    int Width { get; }
    int CursorTop { get; }
    void SetCursorPosition(int left, int top);
    void Write(string text, ConsoleColor? color);
    void SetCursorVisible(bool visible);
}

using System;
using System.IO;

namespace LibGitWork.SyncV3;

internal sealed class SystemProgressConsole : IProgressConsole
{
    public int Width => Console.WindowWidth;

    public int CursorTop => Console.CursorTop;

    public void SetCursorPosition(int left, int top)
    {
        Console.SetCursorPosition(left, top);
    }

    public void Write(string text, ConsoleColor? color)
    {
        if (color is null)
        {
            Console.Write(text);
            return;
        }

        ConsoleColor existingColor = Console.ForegroundColor;
        Console.ForegroundColor = color.Value;
        Console.Write(text);
        Console.ForegroundColor = existingColor;
    }

    //კურსორის დამალვა მხოლოდ გარეგნობაა: სადაც კონსოლი მას ვერ მართავს, დაფა კურსორით იხატება
    public void SetCursorVisible(bool visible)
    {
        try
        {
            Console.CursorVisible = visible;
        }
        catch (IOException)
        {
            //კონსოლი კურსორის ხილვადობას ვერ ცვლის
        }
        catch (PlatformNotSupportedException)
        {
            //ამ პლატფორმაზე კურსორის ხილვადობა არ იმართება
        }
    }
}

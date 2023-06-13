namespace LibAppProjectCreator;

public static class StSt
{
    public static string ToNormalClassName(this string source)
    {
        return source.Replace(".", "");
    }
}
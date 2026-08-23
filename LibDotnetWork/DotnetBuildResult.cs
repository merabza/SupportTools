namespace LibDotnetWork;

//dotnet build-ის შედეგი: დასრულდა თუ არა წარმატებით (exit code) და MSBuild-ის შეჯამებიდან წაკითხული
//შეცდომებისა და გაფრთხილებების რაოდენობა
public sealed class DotnetBuildResult
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public DotnetBuildResult(bool succeeded, int errorCount, int warningCount)
    {
        Succeeded = succeeded;
        ErrorCount = errorCount;
        WarningCount = warningCount;
    }

    public bool Succeeded { get; }
    public int ErrorCount { get; }
    public int WarningCount { get; }
}

using SupportToolsData;

namespace SupportTools.Menu;

//ერთი პროექტის "Check ... build"-ის შედეგი: სტატუსი და, თუ build რეალურად გაეშვა,
//dotnet build-ის მიერ დათვლილი შეცდომებისა და გაფრთხილებების რაოდენობა
public sealed class ProjectBuildCheckResult
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectBuildCheckResult(EProjectBuildCheckStatus status, int errorCount = 0, int warningCount = 0)
    {
        Status = status;
        ErrorCount = errorCount;
        WarningCount = warningCount;
    }

    public EProjectBuildCheckStatus Status { get; }
    public int ErrorCount { get; }
    public int WarningCount { get; }

    //build გაეშვა და წარმატებით დასრულდა (გაფრთხილებების გარეშე ან გაფრთხილებებით)
    public bool IsBuildSucceeded => Status is EProjectBuildCheckStatus.Success
        or EProjectBuildCheckStatus.SuccessWithWarnings;

    //build საერთოდ გაეშვა - მხოლოდ ამ სტატუსებისთვის აქვს რაოდენობებს აზრი
    public bool IsBuildExecuted => IsBuildSucceeded || Status == EProjectBuildCheckStatus.BuildFailed;
}

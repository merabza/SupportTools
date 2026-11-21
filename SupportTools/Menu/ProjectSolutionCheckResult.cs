using System.Collections.Generic;
using SupportToolsData;

namespace SupportTools.Menu;

//ერთი პროექტის "Check Solutions"-ის შედეგი: სტატუსი და მისი მიზეზები კონსოლისთვის
//(Needs Distribute Packages-ისთვის - დასაბრუნებელი პაკეტები, Needs attention-ისთვის - პრობლემები)
public sealed class ProjectSolutionCheckResult
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectSolutionCheckResult(EProjectSolutionCheckStatus status, IReadOnlyList<string> details)
    {
        Status = status;
        Details = details;
    }

    public EProjectSolutionCheckStatus Status { get; }
    public IReadOnlyList<string> Details { get; }
}

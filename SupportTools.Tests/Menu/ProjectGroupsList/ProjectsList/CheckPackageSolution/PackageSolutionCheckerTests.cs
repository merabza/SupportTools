using System.Xml.Linq;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckPackageSolution;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.CheckPackageSolution;

public sealed class PackageSolutionCheckerTests
{
    [Theory]
    [InlineData("<configuration><disabledPackageSources><add key=\"BaGetter\" value=\"true\" /></disabledPackageSources></configuration>", true)]
    [InlineData("<configuration><disabledPackageSources><add key=\"bagetter\" value=\"True\" /></disabledPackageSources></configuration>", true)]
    [InlineData("<configuration><disabledPackageSources><add key=\"BaGetter\" value=\"false\" /></disabledPackageSources></configuration>", false)]
    [InlineData("<configuration><disabledPackageSources><add key=\"nuget.org\" value=\"true\" /></disabledPackageSources></configuration>", false)]
    [InlineData("<configuration><disabledPackageSources /></configuration>", false)]
    [InlineData("<configuration />", false)]
    public void IsPackageSourceDisabled_ReadsDisabledPackageSources(string configXml, bool expected)
    {
        XElement configurationElement = XElement.Parse(configXml);

        Assert.Equal(expected, PackageSolutionChecker.IsPackageSourceDisabled(configurationElement, "BaGetter"));
    }
}

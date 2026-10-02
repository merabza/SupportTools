using System.IO;
using LibSupportToolsServerWork.Registry.Paths;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Paths;

public sealed class PathMapperTests
{
    private const char WindowsSeparator = '\\';
    private const char LinuxSeparator = '/';
    private const string CanonicalWorkFolder = @"D:\1WorkDotnet";
    private const string LinuxWorkFolder = "/home/u/1WorkDotnet";
    private const string CanonicalSolution = @"D:\1WorkDotnet\X\Y.slnx";
    private const string LinuxSolution = "/home/u/1WorkDotnet/X/Y.slnx";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToLocal_WhenPathIsNullOrEmpty_ReturnsItUnchangedWithoutIssue(string? path)
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.ToLocal(path);

        // Assert
        Assert.Equal(path, result);
        Assert.Empty(sut.Issues);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToCanonical_WhenPathIsNullOrEmpty_ReturnsItUnchangedWithoutIssue(string? path)
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.ToCanonical(path);

        // Assert
        Assert.Equal(path, result);
        Assert.Empty(sut.Issues);
    }

    //a Windows computer with the layout of the main computer needs no rules: nothing changes
    [Fact]
    public void ToLocal_WhenWindowsHasNoRules_ReturnsPathUnchangedWithoutIssue()
    {
        // Arrange
        PathMapper sut = CreateWindowsSut();

        // Act
        string? result = sut.ToLocal(CanonicalSolution);

        // Assert
        Assert.Equal(CanonicalSolution, result);
        Assert.Empty(sut.Issues);
    }

    [Fact]
    public void ToCanonical_WhenWindowsHasNoRules_ReturnsPathUnchangedWithoutIssue()
    {
        // Arrange
        PathMapper sut = CreateWindowsSut();

        // Act
        string? result = sut.ToCanonical(CanonicalSolution);

        // Assert
        Assert.Equal(CanonicalSolution, result);
        Assert.Empty(sut.Issues);
    }

    [Fact]
    public void ToLocal_WhenWindowsRuleMapsToOtherDrive_ReplacesPrefixAndKeepsBackslashes()
    {
        // Arrange
        PathMapper sut = CreateWindowsSut(Rule(CanonicalWorkFolder, @"E:\Work\1WorkDotnet"));

        // Act
        string? result = sut.ToLocal(CanonicalSolution);

        // Assert
        Assert.Equal(@"E:\Work\1WorkDotnet\X\Y.slnx", result);
    }

    [Fact]
    public void ToCanonical_WhenWindowsRuleMapsToOtherDrive_RestoresCanonicalPrefix()
    {
        // Arrange
        PathMapper sut = CreateWindowsSut(Rule(CanonicalWorkFolder, @"E:\Work\1WorkDotnet"));

        // Act
        string? result = sut.ToCanonical(@"E:\Work\1WorkDotnet\X\Y.slnx");

        // Assert
        Assert.Equal(CanonicalSolution, result);
    }

    //on Windows the rest of the path keeps its separators, even forward slashes
    [Fact]
    public void ToLocal_WhenWindowsPathHasForwardSlashes_KeepsThemInRestOfPath()
    {
        // Arrange
        PathMapper sut = CreateWindowsSut(Rule(CanonicalWorkFolder, @"E:\Work"));

        // Act
        string? result = sut.ToLocal("D:/1WorkDotnet/X/Y.slnx");

        // Assert
        Assert.Equal(@"E:\Work/X/Y.slnx", result);
    }

    [Fact]
    public void ToLocal_WhenLinux_ReplacesPrefixAndSeparators()
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.ToLocal(CanonicalSolution);

        // Assert
        Assert.Equal(LinuxSolution, result);
        Assert.Empty(sut.Issues);
    }

    [Fact]
    public void ToCanonical_WhenLinux_RestoresCanonicalPrefixAndBackslashes()
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.ToCanonical(LinuxSolution);

        // Assert
        Assert.Equal(CanonicalSolution, result);
        Assert.Empty(sut.Issues);
    }

    //project folders are often stored with a trailing separator; a round trip must give back the same text
    [Fact]
    public void ToLocalThenToCanonical_WhenLinuxPathEndsWithSeparator_KeepsTrailingSeparator()
    {
        // Arrange
        const string canonicalProjectFolder = @"D:\1WorkDotnet\VerbsGrammarGe\";
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? local = sut.ToLocal(canonicalProjectFolder);
        string? canonical = sut.ToCanonical(local);

        // Assert
        Assert.Equal("/home/u/1WorkDotnet/VerbsGrammarGe/", local);
        Assert.Equal(canonicalProjectFolder, canonical);
    }

    [Fact]
    public void ToLocal_WhenPathEqualsCanonicalPrefix_ReturnsLocalPrefix()
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.ToLocal(CanonicalWorkFolder);

        // Assert
        Assert.Equal(LinuxWorkFolder, result);
    }

    [Fact]
    public void ToCanonical_WhenPathEqualsLocalPrefix_ReturnsCanonicalPrefix()
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.ToCanonical(LinuxWorkFolder);

        // Assert
        Assert.Equal(CanonicalWorkFolder, result);
    }

    //the longer rule is listed second: the order of the rules must not matter
    [Fact]
    public void ToLocal_WhenSeveralRulesMatch_UsesLongestCanonicalPrefix()
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder),
            Rule(@"D:\1WorkDotnet\X", "/mnt/x"));

        // Act
        string? result = sut.ToLocal(CanonicalSolution);

        // Assert
        Assert.Equal("/mnt/x/Y.slnx", result);
    }

    [Fact]
    public void ToLocal_WhenLongerRuleIsListedFirst_StillUsesLongestCanonicalPrefix()
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(@"D:\1WorkDotnet\X", "/mnt/x"),
            Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.ToLocal(CanonicalSolution);

        // Assert
        Assert.Equal("/mnt/x/Y.slnx", result);
    }

    //the editor rejects a second rule for the same prefix; if a file still has one, the first rule is used
    [Fact]
    public void ToLocal_WhenTwoRulesHaveSamePrefix_UsesFirstRule()
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, "/first"), Rule(@"d:\1workdotnet\", "/second"));

        // Act
        string? result = sut.ToLocal(CanonicalSolution);

        // Assert
        Assert.Equal("/first/X/Y.slnx", result);
    }

    [Fact]
    public void ToCanonical_WhenSeveralRulesMatch_UsesLongestLocalPrefix()
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder),
            Rule(@"D:\Other", "/home/u/1WorkDotnet/X"));

        // Act
        string? result = sut.ToCanonical(LinuxSolution);

        // Assert
        Assert.Equal(@"D:\Other\Y.slnx", result);
    }

    [Fact]
    public void ToLocal_WhenPrefixEndsInsideFolderName_DoesNotMatch()
    {
        // Arrange
        const string canonicalPath = @"D:\1WorkDotnetX\Y.slnx";
        PathMapper sut = CreateWindowsSut(Rule(CanonicalWorkFolder, @"E:\Work"));

        // Act
        string? result = sut.ToLocal(canonicalPath);

        // Assert
        Assert.Equal(canonicalPath, result);
    }

    [Fact]
    public void ToCanonical_WhenPrefixEndsInsideFolderName_DoesNotMatch()
    {
        // Arrange
        const string localPath = "/home/u/1WorkDotnetX/Y.slnx";
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.ToCanonical(localPath);

        // Assert
        Assert.Equal(localPath, result);
    }

    [Theory]
    [InlineData(@"d:\1workdotnet\X\Y.slnx")]
    [InlineData(@"D:\1WORKDOTNET\X\Y.slnx")]
    public void ToLocal_WhenPathDiffersFromPrefixOnlyByCase_Matches(string canonicalPath)
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.ToLocal(canonicalPath);

        // Assert
        Assert.Equal(LinuxSolution, result);
    }

    [Fact]
    public void ToCanonical_WhenPathDiffersFromPrefixOnlyByCase_Matches()
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.ToCanonical("/HOME/u/1workdotnet/X/Y.slnx");

        // Assert
        Assert.Equal(CanonicalSolution, result);
    }

    [Theory]
    [InlineData(@"D:\1WorkDotnet\", "/home/u/1WorkDotnet/")]
    [InlineData(@"D:\1WorkDotnet\\", "/home/u/1WorkDotnet//")]
    [InlineData(@"D:\1WorkDotnet", "/home/u/1WorkDotnet/")]
    [InlineData(@"D:\1WorkDotnet\", "/home/u/1WorkDotnet")]
    public void ToLocal_WhenPrefixesEndWithSeparators_IgnoresThem(string canonicalPrefix, string localPrefix)
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(canonicalPrefix, localPrefix));

        // Act
        string? result = sut.ToLocal(CanonicalSolution);

        // Assert
        Assert.Equal(LinuxSolution, result);
    }

    [Theory]
    [InlineData(@"D:\1WorkDotnet\", "/home/u/1WorkDotnet/")]
    [InlineData(@"D:\1WorkDotnet\\", "/home/u/1WorkDotnet//")]
    public void ToCanonical_WhenPrefixesEndWithSeparators_IgnoresThem(string canonicalPrefix, string localPrefix)
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(canonicalPrefix, localPrefix));

        // Act
        string? result = sut.ToCanonical(LinuxSolution);

        // Assert
        Assert.Equal(CanonicalSolution, result);
    }

    //the canonical form always uses backslashes, even when the rule was typed with forward slashes
    [Fact]
    public void ToCanonical_WhenCanonicalPrefixUsesForwardSlashes_ReturnsBackslashes()
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule("D:/1WorkDotnet", LinuxWorkFolder));

        // Act
        string? result = sut.ToCanonical(LinuxSolution);

        // Assert
        Assert.Equal(CanonicalSolution, result);
    }

    [Theory]
    [InlineData(null, LinuxWorkFolder)]
    [InlineData("   ", LinuxWorkFolder)]
    [InlineData(CanonicalWorkFolder, null)]
    [InlineData(CanonicalWorkFolder, "")]
    public void ToLocal_WhenRuleIsIncomplete_IgnoresIt(string? canonicalPrefix, string? localPrefix)
    {
        // Arrange
        PathMapper sut = CreateWindowsSut(Rule(canonicalPrefix, localPrefix));

        // Act
        string? result = sut.ToLocal(CanonicalSolution);

        // Assert
        Assert.Equal(CanonicalSolution, result);
    }

    [Theory]
    [InlineData(@"D:\1WorkLTG\Project\Project.slnx")]
    [InlineData(@"\\server\share\Project.slnx")]
    public void ToLocal_WhenLinuxWindowsRootedPathHasNoRule_ReturnsItUnchangedAndRecordsIssue(string canonicalPath)
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.ToLocal(canonicalPath);

        // Assert
        Assert.Equal(canonicalPath, result);
        PathMappingIssue issue = Assert.Single(sut.Issues);
        Assert.Equal(new PathMappingIssue(EPathMappingDirection.ToLocal, canonicalPath), issue);
    }

    [Fact]
    public void ToCanonical_WhenLinuxPathHasNoRule_ReturnsItUnchangedAndRecordsIssue()
    {
        // Arrange
        const string localPath = "/opt/other/Project.slnx";
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.ToCanonical(localPath);

        // Assert
        Assert.Equal(localPath, result);
        PathMappingIssue issue = Assert.Single(sut.Issues);
        Assert.Equal(new PathMappingIssue(EPathMappingDirection.ToCanonical, localPath), issue);
    }

    //URLs and relative paths are not Windows-rooted, so they are not expected to have a rule
    [Theory]
    [InlineData("ftp://192.168.10.50/ProdBaseExchange/")]
    [InlineData(@"src\appcarcass")]
    public void ToLocal_WhenLinuxPathIsNotWindowsRooted_ReturnsItUnchangedWithoutIssue(string path)
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.ToLocal(path);

        // Assert
        Assert.Equal(path, result);
        Assert.Empty(sut.Issues);
    }

    [Theory]
    [InlineData(CanonicalSolution)]
    [InlineData("ftp://192.168.10.50/ProdBaseExchange/")]
    [InlineData("src/appcarcass")]
    public void ToCanonical_WhenLinuxPathIsNotLinuxRooted_ReturnsItUnchangedWithoutIssue(string path)
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.ToCanonical(path);

        // Assert
        Assert.Equal(path, result);
        Assert.Empty(sut.Issues);
    }

    //on Windows an unmatched path is already in the Windows form
    [Fact]
    public void ToLocalAndToCanonical_WhenWindowsPathHasNoRule_RecordNoIssue()
    {
        // Arrange
        const string path = @"D:\1WorkLTG\Project\Project.slnx";
        PathMapper sut = CreateWindowsSut(Rule(CanonicalWorkFolder, @"E:\Work"));

        // Act
        string? local = sut.ToLocal(path);
        string? canonical = sut.ToCanonical(path);

        // Assert
        Assert.Equal(path, local);
        Assert.Equal(path, canonical);
        Assert.Empty(sut.Issues);
    }

    [Fact]
    public void ToLocal_WhenSamePathIsUnmatchedTwice_RecordsOneIssue()
    {
        // Arrange
        const string canonicalPath = @"D:\1WorkLTG";
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        sut.ToLocal(canonicalPath);
        sut.ToLocal(canonicalPath);

        // Assert
        Assert.Single(sut.Issues);
    }

    [Fact]
    public void Issues_WhenPathsAreUnmatchedInBothDirections_KeepsThemInOrder()
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));
        PathMappingIssue[] expected =
        [
            new(EPathMappingDirection.ToLocal, @"E:\BAK"),
            new(EPathMappingDirection.ToCanonical, "/opt/other"),
            new(EPathMappingDirection.ToLocal, @"D:\1WorkLTG")
        ];

        // Act
        sut.ToLocal(@"E:\BAK");
        sut.ToCanonical("/opt/other");
        sut.ToLocal(@"D:\1WorkLTG");

        // Assert
        Assert.Equal(expected, sut.Issues);
    }

    [Fact]
    public void NormalizeRelativeToLocal_WhenLinux_ReplacesBackslashes()
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.NormalizeRelativeToLocal(@"{SpaProjectFolderRelativePath}\src\appcarcass");

        // Assert
        Assert.Equal("{SpaProjectFolderRelativePath}/src/appcarcass", result);
    }

    [Fact]
    public void NormalizeRelativeToCanonical_WhenLinux_ReplacesSlashes()
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.NormalizeRelativeToCanonical("{SpaProjectFolderRelativePath}/src/appcarcass");

        // Assert
        Assert.Equal(@"{SpaProjectFolderRelativePath}\src\appcarcass", result);
    }

    [Theory]
    [InlineData(@"src\appcarcass")]
    [InlineData("src/appcarcass")]
    public void NormalizeRelative_WhenWindows_ReturnsPathUnchangedInBothDirections(string relativePath)
    {
        // Arrange
        PathMapper sut = CreateWindowsSut(Rule(CanonicalWorkFolder, @"E:\Work"));

        // Act
        string? local = sut.NormalizeRelativeToLocal(relativePath);
        string? canonical = sut.NormalizeRelativeToCanonical(relativePath);

        // Assert
        Assert.Equal(relativePath, local);
        Assert.Equal(relativePath, canonical);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NormalizeRelative_WhenPathIsNullOrEmpty_ReturnsItUnchanged(string? relativePath)
    {
        // Arrange
        PathMapper sut = CreateLinuxSut();

        // Act
        string? local = sut.NormalizeRelativeToLocal(relativePath);
        string? canonical = sut.NormalizeRelativeToCanonical(relativePath);

        // Assert
        Assert.Equal(relativePath, local);
        Assert.Equal(relativePath, canonical);
    }

    //a relative path never matches a rule, even when it starts like the rest of a canonical prefix
    [Fact]
    public void NormalizeRelativeToLocal_WhenRelativePathLooksLikePrefix_OnlyReplacesSeparators()
    {
        // Arrange
        PathMapper sut = CreateLinuxSut(Rule(CanonicalWorkFolder, LinuxWorkFolder));

        // Act
        string? result = sut.NormalizeRelativeToLocal(@"1WorkDotnet\X");

        // Assert
        Assert.Equal("1WorkDotnet/X", result);
    }

    [Theory]
    [InlineData(@"d:\1WorkDotnet\", CanonicalWorkFolder, true)]
    [InlineData("D:/1WorkDotnet", CanonicalWorkFolder, true)]
    [InlineData(" /home/u/1WorkDotnet/ ", LinuxWorkFolder, true)]
    [InlineData(null, "", true)]
    [InlineData("", null, true)]
    [InlineData(CanonicalWorkFolder, @"D:\1WorkDotnetX", false)]
    [InlineData(null, CanonicalWorkFolder, false)]
    [InlineData(CanonicalWorkFolder, null, false)]
    public void IsSamePrefix_WhenCompared_IgnoresCaseTrailingAndKindOfSeparators(string? first, string? second,
        bool expected)
    {
        // Act
        bool result = PathMapper.IsSamePrefix(first, second);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(CanonicalWorkFolder, true)]
    [InlineData("d:", true)]
    [InlineData(@"\\server\share\x", true)]
    [InlineData(LinuxWorkFolder, false)]
    [InlineData(@"src\app", false)]
    [InlineData("ftp://192.168.10.50/x", false)]
    [InlineData("1:", false)]
    [InlineData("D", false)]
    [InlineData("", false)]
    public void IsWindowsRooted_WhenChecked_AcceptsDriveAndNetworkPaths(string path, bool expected)
    {
        // Act
        bool result = PathMapper.IsWindowsRooted(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void PublicConstructor_WhenCreated_UsesSeparatorOfCurrentOs()
    {
        // Arrange
        var sut = new PathMapper([Rule(CanonicalWorkFolder, @"E:\Work")]);
        string expected = Path.DirectorySeparatorChar == LinuxSeparator ? @"E:\Work/X/Y.slnx" : @"E:\Work\X\Y.slnx";

        // Act
        string? result = sut.ToLocal(CanonicalSolution);

        // Assert
        Assert.Equal(expected, result);
    }

    private static PathMapper CreateWindowsSut(params PathMappingModel[] pathMappings)
    {
        return new PathMapper(pathMappings, WindowsSeparator);
    }

    private static PathMapper CreateLinuxSut(params PathMappingModel[] pathMappings)
    {
        return new PathMapper(pathMappings, LinuxSeparator);
    }

    private static PathMappingModel Rule(string? canonicalPrefix, string? localPrefix)
    {
        return new PathMappingModel { CanonicalPrefix = canonicalPrefix, LocalPrefix = localPrefix };
    }
}

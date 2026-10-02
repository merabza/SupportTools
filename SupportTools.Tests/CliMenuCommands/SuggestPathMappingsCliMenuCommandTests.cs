using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using Moq;
using ParametersManagement.LibFileParameters.Models;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class SuggestPathMappingsCliMenuCommandTests : IDisposable
{
    private const string MenuName = "Suggest Path Mappings...";
    private const string WorkRoot = @"D:\1WorkDotnet";
    private const string SecurityRoot = @"D:\1WorkSecurity";
    private const string BackupRoot = @"E:\BAK";
    private const string LinuxWorkFolder = "/home/u/1WorkDotnet";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;

    private readonly SupportToolsParameters _parameters = new()
    {
        Projects =
        {
            ["SupportTools"] = new ProjectModel
            {
                ProjectFolderName = @"D:\1WorkDotnet\SupportTools",
                SolutionFileName = @"D:\1WorkDotnet\SupportTools\SupportTools\SupportTools.slnx",
                ProjectSecurityFolderPath = @"D:\1WorkSecurity\SupportTools"
            }
        },
        FileStorages = { ["Backups"] = new FileStorageData { FileStoragePath = BackupRoot } }
    };

    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly Queue<Func<string?, string?>> _textAnswers = new();
    private readonly List<(string FieldName, string? DefaultValue)> _textPrompts = [];

    public SuggestPathMappingsCliMenuCommandTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        SetupSaveResult(true);

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void Constructor_WhenCreated_SetsMenuNameAndReloadsMenuAfterRun()
    {
        // Act
        SuggestPathMappingsCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal(MenuName, sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
    }

    [Fact]
    public void PublicConstructor_WhenCreated_SetsMenuName()
    {
        // Act
        var sut = new SuggestPathMappingsCliMenuCommand(_parametersManager.Object);

        // Assert
        Assert.Equal(MenuName, sut.Name);
    }

    //the public constructor asks on the console: the test host has no console input, so reading the answer throws
    //after the prompt with the offered default is printed
    [Fact]
    public async Task PublicConstructor_WhenRunBodyAsks_PromptsForLocalPrefixWithCurrentValue()
    {
        // Arrange
        var sut = new SuggestPathMappingsCliMenuCommand(_parametersManager.Object);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => CliMenuTestAccess.InvokeRunBody(sut));

        // Assert
        Assert.Contains($"Enter Local prefix for {WorkRoot} [{WorkRoot}]: ", ConsoleText(), StringComparison.Ordinal);
        Assert.Empty(_parameters.PathMappings);
        VerifyNotSaved();
    }

    [Fact]
    public async Task RunBody_WhenParametersHaveNoCanonicalPaths_ReportsErrorWithoutAsking()
    {
        // Arrange
        _parameters.Projects.Clear();
        _parameters.FileStorages.Clear();

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_textPrompts);
        Assert.Contains("No canonical paths found in parameters", ConsoleText(), StringComparison.Ordinal);
        VerifyNotSaved();
    }

    [Fact]
    public async Task RunBody_WhenAsking_ExplainsHowToAnswer()
    {
        // Arrange
        AnswerAllWithDefaults();

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Contains("Enter keeps the current local prefix. The root itself or empty text means no mapping.",
            ConsoleText(), StringComparison.Ordinal);
    }

    //without a rule a root maps to itself, so the root is the default answer
    [Fact]
    public async Task RunBody_WhenRootsHaveNoRules_AsksForEveryRootInOrderWithRootAsDefault()
    {
        // Arrange
        (string, string?)[] expected =
        [
            ($"Local prefix for {WorkRoot}", WorkRoot), ($"Local prefix for {SecurityRoot}", SecurityRoot),
            ($"Local prefix for {BackupRoot}", BackupRoot)
        ];
        AnswerAllWithDefaults();

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal(expected, _textPrompts);
    }

    [Fact]
    public async Task RunBody_WhenRootHasRule_OffersItsLocalPrefixAsDefault()
    {
        // Arrange
        _parameters.PathMappings.Add(Rule(@"d:\1workdotnet\", LinuxWorkFolder));
        AnswerAllWithDefaults();

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal(LinuxWorkFolder, _textPrompts[0].DefaultValue);
    }

    [Fact]
    public async Task RunBody_WhenRootIsCoveredByShorterRule_OffersMappedPathAsDefault()
    {
        // Arrange
        _parameters.PathMappings.Add(Rule(@"D:\", @"F:\Work"));
        AnswerAllWithDefaults();

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal($@"F:\Work{Path.DirectorySeparatorChar}1WorkDotnet", _textPrompts[0].DefaultValue);
    }

    [Fact]
    public async Task RunBody_WhenEveryDefaultIsAccepted_ChangesNothingAndDoesNotSave()
    {
        // Arrange
        PathMappingModel rule = Rule(WorkRoot, LinuxWorkFolder);
        _parameters.PathMappings.Add(rule);
        AnswerAllWithDefaults();

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Same(rule, Assert.Single(_parameters.PathMappings));
        VerifyNotSaved();
    }

    [Fact]
    public async Task RunBody_WhenNewLocalPrefixEntered_AddsRuleForRoot()
    {
        // Arrange
        AnswerText(LinuxWorkFolder);
        AnswerWithDefault();
        AnswerWithDefault();

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        AssertSingleRule(WorkRoot, LinuxWorkFolder);
    }

    [Fact]
    public async Task RunBody_WhenRulesChanged_SavesParametersWithNumberOfChangedRoots()
    {
        // Arrange
        AnswerText(LinuxWorkFolder);
        AnswerWithDefault();
        AnswerText("/mnt/bak");

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        _parametersManager.Verify(
            x => x.Save(_parameters, "Path mappings changed: 2", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunBody_WhenAnswerHasSurroundingWhitespace_StoresTrimmedLocalPrefix()
    {
        // Arrange
        AnswerText($"  {LinuxWorkFolder}  ");
        AnswerWithDefault();
        AnswerWithDefault();

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        AssertSingleRule(WorkRoot, LinuxWorkFolder);
    }

    //the stored rule is written the way the root is: one rule per root, whatever the old spelling was
    [Fact]
    public async Task RunBody_WhenRootHasRuleAndOtherLocalPrefixEntered_ReplacesRule()
    {
        // Arrange
        _parameters.PathMappings.Add(Rule(@"d:\1workdotnet\", "/home/old/1WorkDotnet"));
        AnswerText(LinuxWorkFolder);
        AnswerWithDefault();
        AnswerWithDefault();

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        AssertSingleRule(WorkRoot, LinuxWorkFolder);
    }

    [Theory]
    [InlineData(WorkRoot)]
    [InlineData(@"d:\1workdotnet\")]
    [InlineData("")]
    [InlineData(null)]
    public async Task RunBody_WhenRootItselfOrNothingEntered_RemovesRuleOfRoot(string? answer)
    {
        // Arrange
        _parameters.PathMappings.Add(Rule(WorkRoot, LinuxWorkFolder));
        AnswerText(answer);
        AnswerWithDefault();
        AnswerWithDefault();

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_parameters.PathMappings);
        _parametersManager.Verify(
            x => x.Save(_parameters, "Path mappings changed: 1", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    //a root that maps to itself needs no rule on a computer with the layout of the main computer
    [Fact]
    public async Task RunBody_WhenRootWithoutRuleIsEmptied_ChangesNothingAndDoesNotSave()
    {
        // Arrange
        AnswerText(string.Empty);
        AnswerWithDefault();
        AnswerWithDefault();

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_parameters.PathMappings);
        VerifyNotSaved();
    }

    [Fact]
    public async Task RunBody_WhenRuleIsMoreSpecificThanRoot_KeepsIt()
    {
        // Arrange
        PathMappingModel specificRule = Rule(@"D:\1WorkDotnet\SupportTools", "/mnt/supporttools");
        _parameters.PathMappings.Add(specificRule);
        AnswerText(LinuxWorkFolder);
        AnswerWithDefault();
        AnswerWithDefault();

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal(2, _parameters.PathMappings.Count);
        Assert.Contains(specificRule, _parameters.PathMappings);
    }

    [Fact]
    public async Task RunBody_WhenSavingFails_ReturnsFalse()
    {
        // Arrange
        SetupSaveResult(false);
        AnswerText(LinuxWorkFolder);
        AnswerWithDefault();
        AnswerWithDefault();

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
    }

    //the rules change only after the last answer, so escaping half way leaves them as they were
    [Fact]
    public async Task RunBody_WhenInputIsEscapedAfterSomeAnswers_KeepsPathMappingsUnchanged()
    {
        // Arrange
        PathMappingModel rule = Rule(SecurityRoot, "/home/u/1WorkSecurity");
        _parameters.PathMappings.Add(rule);
        AnswerText(LinuxWorkFolder);
        AnswerText(string.Empty);
        _textAnswers.Enqueue(_ => throw new DataInputEscapeException("Escape"));

        // Act
        await Assert.ThrowsAsync<DataInputEscapeException>(() => CliMenuTestAccess.InvokeRunBody(CreateSut()));

        // Assert
        Assert.Same(rule, Assert.Single(_parameters.PathMappings));
        VerifyNotSaved();
    }

    private SuggestPathMappingsCliMenuCommand CreateSut()
    {
        return new SuggestPathMappingsCliMenuCommand(_parametersManager.Object, InputText);
    }

    private string? InputText(string fieldName, string? defaultValue)
    {
        _textPrompts.Add((fieldName, defaultValue));
        return _textAnswers.Dequeue()(defaultValue);
    }

    //Enter on an empty prompt returns the offered default
    private void AnswerWithDefault()
    {
        _textAnswers.Enqueue(defaultValue => defaultValue);
    }

    private void AnswerAllWithDefaults()
    {
        AnswerWithDefault();
        AnswerWithDefault();
        AnswerWithDefault();
    }

    private void AnswerText(string? text)
    {
        _textAnswers.Enqueue(_ => text);
    }

    private void SetupSaveResult(bool result)
    {
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(result);
    }

    private void VerifyNotSaved()
    {
        _parametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    private void AssertSingleRule(string canonicalPrefix, string localPrefix)
    {
        PathMappingModel rule = Assert.Single(_parameters.PathMappings);
        Assert.Equal(canonicalPrefix, rule.CanonicalPrefix);
        Assert.Equal(localPrefix, rule.LocalPrefix);
    }

    private static PathMappingModel Rule(string canonicalPrefix, string localPrefix)
    {
        return new PathMappingModel { CanonicalPrefix = canonicalPrefix, LocalPrefix = localPrefix };
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
    }
}

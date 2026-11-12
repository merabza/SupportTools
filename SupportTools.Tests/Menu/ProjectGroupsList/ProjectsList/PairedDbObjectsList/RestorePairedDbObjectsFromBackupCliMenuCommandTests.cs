using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class RestorePairedDbObjectsFromBackupCliMenuCommandTests : IDisposable
{
    private const string OldBackupName = "Pairs.json.20261101-100000-000.bak";
    private const string NewBackupName = "Pairs.json.20261110-100000-000.bak";

    private readonly List<string> _boolPrompts = [];
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly string _resultFileName;
    private readonly string _rootFolder;
    private readonly List<List<string>> _offeredBackups = [];
    private bool _confirm = true;
    private string? _selectedBackup = OldBackupName;

    public RestorePairedDbObjectsFromBackupCliMenuCommandTests()
    {
        _rootFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
        _resultFileName = Path.Combine(_rootFolder, "Pairs.json");
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_rootFolder, true);
    }

    [Fact]
    public void GetBackupFiles_WhenCalled_ListsOnlyTheDatedBackupsNewestFirst()
    {
        // Arrange
        WriteModel(OldBackupName, "Old");
        WriteModel(NewBackupName, "New");
        WriteModel("Pairs.json.bak", "Manual");
        WriteModel("Pairs.json.20261110-100000.bak", "Manual");
        WriteModel("Other.json.20261110-100000-000.bak", "Other");

        // Act
        List<string> backups = PairedDbObjectsBackups.GetBackupFiles(_resultFileName);

        // Assert
        Assert.Equal([NewBackupName, OldBackupName], backups.Select(Path.GetFileName));
    }

    //the restore saves through ParametersManager.Save, so the current file becomes a backup itself
    [Fact]
    public async Task RunBody_WhenBackupIsChosenAndConfirmed_RestoresItAndKeepsTheCurrentFile()
    {
        // Arrange
        WriteModel("Pairs.json", "Current");
        WriteModel(OldBackupName, "Old");
        WriteModel(NewBackupName, "New");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([[NewBackupName, OldBackupName]], _offeredBackups);
        Assert.Equal(["Old"], ReadModel(_resultFileName).PairedTables.Keys);
        List<string> backups = PairedDbObjectsBackups.GetBackupFiles(_resultFileName);
        Assert.Equal(3, backups.Count);
        Assert.Equal(["Current"], ReadModel(backups[0]).PairedTables.Keys);
        Assert.Contains($"Replace {_resultFileName} with backup {OldBackupName} (1 paired tables)?",
            Assert.Single(_boolPrompts), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenNotConfirmed_KeepsTheFile()
    {
        // Arrange
        WriteModel("Pairs.json", "Current");
        WriteModel(OldBackupName, "Old");
        _confirm = false;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(["Current"], ReadModel(_resultFileName).PairedTables.Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("(None)")]
    public async Task RunBody_WhenNoBackupIsChosen_AsksNothingMore(string? selected)
    {
        // Arrange
        WriteModel("Pairs.json", "Current");
        WriteModel(OldBackupName, "Old");
        _selectedBackup = selected;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_boolPrompts);
        Assert.Equal(["Current"], ReadModel(_resultFileName).PairedTables.Keys);
    }

    [Fact]
    public async Task RunBody_WhenThereAreNoBackups_ReportsIt()
    {
        // Arrange
        WriteModel("Pairs.json", "Current");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_offeredBackups);
        Assert.Contains($"No backups of {_resultFileName} found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenBackupIsNotJson_KeepsTheFile()
    {
        // Arrange
        WriteModel("Pairs.json", "Current");
        await File.WriteAllTextAsync(Path.Combine(_rootFolder, OldBackupName), "{ not json");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_boolPrompts);
        Assert.Equal(["Current"], ReadModel(_resultFileName).PairedTables.Keys);
    }

    private RestorePairedDbObjectsFromBackupCliMenuCommand CreateSut()
    {
        return new RestorePairedDbObjectsFromBackupCliMenuCommand(new Mock<ILogger>().Object, _resultFileName,
            (_, backupNames) =>
            {
                _offeredBackups.Add(backupNames);
                return _selectedBackup;
            }, (caption, _) =>
            {
                _boolPrompts.Add(caption);
                return _confirm;
            });
    }

    private void WriteModel(string fileName, string tableKey)
    {
        var model = new PairedDbObjectsModel(new Dictionary<string, PairedTable> { [tableKey] = new() });
        File.WriteAllText(Path.Combine(_rootFolder, fileName),
            JsonConvert.SerializeObject(model, Formatting.Indented));
    }

    private static PairedDbObjectsModel ReadModel(string filePath)
    {
        return JsonConvert.DeserializeObject<PairedDbObjectsModel>(File.ReadAllText(filePath))!;
    }
}

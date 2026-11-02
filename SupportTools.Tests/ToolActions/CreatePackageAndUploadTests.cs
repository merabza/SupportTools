using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibAppInstallWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.ToolActions;

//runs the real action over a throwaway project folder: the publish step is a fake that writes the output files,
//the exchange storage is a local folder, and a program "running from bin" is a copy of ping.exe
[Collection(ConsoleCaptureCollection.Name)]
public sealed class CreatePackageAndUploadTests : IDisposable
{
    private const string DateMask = "yyyyMMddHHmmssfff";
    private const string Runtime = "win-x64";
    private const string ServerName = "Srv";
    private const string EnvironmentName = "Prod";
    private const string ProjectName = "MyApp";
    private const string FilePrefix = $"{ServerName}-{EnvironmentName}-{ProjectName}-{Runtime}-";

    private readonly string _binFolder;
    private readonly List<string> _confirmQuestions = [];
    private readonly RecordingLogger _logger = new();
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly string _exchangeFolder;
    private readonly string _objFolder;
    private readonly TextWriter _originalConsoleOutput;
    private readonly string _processName = $"PkgTest{Guid.NewGuid():N}"[..20];
    private readonly string _projectFileName;
    private readonly string _projectFolder;
    private readonly List<string[]> _publishCalls = [];
    private readonly List<Process> _startedProcesses = [];
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private readonly string _workFolder;
    private bool _confirmAnswer;
    private bool _publishResult = true;

    public CreatePackageAndUploadTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _projectFolder = Path.Combine(_tempFolder, "Project");
        _binFolder = Path.Combine(_projectFolder, "bin");
        _objFolder = Path.Combine(_projectFolder, "obj");
        Directory.CreateDirectory(_binFolder);
        Directory.CreateDirectory(_objFolder);
        File.WriteAllText(Path.Combine(_binFolder, "old.dll"), "old");
        File.WriteAllText(Path.Combine(_objFolder, "old.json"), "old");
        _projectFileName = Path.Combine(_projectFolder, $"{_processName}.csproj");
        WriteProjectFile("<Version>2.3.4</Version>");
        _workFolder = Path.Combine(_tempFolder, "Work");
        _exchangeFolder = Path.Combine(_tempFolder, "Exchange");
    }

    public void Dispose()
    {
        foreach (Process process in _startedProcesses)
        {
            if (!process.HasExited)
            {
                process.Kill(true);
                process.WaitForExit();
            }

            process.Dispose();
        }

        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        GitTestRepositories.DeleteFolder(_tempFolder);
    }

    [Theory]
    [InlineData("server", "Server name is not specified")]
    [InlineData("environment", "Environment Name is not specified")]
    [InlineData("projectFile", "Project file name is not specified")]
    [InlineData("project", "Project name is not specified")]
    [InlineData("workFolder", "Work Folder name is not specified")]
    public async Task Run_WhenARequiredParameterIsEmpty_FailsWithoutPublishing(string emptyParameter,
        string expectedError)
    {
        // Arrange
        CreatePackageAndUpload sut = Create(emptyParameter == "server" ? " " : ServerName,
            emptyParameter == "environment" ? " " : EnvironmentName,
            emptyParameter == "projectFile" ? " " : _projectFileName, emptyParameter == "project" ? " " : ProjectName,
            emptyParameter == "workFolder" ? " " : _workFolder);

        // Act
        bool result = await sut.Run();

        // Assert
        Assert.False(result);
        Assert.Empty(_publishCalls);
        Assert.True(Directory.Exists(_binFolder));
        Assert.Null(sut.AssemblyVersion);
        AssertLogged(LogLevel.Error, expectedError);
    }

    [Fact]
    public async Task Run_WhenTheWorkFolderCannotBeCreated_Fails()
    {
        // Arrange
        string blockingFile = Path.Combine(_tempFolder, "AFile");
        await File.WriteAllTextAsync(blockingFile, "x");

        string workFolder = Path.Combine(blockingFile, "Work");

        // Act
        bool result = await Create(workFolder: workFolder).Run();

        // Assert
        Assert.False(result);
        Assert.Empty(_publishCalls);
        Assert.Contains($"[ERROR] Cannot create Folder {workFolder}.", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenTheOutputFolderAlreadyExists_FailsWithoutPublishing()
    {
        // Arrange
        const string fixedDateMask = "'fixed'";
        string outputFolder = Path.Combine(_workFolder, $"{FilePrefix}fixed");
        Directory.CreateDirectory(outputFolder);

        // Act
        bool result = await Create(dateMask: fixedDateMask).Run();

        // Assert
        Assert.False(result);
        Assert.Empty(_publishCalls);
        Assert.True(Directory.Exists(_binFolder));
        string expectedError = $"Project output folder {outputFolder} is already exists";
        AssertLogged(LogLevel.Error, expectedError);
        Assert.Contains($"[ERROR] {expectedError}", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenEverythingWorks_PublishesZipsAndUploadsThePackage()
    {
        // Arrange
        CreatePackageAndUpload sut = Create(localSmartSchema: KeepNewestSmartSchema());

        // Act
        bool result = await sut.Run();

        // Assert
        Assert.True(result);
        string[] call = Assert.Single(_publishCalls);
        Assert.Equal(Runtime, call[0]);
        Assert.Equal(_workFolder, Path.GetDirectoryName(call[1]));
        Assert.StartsWith(FilePrefix, Path.GetFileName(call[1]), StringComparison.Ordinal);
        Assert.Equal(_projectFileName, call[2]);
        Assert.Equal(sut.AssemblyVersion, call[3]);
        string zipFile = Assert.Single(Directory.GetFiles(_exchangeFolder));
        Assert.Equal($"{Path.GetFileName(call[1])}.zip", Path.GetFileName(zipFile));
        Assert.Empty(Directory.GetFileSystemEntries(_workFolder));
        using ZipArchive archive = await ZipFile.OpenReadAsync(zipFile);
        Assert.Equal(["app.dll", "appsettings.json"], archive.Entries.Select(x => x.FullName).Order());
    }

    [Fact]
    public async Task Run_WhenEverythingWorks_LogsEveryStep()
    {
        // Arrange
        CreatePackageAndUpload sut = Create();

        // Act
        await sut.Run();

        // Assert
        string outputFolder = _publishCalls[0][1];
        string zipFile = $"{outputFolder}.zip";
        string[] expected =
        [
            "Program Publisher Started...", "Detecting version...",
            $"dotnet publishing with assemblyVersion {sut.AssemblyVersion} ...", "Cleaning previous build outputs...",
            "Delete Redundant Files", "Delete appsettings.Development.json", $"Archiving {zipFile}...",
            $"Removing folder {outputFolder}...", $"Uploading {zipFile}...", "Remove redundant files...",
            $"Deleting {zipFile} file..."
        ];
        List<string> messages = [.. _logger.Messages.Select(x => x.Message)];
        Assert.All(expected, x => Assert.Contains((LogLevel.Information, x), _logger.Messages));
        Assert.Equal(expected, messages.Where(expected.Contains));
    }

    [Fact]
    public async Task Run_WhenEverythingWorks_DeletesTheOldBinAndObjFoldersBeforePublishing()
    {
        // Arrange
        bool binExistedAtPublish = true;
        bool objExistedAtPublish = true;
        CreatePackageAndUpload sut = Create(publishRelease: (_, output, _, _) =>
        {
            binExistedAtPublish = Directory.Exists(_binFolder);
            objExistedAtPublish = Directory.Exists(_objFolder);
            Directory.CreateDirectory(output);
            return true;
        });

        // Act
        bool result = await sut.Run();

        // Assert
        Assert.True(result);
        Assert.False(binExistedAtPublish);
        Assert.False(objExistedAtPublish);
        Assert.Empty(_confirmQuestions);
    }

    [Theory]
    [InlineData("<Version>2.3.4</Version>", "2.3")]
    [InlineData("<Version>7</Version>", "7.0")]
    [InlineData("", "1.0")]
    public async Task Run_SetsTheAssemblyVersionFromTheProjectVersionAndTheCurrentTime(string versionElement,
        string expectedPrefix)
    {
        // Arrange
        WriteProjectFile(versionElement);
        CreatePackageAndUpload sut = Create();
        DateTime before = DateTime.Now;

        // Act
        await sut.Run();

        // Assert
        DateTime after = DateTime.Now;
        Assert.NotNull(sut.AssemblyVersion);
        string[] parts = sut.AssemblyVersion.Split('.');
        Assert.Equal(4, parts.Length);
        Assert.Equal(expectedPrefix, $"{parts[0]}.{parts[1]}");
        Assert.Equal((DateTime.Today - new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays,
            int.Parse(parts[2], CultureInfo.InvariantCulture));
        int halfSeconds = int.Parse(parts[3], CultureInfo.InvariantCulture);
        Assert.InRange(halfSeconds, (int)(before - before.Date).TotalSeconds / 2,
            (int)(after - after.Date).TotalSeconds / 2);
    }

    [Fact]
    public async Task Run_WhenThePublishFails_FailsWithoutAPackage()
    {
        // Arrange
        _publishResult = false;

        // Act
        bool result = await Create().Run();

        // Assert
        Assert.False(result);
        Assert.False(Directory.Exists(_exchangeFolder));
        Assert.Empty(Directory.GetFiles(_workFolder));
        AssertLogged(LogLevel.Error, $"Cannot publish project {ProjectName}");
    }

    [Fact]
    public async Task Run_WhenTheExchangeStorageCannotBeCreated_Fails()
    {
        // Arrange
        string blockingFile = Path.Combine(_tempFolder, "AFile");
        await File.WriteAllTextAsync(blockingFile, "x");

        // Act
        bool result = await Create(exchangeFolder: Path.Combine(blockingFile, "Exchange")).Run();

        // Assert
        Assert.False(result);
        Assert.Single(Directory.GetFiles(_workFolder, "*.zip"));
        AssertLogged(LogLevel.Error, "cannot create file manager");
    }

    [Fact]
    public async Task Run_RemovesRedundantPackagesFromTheExchangeAndTheWorkFolders()
    {
        // Arrange
        string oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local).ToString(DateMask,
            CultureInfo.InvariantCulture);
        string oldPackageName = $"{FilePrefix}{oldDate}.zip";
        string otherFileName = $"Other-{oldDate}.zip";
        Directory.CreateDirectory(_exchangeFolder);
        Directory.CreateDirectory(_workFolder);
        await File.WriteAllTextAsync(Path.Combine(_exchangeFolder, oldPackageName), "old");
        await File.WriteAllTextAsync(Path.Combine(_exchangeFolder, otherFileName), "other");
        await File.WriteAllTextAsync(Path.Combine(_workFolder, oldPackageName), "old");

        // Act
        bool result = await Create().Run();

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(Path.Combine(_exchangeFolder, oldPackageName)));
        Assert.True(File.Exists(Path.Combine(_exchangeFolder, otherFileName)));
        Assert.False(File.Exists(Path.Combine(_workFolder, oldPackageName)));
        Assert.Equal(2, Directory.GetFiles(_exchangeFolder).Length);
    }

    [Fact]
    public async Task Run_WhenTheProgramRunsFromBinAndTheUserRefuses_CancelsTheUpdateAndKeepsTheProgramRunning()
    {
        // Arrange
        Process process = StartProgramIn(_binFolder);
        _confirmAnswer = false;

        // Act
        bool result = await Create().Run();

        // Assert
        Assert.False(result);
        string question = Assert.Single(_confirmQuestions);
        Assert.Equal($"Stop {_processName} (PID {process.Id}) to continue?", question);
        Assert.False(process.HasExited);
        Assert.True(File.Exists(Path.Combine(_binFolder, "old.dll")));
        Assert.Empty(_publishCalls);
        AssertLogged(LogLevel.Warning,
            $"Program {_processName} (PID {process.Id}) is running from {_binFolder}{Path.DirectorySeparatorChar}");
        AssertLogged(LogLevel.Error, $"Update cancelled, because {_processName} is still running");
    }

    [Fact]
    public async Task Run_WhenSeveralCopiesRunFromBin_AsksOnceForAllAndStopsThemAll()
    {
        // Arrange
        Process first = StartProgramIn(_binFolder);
        Process second = StartProgramIn(_binFolder, "Copy");
        _confirmAnswer = true;

        // Act
        bool result = await Create().Run();

        // Assert
        Assert.True(result);
        string question = Assert.Single(_confirmQuestions);
        Assert.Contains(question, (string[])
        [
            $"Stop {_processName} (PID {first.Id}, {second.Id}) to continue?",
            $"Stop {_processName} (PID {second.Id}, {first.Id}) to continue?"
        ]);
        Assert.True(first.HasExited);
        Assert.True(second.HasExited);
    }

    [Fact]
    public async Task Run_WhenTheProgramRunsFromBinAndTheUserAgrees_StopsItAndPublishes()
    {
        // Arrange
        Process process = StartProgramIn(_binFolder);
        _confirmAnswer = true;

        // Act
        bool result = await Create().Run();

        // Assert
        Assert.True(result);
        Assert.Single(_confirmQuestions);
        Assert.True(process.HasExited);
        Assert.False(Directory.Exists(_binFolder));
        Assert.Single(_publishCalls);
        AssertLogged(LogLevel.Information, $"Program {_processName} stopped");
    }

    [Fact]
    public async Task Run_WhenAProgramOfTheSameNameRunsFromAnotherFolder_DoesNotAsk()
    {
        // Arrange
        Process process = StartProgramIn(Path.Combine(_tempFolder, "Elsewhere"));

        // Act
        bool result = await Create().Run();

        // Assert
        Assert.True(result);
        Assert.Empty(_confirmQuestions);
        Assert.False(process.HasExited);
    }

    [Fact]
    public async Task Run_WhenAProgramRunsFromAFolderWhoseNameStartsLikeBin_DoesNotAsk()
    {
        // Arrange
        Process process = StartProgramIn(Path.Combine(_projectFolder, "binary"));

        // Act
        bool result = await Create().Run();

        // Assert
        Assert.True(result);
        Assert.Empty(_confirmQuestions);
        Assert.False(process.HasExited);
    }

    [Fact]
    public void IsRunningFrom_WhenTheFolderMatchesIgnoringCase_ReturnsTrue()
    {
        // Arrange
        Process process = StartProgramIn(_binFolder);

        // Act
        bool result = CreatePackageAndUpload.IsRunningFrom(process,
            (_binFolder + Path.DirectorySeparatorChar).ToUpperInvariant());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRunningFrom_WhenTheProcessHasExited_ReturnsFalse()
    {
        // Arrange
        Process process = StartProgramIn(_binFolder);
        process.Kill();
        process.WaitForExit();

        // Act
        bool result = CreatePackageAndUpload.IsRunningFrom(process, _binFolder + Path.DirectorySeparatorChar);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRunningFrom_WhenNoProcessIsAssociated_ReturnsFalse()
    {
        // Arrange
        using var process = new Process();

        // Act
        bool result = CreatePackageAndUpload.IsRunningFrom(process, @"C:\");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsRunningFrom_WhenTheModuleCannotBeRead_ReturnsFalse()
    {
        // Arrange
        using Process systemProcess = Process.GetProcessById(4);

        // Act
        bool result = CreatePackageAndUpload.IsRunningFrom(systemProcess, @"C:\");

        // Assert
        Assert.False(result);
    }

    private CreatePackageAndUpload Create(string serverName = ServerName, string environmentName = EnvironmentName,
        string? projectFileName = null, string projectName = ProjectName, string? workFolder = null,
        string dateMask = DateMask, string? exchangeFolder = null, SmartSchema? localSmartSchema = null,
        Func<string, string, string, string, bool>? publishRelease = null)
    {
        //the exchange keeps the newest package, the work folder keeps none unless the test says otherwise
        return new CreatePackageAndUpload(_logger, projectName, projectFileName ?? _projectFileName,
            new ServerInfoModel { ServerName = serverName, EnvironmentName = environmentName },
            workFolder ?? _workFolder, dateMask, Runtime, ["appsettings.Development.json", " "], "up",
            new FileStorageData { FileStoragePath = exchangeFolder ?? _exchangeFolder },
            localSmartSchema ?? new SmartSchema(), KeepNewestSmartSchema(),
            question =>
            {
                _confirmQuestions.Add(question);
                return _confirmAnswer;
            }, publishRelease ?? Publish);
    }

    private static SmartSchema KeepNewestSmartSchema()
    {
        return new SmartSchema
        {
            LastPreserveCount = 1, Details = [new SmartSchemaDetail { PeriodType = EPeriodType.Day, PreserveCount = 1 }]
        };
    }

    private void AssertLogged(LogLevel level, string message)
    {
        Assert.Contains((level, message), _logger.Messages);
    }

    private bool Publish(string runtime, string outputFolderPath, string projectFileName, string assemblyVersion)
    {
        _publishCalls.Add([runtime, outputFolderPath, projectFileName, assemblyVersion]);
        if (!_publishResult)
        {
            return false;
        }

        File.WriteAllText(Path.Combine(outputFolderPath, "app.dll"), "app");
        File.WriteAllText(Path.Combine(outputFolderPath, "appsettings.json"), "{}");
        File.WriteAllText(Path.Combine(outputFolderPath, "appsettings.Development.json"), "{}");
        return true;
    }

    //a second copy of the same program goes into a subfolder, so the process name stays the same
    private Process StartProgramIn(string folder, string? subFolder = null)
    {
        if (subFolder is not null)
        {
            folder = Path.Combine(folder, subFolder);
        }

        Directory.CreateDirectory(folder);
        string programFileName = Path.Combine(folder, $"{_processName}.exe");
        File.Copy(Path.Combine(Environment.SystemDirectory, "PING.EXE"), programFileName);
        var startInfo = new ProcessStartInfo(programFileName, "-n 120 127.0.0.1")
        {
            CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true
        };
        Process process = Process.Start(startInfo) ??
                          throw new InvalidOperationException($"Cannot start {programFileName}");
        _startedProcesses.Add(process);
        //right after the start the main module of the process cannot be read yet
        Assert.True(SpinWait.SpinUntil(() => HasMainModule(process), TimeSpan.FromSeconds(10)));
        return process;
    }

    private static bool HasMainModule(Process process)
    {
        try
        {
            return process.MainModule is not null;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    private void WriteProjectFile(string versionElement)
    {
        File.WriteAllText(_projectFileName,
            $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>{versionElement}</PropertyGroup></Project>");
    }

    //every level is enabled and the messages are kept formatted
    private sealed class RecordingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add((logLevel, formatter(state, exception)));
        }
    }
}

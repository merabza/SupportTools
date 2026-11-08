using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AppCliTools.LibDataInput;
using Newtonsoft.Json;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests;

//the entry point (Program.cs) in this process, in the --run mode: the menu needs a real console, the --run mode does
//not. The parameters file is a temp file with made-up values; the auto-sync server is 127.0.0.1:0, which refuses at
//once
[Collection(ConsoleCaptureCollection.Name)]
public sealed class ProgramTests : IDisposable
{
    private const string ApiClientName = "SupportToolsServer";
    private const string PairsFileContent = "{ \"PairedTables\": {} }";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly string _parametersFileName;
    private readonly string _tempFolder;

    public ProgramTests()
    {
        _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
        _parametersFileName = Path.Combine(_tempFolder, "SupportTools.json");
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        //--allyes stays on in this process after the run
        Inputer.AnswerYesToAll = false;
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public async Task Main_WhenToolDoesNotExist_ReturnsTheExitCodeOfTheArguments()
    {
        // Act
        int result = await RunMain("--run", "NoSuchTool", "--project", "App");

        // Assert
        Assert.Equal(5, result);
        Assert.Contains("Tool with name NoSuchTool does not exists", ConsoleText(), StringComparison.Ordinal);
    }

    //without --use the parameters file is looked for in the current folder and next to the program; the test host has
    //none, so the program shows its options
    [Fact]
    public async Task Main_WhenNoParametersFileIsFound_ShowsTheHelpAndReturns1()
    {
        // Act
        int result = await RunMain();

        // Assert
        Assert.Equal(1, result);
        Assert.Contains("--use", ConsoleText(), StringComparison.Ordinal);
    }

    //an invalid file makes ParametersService offer to create a new one, which needs a console answer; without a console
    //(a script, the test host) the exception is reported and the program ends with 4
    [Fact]
    public async Task Main_WhenParametersFileIsNotValidAndNoConsoleAnswers_ReportsTheErrorAndReturns4()
    {
        // Arrange
        await File.WriteAllTextAsync(_parametersFileName, "{ not json");

        // Act
        int result = await RunMain("--use", _parametersFileName);

        // Assert
        Assert.Equal(4, result);
        Assert.Contains($"File {_parametersFileName} is not valid parameters file", ConsoleLines());
        //the question has no line break, so the error follows it on the same line
        Assert.Contains("[ERROR] InvalidOperationException thrown with message: ", ConsoleText(),
            StringComparison.Ordinal);
    }

    //D2: the start-up pull runs before the tool and writes at once (no menu clears the screen in this mode); an
    //unreachable server only warns, and the tool runs after it
    [Fact]
    public async Task Main_WhenRunModeAndServerIsUnreachable_WarnsAndThenRunsTheTool()
    {
        // Arrange
        await WriteParameters(CreateParametersWithServer(true));

        // Act
        int result = await RunMain("--use", _parametersFileName, "--run", "DropDevDatabase", "--project",
            "NoSuchProject");

        // Assert
        Assert.Equal(7, result);
        List<string> lines = ConsoleLines();
        int warningIndex = lines.FindIndex(x =>
            x.StartsWith("[warning] Registry auto-sync: SupportToolsServer is not reachable (",
                StringComparison.Ordinal));
        int errorIndex = lines.IndexOf("[ERROR] Project with name NoSuchProject not found");
        Assert.Equal("Loading...", lines[0]);
        Assert.True(warningIndex > 0, ConsoleText());
        Assert.True(errorIndex > warningIndex, ConsoleText());
    }

    [Fact]
    public async Task Main_WhenRunModeAndAutoSyncIsOff_DoesNotSync()
    {
        // Arrange
        await WriteParameters(CreateParametersWithServer(false));

        // Act
        int result = await RunMain("--use", _parametersFileName, "--run", "DropDevDatabase", "--project",
            "NoSuchProject");

        // Assert
        Assert.Equal(7, result);
        Assert.DoesNotContain("Registry auto-sync", ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("[ERROR] Project with name NoSuchProject not found", ConsoleLines());
    }

    //a project tool runs only when the project allows it
    [Fact]
    public async Task Main_WhenProjectToolIsNotAllowed_ReportsItAndReturns7()
    {
        // Arrange
        await WriteParameters(new SupportToolsParameters { Projects = { ["App"] = new ProjectModel() } });

        // Act
        int result = await RunMain("--use", _parametersFileName, "--run", "DropDevDatabase", "--project", "App");

        // Assert
        Assert.Equal(7, result);
        Assert.Contains("[ERROR] Tool DropDevDatabase is not allowed for project App. Allowed tools are: ",
            ConsoleLines());
    }

    //a server tool runs on a server of the project
    [Fact]
    public async Task Main_WhenServerToolRunsOnAMissingServer_ReportsItAndReturns7()
    {
        // Arrange
        await WriteParameters(new SupportToolsParameters { Projects = { ["App"] = new ProjectModel() } });

        // Act
        int result = await RunMain("--use", _parametersFileName, "--run", "AppSettingsEncoder", "--project", "App",
            "--server", "Srv");

        // Assert
        Assert.Equal(7, result);
        Assert.Contains("[ERROR] Server with name Srv not found for project App. Existing servers are: ",
            ConsoleLines());
    }

    //--allyes answers the yes/no questions of the tool without the console: PairProdCopyAndDevDbObjects first asks
    //whether its result file may be overwritten (default no) and then reads the databases, which refuse at once
    [Fact]
    public async Task Main_WhenAllYes_AnswersTheQuestionOfTheToolWithoutTheConsole()
    {
        // Arrange
        string resultFileName = await WritePairingProject();

        // Act
        int result = await RunMain("--use", _parametersFileName, "--run", "PairProdCopyAndDevDbObjects", "--project",
            "App", "--allyes");

        // Assert
        Assert.Equal(7, result);
        Assert.Contains($"File {resultFileName} exists, overwrite? (y/n)[n]: Y (yes to all){Environment.NewLine}",
            ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("[ERROR] Failed to read tables and columns from ProdCopy database", ConsoleLines());
        Assert.Equal(PairsFileContent, await File.ReadAllTextAsync(resultFileName));
    }

    //without --allyes the same question waits for the console, which the test host does not have: the tool stops there
    //and never reaches the databases
    [Fact]
    public async Task Main_WhenNotAllYes_AsksTheQuestionOfTheToolFromTheConsole()
    {
        // Arrange
        string resultFileName = await WritePairingProject();

        // Act
        int result = await RunMain("--use", _parametersFileName, "--run", "PairProdCopyAndDevDbObjects", "--project",
            "App");

        // Assert
        Assert.Equal(7, result);
        Assert.Contains($"File {resultFileName} exists, overwrite? (y/n)[n]: ", ConsoleText(),
            StringComparison.Ordinal);
        Assert.DoesNotContain("(yes to all)", ConsoleText(), StringComparison.Ordinal);
        Assert.DoesNotContain("Failed to read tables and columns", ConsoleText(), StringComparison.Ordinal);
        Assert.Equal(PairsFileContent, await File.ReadAllTextAsync(resultFileName));
    }

    //the parameters file is read before --allyes is turned on, so the offer to rewrite an invalid file still needs a
    //console answer, and the file stays as it is
    [Fact]
    public async Task Main_WhenAllYesAndParametersFileIsNotValid_DoesNotAnswerTheRewriteQuestion()
    {
        // Arrange
        const string invalidParameters = "{ not json";
        await File.WriteAllTextAsync(_parametersFileName, invalidParameters);

        // Act
        int result = await RunMain("--use", _parametersFileName, "--run", "DropDevDatabase", "--project", "App",
            "--allyes");

        // Assert
        Assert.Equal(4, result);
        Assert.DoesNotContain("(yes to all)", ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("[ERROR] InvalidOperationException thrown with message: ", ConsoleText(),
            StringComparison.Ordinal);
        Assert.Equal(invalidParameters, await File.ReadAllTextAsync(_parametersFileName));
    }

    //PairProdCopyAndDevDbObjects for project App, whose result file exists already. Both databases are on a MariaDB
    //address where nothing listens
    private async Task<string> WritePairingProject()
    {
        const string connectionName = "MariaDbNowhere";
        string resultFileName = Path.Combine(_tempFolder, "PairedDbObjects.json");
        await File.WriteAllTextAsync(resultFileName, PairsFileContent);
        await WriteParameters(new SupportToolsParameters
        {
            DatabaseServerConnections =
            {
                [connectionName] = new DatabaseServerConnectionData
                {
                    DatabaseServerProvider = EDatabaseProvider.MariaDb,
                    ServerAddress = "127.0.0.1:1",
                    ServerUser = "root",
                    ServerPass = "fake-password",
                    ConnectionTimeOut = 5
                }
            },
            Projects =
            {
                ["App"] = new ProjectModel
                {
                    AllowToolsList = [EProjectTools.PairProdCopyAndDevDbObjects],
                    ProdCopyDatabaseParameters =
                        new DatabaseParameters { DbConnectionName = connectionName, DatabaseName = "AppProdCopy" },
                    DevDatabaseParameters =
                        new DatabaseParameters { DbConnectionName = connectionName, DatabaseName = "AppDev" },
                    PairedDbObjectsResultFileName = resultFileName
                }
            }
        });
        return resultFileName;
    }

    private static SupportToolsParameters CreateParametersWithServer(bool autoSync)
    {
        return new SupportToolsParameters
        {
            AutoSyncWithSupportToolsServer = autoSync,
            SupportToolsServerWebApiClientName = ApiClientName,
            ApiClients =
            {
                [ApiClientName] = new ApiClientSettings
                {
                    Server = "http://127.0.0.1:0/api/v1", ApiKey = "fake-api-key-of-the-tests"
                }
            }
        };
    }

    private Task WriteParameters(SupportToolsParameters parameters)
    {
        return File.WriteAllTextAsync(_parametersFileName, JsonConvert.SerializeObject(parameters));
    }

    //top-level statements compile to Program.<Main>$, which returns the exit code
    private static async Task<int> RunMain(params string[] args)
    {
        MethodInfo main = typeof(Program).GetMethod("<Main>$", BindingFlags.Static | BindingFlags.NonPublic)!;
        return await (Task<int>)main.Invoke(null, [args])!;
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
    }

    private List<string> ConsoleLines()
    {
        return [.. ConsoleText().Split(Environment.NewLine)];
    }
}

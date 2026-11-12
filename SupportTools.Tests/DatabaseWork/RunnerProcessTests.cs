using System;
using System.Collections.Generic;
using System.IO;
using LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SupportTools.Tests.DatabaseWork;

//SeederRules-ისა და OldDataConvertor-ის გამოტანა მონაცემებია (მაგ. მომხმარებლების ელფოსტები, სახელები და პაროლის
//hash-ები), OldDataConvertor-ის არგუმენტებში კი ProdCopy-ის მიერთების სტრიქონია: არცერთი არ უნდა მოხვდეს ლოგსა და
//კონსოლზე. პროცესი ნამდვილია (cmd ან sh) და გამოგონილ მონაცემებს stdout-სა და stderr-ში წერს, StShared კი ის
//პაკეტია, რომელსაც SupportTools იყენებს
[Collection(ConsoleCaptureCollection.Name)]
public sealed class RunnerProcessTests : IDisposable
{
    private const string SourceLabel = "DataSeederRules";
    private const string TableName = "Users";
    private const string ProjectFilePath = @"D:\Fake\Fake.DataSeederRules.csproj";
    private const string UserName = "runner.test.username";
    private const string Email = "runner.test.mail@example.com";
    private const string FirstName = "RunnerTestFirstName";
    private const string PasswordHash = "AQAAAAIAAYagAAAAEFakeHashForRunnerProcessTests==";
    private const string ConnectionStringPassword = "RunnerTestProdCopyPassword";

    private const string JsonLine =
        $$"""[{"UserName":"{{UserName}}","Email":"{{Email}}","FirstName":"{{FirstName}}","PasswordHash":"{{PasswordHash}}"}]""";

    private static readonly string[] DataValues = [UserName, Email, FirstName, PasswordHash, ConnectionStringPassword];

    private readonly StringWriter _console = new();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"RunnerProcessTests_{Guid.NewGuid():N}");
    private readonly RecordingLogger _logger = new();
    private readonly TextWriter _originalConsoleOut = Console.Out;

    public RunnerProcessTests()
    {
        Directory.CreateDirectory(_folder);
        Console.SetOut(_console);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOut);
        _console.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    [Fact]
    public void RunAndParseRows_ReturnsTheRowsWithoutWritingTheOutputToTheLogOrTheConsole()
    {
        // Arrange
        string outputFile = WriteOutputFile($"[10:15:00 INF] Starting data seeding for table: {TableName}", JsonLine);

        // Act
        List<Dictionary<string, object?>>? rows = RunProcess(outputFile, 0);

        // Assert
        Assert.NotNull(rows);
        Dictionary<string, object?> row = Assert.Single(rows);
        Assert.Equal(Email, row["Email"]);
        Assert.Equal(PasswordHash, row["PasswordHash"]);
        Assert.Contains(_logger.Messages,
            m => m.Level == LogLevel.Information &&
                 m.Message == $"Running {SourceLabel} {ProjectFilePath} for table {TableName}");
        AssertNoDataWritten();
    }

    [Fact]
    public void RunAndParseRows_WhenTheProcessFails_WritesAnErrorWithoutTheOutput()
    {
        // Arrange: SeederRules საკუთარ შეცდომას stdout-ში წერს და 3 ან 4 კოდით სრულდება
        string outputFile = WriteOutputFile($"[ERROR] Fake failure for user {Email}", JsonLine);

        // Act
        List<Dictionary<string, object?>>? rows = RunProcess(outputFile, 3);

        // Assert
        Assert.Null(rows);
        Assert.Contains(_logger.Messages,
            m => m.Level == LogLevel.Error && m.Message.StartsWith(
                $"{SourceLabel} process failed for table '{TableName}'", StringComparison.Ordinal));
        AssertNoDataWritten();
    }

    [Fact]
    public void RunAndParseRows_WhenTheOutputHasNoJsonArray_WritesAnErrorWithoutTheOutput()
    {
        // Arrange
        string outputFile = WriteOutputFile($"UserName: {UserName}, Email: {Email}, PasswordHash: {PasswordHash}");

        // Act
        List<Dictionary<string, object?>>? rows = RunProcess(outputFile, 0);

        // Assert
        Assert.Null(rows);
        Assert.Contains(_logger.Messages,
            m => m.Level == LogLevel.Error &&
                 m.Message == $"Could not find JSON array in {SourceLabel} output for table '{TableName}'");
        AssertNoDataWritten();
    }

    [Fact]
    public void RunAndParseRows_WhenTheJsonIsInvalid_WritesAnErrorWithoutTheOutput()
    {
        // Arrange: Email-ის შემდეგ მძიმე აკლია
        string outputFile =
            WriteOutputFile($$"""[{"Email":"{{Email}}" "PasswordHash":"{{PasswordHash}}","FirstName":"{{FirstName}}"}]""");

        // Act
        List<Dictionary<string, object?>>? rows = RunProcess(outputFile, 0);

        // Assert
        Assert.Null(rows);
        Assert.Contains(_logger.Messages,
            m => m.Level == LogLevel.Error && m.Message.StartsWith(
                $"Failed to parse JSON from {SourceLabel} for table '{TableName}'", StringComparison.Ordinal));
        AssertNoDataWritten();
    }

    //პროცესი ფაილს stdout-სა და stderr-ში წერს და exitCode-ით სრულდება; ბრძანებაში ProdCopy-ის მიერთების სტრიქონიც
    //არის, როგორც OldDataConvertor-ის არგუმენტებში
    private List<Dictionary<string, object?>>? RunProcess(string outputFile, int exitCode)
    {
        const string connectionString = $"Server=prodcopy;Password={ConnectionStringPassword}";
        (string programFileName, string arguments) = OperatingSystem.IsWindows()
            ? ("cmd",
                $"/c set \"ConnectionString={connectionString}\" & type \"{outputFile}\" & type \"{outputFile}\" 1>&2 & exit {exitCode}")
            : ("sh",
                $"-c \"ConnectionString='{connectionString}'; cat '{outputFile}'; cat '{outputFile}' 1>&2; exit {exitCode}\"");
        return RunnerProcess.RunAndParseRows(SourceLabel, ProjectFilePath, TableName, programFileName, arguments,
            _logger);
    }

    private string WriteOutputFile(params string[] lines)
    {
        string fileName = Path.Combine(_folder, $"output_{Guid.NewGuid():N}.txt");
        File.WriteAllLines(fileName, lines);
        return fileName;
    }

    private void AssertNoDataWritten()
    {
        string console = _console.ToString();
        foreach (string value in DataValues)
        {
            Assert.DoesNotContain(value, console, StringComparison.Ordinal);
            Assert.DoesNotContain(_logger.Messages, m => m.Message.Contains(value, StringComparison.Ordinal));
        }
    }

    //ყველა დონე ჩართულია; შეტყობინებას გამონაკლისის ტექსტიც ერთვის, როგორც Serilog-ის ფაილში
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
            string message = formatter(state, exception);
            Messages.Add((logLevel, exception is null ? message : $"{message}{Environment.NewLine}{exception}"));
        }
    }
}

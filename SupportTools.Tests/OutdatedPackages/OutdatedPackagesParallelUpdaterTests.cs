using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using LibGitWork;
using LibGitWork.OutdatedPackages;
using Microsoft.Extensions.Logging;
using Moq;
using SupportTools.Tests.SyncV3;
using Xunit;

namespace SupportTools.Tests.OutdatedPackages;

//the jobs are plain functions, so no restore and no dotnet outdated run; FakeProgressConsole is the screen of the board
[Collection(ConsoleCaptureCollection.Name)]
public sealed class OutdatedPackagesParallelUpdaterTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;

    public OutdatedPackagesParallelUpdaterTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void Run_WhenCalled_ReturnsTheResultsInTheOrderOfTheJobs()
    {
        // Arrange
        PackageUpdateJob[] jobs =
        [
            new("GitA", "W1", _ => EPackageUpdateResult.Success),
            new("GitB", "W2", _ => EPackageUpdateResult.OutdatedFailed),
            new("GitC", "W3", _ => EPackageUpdateResult.RestoreAfterUpdateFailed)
        ];

        // Act
        EPackageUpdateResult[] results =
            new OutdatedPackagesParallelUpdater(null, null).Run(jobs, CancellationToken.None);

        // Assert
        Assert.Equal(
        [
            EPackageUpdateResult.Success, EPackageUpdateResult.OutdatedFailed,
            EPackageUpdateResult.RestoreAfterUpdateFailed
        ], results);
    }

    //every job waits until eight run (or two seconds pass), so a ninth at once would be seen
    [Fact]
    public void Run_WhenThereAreManyJobs_RunsEightAtOnce()
    {
        // Arrange
        int running = 0;
        int maxRunning = 0;
        PackageUpdateJob[] jobs =
        [
            .. Enumerable.Range(0, 20).Select(i => new PackageUpdateJob($"Git{i}", $"W{i}", _ =>
            {
                int now = Interlocked.Increment(ref running);
                InterlockedMax(ref maxRunning, now);
                SpinWait.SpinUntil(() => Volatile.Read(ref running) >= OutdatedPackagesParallelUpdater.MaxParallelism,
                    TimeSpan.FromSeconds(2));
                Interlocked.Decrement(ref running);
                return EPackageUpdateResult.Success;
            }))
        ];

        // Act
        new OutdatedPackagesParallelUpdater(null, null).Run(jobs, CancellationToken.None);

        // Assert
        Assert.Equal(8, maxRunning);
    }

    //their restores would write the same obj files of the neighbour projects; folder names are compared without case
    [Fact]
    public void Run_WhenJobsShareAFolder_RunsThemOneAfterTheOtherInTheirOrder()
    {
        // Arrange
        var order = new ConcurrentQueue<string>();
        int runningInFolder = 0;
        int maxRunningInFolder = 0;
        PackageUpdateJob[] jobs =
        [
            .. new[] { ("GitA", @"D:\W1"), ("GitB", @"D:\W2"), ("GitC", @"d:\w1"), ("GitD", @"D:\W1") }.Select(x =>
                new PackageUpdateJob(x.Item1, x.Item2, _ =>
                {
                    if (!x.Item2.Equals(@"D:\W1", StringComparison.OrdinalIgnoreCase))
                    {
                        return EPackageUpdateResult.Success;
                    }

                    InterlockedMax(ref maxRunningInFolder, Interlocked.Increment(ref runningInFolder));
                    order.Enqueue(x.Item1);
                    //another job of the folder that ran at the same time would be seen while this one waits
                    SpinWait.SpinUntil(() => Volatile.Read(ref runningInFolder) > 1, TimeSpan.FromMilliseconds(50));
                    Interlocked.Decrement(ref runningInFolder);
                    return EPackageUpdateResult.Success;
                }))
        ];

        // Act
        new OutdatedPackagesParallelUpdater(null, null).Run(jobs, CancellationToken.None);

        // Assert
        Assert.Equal(1, maxRunningInFolder);
        Assert.Equal(["GitA", "GitC", "GitD"], order);
    }

    //a job's line shows its step and disappears when the job ends; the lines it writes stay above the board
    [Fact]
    public void Run_WhenThereIsABoard_ShowsTheStepOfAJobInItsLineAndKeepsItsLines()
    {
        // Arrange
        var console = new FakeProgressConsole(80, 20);
        PackageUpdateJob job = new("GitA", "W1", output =>
        {
            output.SetStatus("dotnet outdated");
            output.WriteLine("1 package version updated", null);
            output.WriteLine("restore after the update failed for A.slnx", ConsoleColor.Red);
            return EPackageUpdateResult.RestoreAfterUpdateFailed;
        });

        // Act
        new OutdatedPackagesParallelUpdater(null, console).Run([job], CancellationToken.None);

        // Assert
        Assert.Equal(["GitA: 1 package version updated", "GitA: restore after the update failed for A.slnx"],
            console.Lines);
        Assert.Contains(console.Writes,
            x => x.Text.StartsWith("Updating packages of 1 gits: 0 done ", StringComparison.Ordinal));
        Assert.Contains(console.Writes, x => x.Text.StartsWith("GitA  dotnet outdated ", StringComparison.Ordinal));
        Assert.Contains(console.Writes,
            x => x.Text == "GitA: restore after the update failed for A.slnx" && x.Color == ConsoleColor.Red);
        Assert.Contains(console.Writes,
            x => x.Text.StartsWith("Updating packages of 1 gits: 1 done ", StringComparison.Ordinal));
        Assert.True(console.CursorVisible);
    }

    //redirected output: no board is drawn, the lines go to the console
    [Fact]
    public void Run_WhenThereIsNoBoard_WritesTheLinesToTheConsole()
    {
        // Arrange
        PackageUpdateJob job = new("GitA", "W1", output =>
        {
            output.SetStatus("dotnet outdated");
            output.WriteLine("1 package version updated", null);
            return EPackageUpdateResult.Success;
        });

        // Act
        new OutdatedPackagesParallelUpdater(null, null).Run([job], CancellationToken.None);

        // Assert
        Assert.Equal("GitA: 1 package version updated" + Environment.NewLine, _consoleOutput.ToString());
    }

    [Fact]
    public void Run_WhenCancelled_StartsNoJob()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        bool started = false;
        PackageUpdateJob job = new("GitA", "W1", _ =>
        {
            started = true;
            return EPackageUpdateResult.Success;
        });

        // Act
        Assert.Throws<OperationCanceledException>(() =>
            new OutdatedPackagesParallelUpdater(null, null).Run([job], cancellationTokenSource.Token));

        // Assert
        Assert.False(started);
    }

    //a red line is an error, a yellow one a warning, the others information
    [Theory]
    [InlineData(ConsoleColor.Red, LogLevel.Error)]
    [InlineData(ConsoleColor.Yellow, LogLevel.Warning)]
    [InlineData(null, LogLevel.Information)]
    public void Run_WhenThereIsALogger_LogsTheLinesWithTheLevelOfTheirColor(ConsoleColor? color, LogLevel logLevel)
    {
        // Arrange
        var logger = new Mock<ILogger>();
        logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        PackageUpdateJob job = new("GitA", "W1", output =>
        {
            output.WriteLine("text", color);
            return EPackageUpdateResult.Success;
        });

        // Act
        new OutdatedPackagesParallelUpdater(logger.Object, null).Run([job], CancellationToken.None);

        // Assert
        //a Moq expression: the arguments only describe the expected call, nothing is evaluated for logging
#pragma warning disable CA1873
        logger.Verify(
            x => x.Log(logLevel, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) => value!.ToString() == "GitA: text"), It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
#pragma warning restore CA1873
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current = Volatile.Read(ref target);
        while (value > current)
        {
            int previous = Interlocked.CompareExchange(ref target, value, current);
            if (previous == current)
            {
                return;
            }

            current = previous;
        }
    }
}

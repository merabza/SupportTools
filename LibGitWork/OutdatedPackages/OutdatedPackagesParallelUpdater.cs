using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitWork.SyncV3;
using Microsoft.Extensions.Logging;

namespace LibGitWork.OutdatedPackages;

//ერთი დონის git-ების განახლება: მაქსიმუმ MaxParallelism ერთდროულად, V3-ის დაფით - თითო სტრიქონი თითო მიმდინარე
//git-ისთვის, რომელშიც მისი ნაბიჯი თავის ადგილზე იცვლება; შედეგები და შეცდომები დაფის ზემოთ რჩება. ერთი ფოლდერის
//(პროექტის workspace-ის) git-ები ერთმანეთის მიყოლებით ახლდება: მათი restore-ები მეზობელი პროექტების საერთო
//obj-ფაილებს წერს
internal sealed class OutdatedPackagesParallelUpdater
{
    //ერთდროულად მაქსიმუმ ამდენი git ახლდება (მომხმარებლის არჩევანი)
    internal const int MaxParallelism = 8;

    private readonly ILogger? _logger;
    private readonly IProgressConsole? _progressConsole;

    //progressConsole=null: დაფის გარეშე, მხოლოდ სტრიქონები (გადამისამართებული გამოტანა, ტესტები)
    // ReSharper disable once ConvertToPrimaryConstructor
    public OutdatedPackagesParallelUpdater(ILogger? logger, IProgressConsole? progressConsole)
    {
        _logger = logger;
        _progressConsole = progressConsole;
    }

    //შედეგები სამუშაოების რიგით
    public EPackageUpdateResult[] Run(IReadOnlyList<PackageUpdateJob> jobs, CancellationToken cancellationToken)
    {
        var results = new EPackageUpdateResult[jobs.Count];
        List<int[]> folderGroups =
        [
            .. Enumerable.Range(0, jobs.Count)
                .GroupBy(x => jobs[x].WorkspaceFolder, StringComparer.OrdinalIgnoreCase).Select(x => x.ToArray())
        ];
        int doneCount = 0;
        using (var board = new ParallelProgressBoard(_progressConsole))
        {
            board.SetHeader(Header(jobs.Count, 0));
            Parallel.ForEach(folderGroups,
                new ParallelOptions { MaxDegreeOfParallelism = MaxParallelism, CancellationToken = cancellationToken },
                group =>
                {
                    foreach (int index in group)
                    {
                        //გაუქმებისას ფოლდერის დარჩენილი git-ები აღარ იწყება
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        PackageUpdateJob job = jobs[index];
                        using (ProgressBoardLine line = board.StartLine($"{job.GitName}  start"))
                        {
                            results[index] = job.Run(new BoardOutput(board, line, job.GitName, _logger));
                        }

                        board.SetHeader(Header(jobs.Count, Interlocked.Increment(ref doneCount)));
                    }
                });
        }

        //გაუქმებისას Parallel.ForEach თავად აგდებს OperationCanceledException-ს
        return results;
    }

    private static string Header(int count, int doneCount)
    {
        return string.Create(CultureInfo.InvariantCulture, $"Updating packages of {count} gits: {doneCount} done");
    }

    //git-ის ნაბიჯი დაფის მის სტრიქონში იწერება, შეტყობინებები კი git-ის სახელით დაფის ზემოთ
    private sealed class BoardOutput : IPackageUpdateOutput
    {
        private readonly ParallelProgressBoard _board;
        private readonly string _gitName;
        private readonly ProgressBoardLine _line;
        private readonly ILogger? _logger;

        // ReSharper disable once ConvertToPrimaryConstructor
        public BoardOutput(ParallelProgressBoard board, ProgressBoardLine line, string gitName, ILogger? logger)
        {
            _board = board;
            _line = line;
            _gitName = gitName;
            _logger = logger;
        }

        public void SetStatus(string status)
        {
            _line.SetText($"{_gitName}  {status}");
        }

        public void WriteLine(string text, ConsoleColor? color)
        {
            string line = $"{_gitName}: {text}";
            _board.WriteLine(line, color);
            if (_logger is null)
            {
                return;
            }

            LogLevel logLevel = color switch
            {
                ConsoleColor.Red => LogLevel.Error,
                ConsoleColor.Yellow => LogLevel.Warning,
                _ => LogLevel.Information
            };
            if (_logger.IsEnabled(logLevel))
            {
                _logger.Log(logLevel, "{Text}", line);
            }
        }
    }
}

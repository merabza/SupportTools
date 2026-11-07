using System.Threading;
using Microsoft.Extensions.Logging;
using SystemTools.SharedKernel;

namespace LibDotnetWork;

//StShared.RunProcess-ის სიგნატურა: DotnetProcessor-ის ტესტები ბრძანებებს არ უშვებენ, მათ არგუმენტებს იწერენ
internal delegate Result RunProcessFunc(bool useConsole, ILogger? logger, string programFileName, string arguments,
    int[]? allowExitCodes = null, bool useErrorLine = true, int waitForExit = Timeout.Infinite);

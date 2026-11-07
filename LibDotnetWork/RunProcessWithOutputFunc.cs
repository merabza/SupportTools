using Microsoft.Extensions.Logging;
using SystemTools.SharedKernel;

namespace LibDotnetWork;

//StShared.RunProcessWithOutput-ის სიგნატურა: DotnetProcessor-ის ტესტები ბრძანებებს არ უშვებენ, მათ არგუმენტებს
//იწერენ და გამოტანას თვითონ აწვდიან
internal delegate Result<(string, int)> RunProcessWithOutputFunc(bool useConsole, ILogger? logger,
    string programFileName, string arguments, int[]? allowExitCodes = null, bool useErrorLine = true);

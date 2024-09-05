//using System.Diagnostics;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using System.Threading;
//using System;
//using System.Linq;

//namespace LibAppProjectCreator.AppCreators;

//public class NpmScript : IDisposable
//{
//    private readonly string _scriptName;

//    private static readonly Regex Urls =
//        new(
//            "(ht|f)tp(s?)\\:\\/\\/[0-9a-zA-Z]([-.\\w]*[0-9a-zA-Z])*(:(0-9)*)*(\\/?)([a-zA-Z0-9\\-\\.\\?\\,\\'\\/\\\\\\+&%\\$#_]*)?",
//            RegexOptions.IgnoreCase | RegexOptions.Compiled);

//    private Process? _process;
//    public string Url { get; private set; }
//    public bool HasServer => !string.IsNullOrEmpty(Url);

//    public int ProcessId => _process?.Id ?? 0;

//    private readonly TaskCompletionSource<bool> _signal = new TaskCompletionSource<bool>(false);

//    /// <summary>
//    /// Will execute a script name with default value of "dev"
//    /// i.e. npm run [script name]
//    /// </summary>
//    /// <param name="scriptName"></param>
//    public NpmScript(string scriptName = "dev")
//    {
//        _scriptName = scriptName;
//    }

//    /// <summary>
//    /// Will wait for npm to start up and retrieve the first url from the output.
//    /// **Only use this to run the development server.**
//    /// </summary>
//    /// <param name="output"></param>
//    /// <param name="timeout">In milliseconds</param>
//    /// <returns></returns>
//    public async Task RunAsync(Action<string>? output = null, int timeout = 2000)
//    {
//        lock (_signal)
//        {
//            if (_process == null)
//            {
//                var info = new ProcessStartInfo("npm")
//                {
//                    RedirectStandardOutput = true,
//                    RedirectStandardError = true,
//                    Arguments = $"run {_scriptName}",
//                    UseShellExecute = false,
//                };

//                _process = Process.Start(info);
//                _process.EnableRaisingEvents = true;
//                _process.BeginOutputReadLine();
//                _process.BeginErrorReadLine();

//                // Process the NPM output and attempt
//                // to find a URL. This will stop processing
//                // when it finds the first URL
//                _process.OutputDataReceived += (sender, eventArgs) =>
//                {
//                    output?.Invoke(eventArgs.Data);

//                    if (!string.IsNullOrEmpty(eventArgs.Data) && string.IsNullOrEmpty(Url))
//                    {
//                        var results = Urls.Matches(eventArgs.Data);

//                        if (results.Any())
//                        {
//                            Url = results.First().Value;
//                            _signal.SetResult(true);
//                        }
//                    }
//                };

//                // Terrible things have happened
//                // so we can stop waiting for the success
//                // event to occur, because it ain't happening
//                _process.ErrorDataReceived += (sender, args) =>
//                {
//                    output?.Invoke(args.Data);

//                    if (!_signal.Task.IsCompleted)
//                    {
//                        _signal.SetException(new Exception("npm web server failed to start"));
//                    }
//                };

//                // set a timeout to wait for the process
//                // to finish starting and find the Url. If it doesn't then we
//                // assume that the user just ran a script
//                var cancellationTokenSource = new CancellationTokenSource(timeout);
//                cancellationTokenSource.Token.Register(() =>
//                {
//                    if (_signal.Task.IsCompleted)
//                        return;

//                    // we don't want to wait for a url anymore
//                    Url = string.Empty;
//                    _signal.SetResult(true);
//                }, false);
//            }
//        }

//        await _signal.Task;
//    }

//    public void Dispose()
//    {
//        _process?.Dispose();
//    }
//}


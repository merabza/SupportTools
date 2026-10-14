using SystemTools.SharedKernel;

namespace LibGitWork.SyncV3;

internal static class GitSyncV3Errors
{
    //აღწერა git-ის ბრძანებას და მის შეცდომის ტექსტს (stderr) შეიცავს: კონსოლზე ის მხოლოდ ასე ჩანს
    public static Error GitCommandFailed(string commandLine, int exitCode, string errorOutput)
    {
        return Error.Problem(nameof(GitCommandFailed),
            $"{commandLine} failed (ExitCode={exitCode}){(errorOutput.Length == 0 ? string.Empty : $": {errorOutput}")}");
    }
}

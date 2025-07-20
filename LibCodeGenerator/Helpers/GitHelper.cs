using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibCodeGenerator.Helpers;

public static class GitHelper
{
    public static async Task<bool> HasUncommittedChangesAsync(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
            throw new ArgumentException("Invalid project path.", nameof(projectPath));

        var result = await RunGitCommandAsync("status --porcelain", projectPath);
        return !string.IsNullOrWhiteSpace(result);
    }

    public static async Task<bool> UpdateTestFolderAsync(string testFolderPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(testFolderPath) || !Directory.Exists(testFolderPath))
            throw new ArgumentException("Invalid test folder path.", nameof(testFolderPath));

        // Pull latest changes
        var pullResult = await RunGitCommandAsync("pull", testFolderPath, cancellationToken);
        return !pullResult.Contains("error", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<string> GetFileDiffAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new ArgumentException("Invalid file path.", nameof(filePath));

        var directory = Path.GetDirectoryName(filePath)!;
        var fileName = Path.GetFileName(filePath);

        // Show diff for the file
        var diffResult = await RunGitCommandAsync($"diff {fileName}", directory);
        return diffResult;
    }

    private static async Task<string> RunGitCommandAsync(string arguments, string workingDirectory, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (!string.IsNullOrEmpty(error))
            return error + Environment.NewLine + output;

        return output;
    }
}
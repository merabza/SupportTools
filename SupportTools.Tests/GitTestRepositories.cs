using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xunit;

namespace SupportTools.Tests;

//throwaway git repositories for the tests that run the real git: a bare "remote" (a local path, so no network is
//needed) made from a seed repository with the given files
internal static class GitTestRepositories
{
    //files: the path relative to the repository => the content; returns the path of the bare repository
    public static string CreateRemote(string rootFolder, string name, IReadOnlyDictionary<string, string> files)
    {
        string seed = InitRepository(Path.Combine(rootFolder, "Seeds", name));
        foreach ((string relativePath, string content) in files)
        {
            string filePath = Path.Combine(seed, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, content);
        }

        RunGit("-C", seed, "add", "--all");
        RunGit("-C", seed, "commit", "-m", "Initial");
        string remote = Path.Combine(rootFolder, "Remotes", $"{name}.git");
        RunGit("clone", "--bare", seed, remote);
        return remote;
    }

    public static string InitRepository(string repository)
    {
        RunGit("init", "--initial-branch=main", repository);
        ConfigureRepository(repository);
        return repository;
    }

    //the tests must not depend on the global git configuration of the machine
    public static void ConfigureRepository(string repository)
    {
        RunGit("-C", repository, "config", "user.name", "Test User");
        RunGit("-C", repository, "config", "user.email", "test.user@example.com");
        RunGit("-C", repository, "config", "commit.gpgsign", "false");
        RunGit("-C", repository, "config", "pull.rebase", "true");
    }

    public static string RunGit(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var errorOutput = new StringBuilder();
        using var git = new Process();
        git.StartInfo = startInfo;
        git.ErrorDataReceived += (_, e) => errorOutput.AppendLine(e.Data);
        git.Start();
        git.BeginErrorReadLine();
        string output = git.StandardOutput.ReadToEnd();
        git.WaitForExit();
        Assert.True(git.ExitCode == 0, $"git {string.Join(' ', arguments)} failed: {errorOutput}");
        return output.Trim();
    }

    //git keeps its object files read-only, Directory.Delete does not remove such files
    public static void DeleteFolder(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (string filePath in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
        }

        Directory.Delete(folder, true);
    }
}

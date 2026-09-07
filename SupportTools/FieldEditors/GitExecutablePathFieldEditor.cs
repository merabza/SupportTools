using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibMenuInput;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace SupportTools.FieldEditors;

//git პროგრამის გამშვები ფაილის გზის რედაქტორი
public sealed class GitExecutablePathFieldEditor : FilePathFieldEditor
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public GitExecutablePathFieldEditor(string propertyName) : base(propertyName)
    {
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        string? currentValue = GetValue(recordForUpdate);

        //თუ git-ის გზა შენახული არ არის, ვცდილობთ ავტომატურად დადგენას და default-ად შეთავაზებას
        if (string.IsNullOrWhiteSpace(currentValue))
        {
            currentValue = DetectGitExecutablePath();
        }

        SetValue(recordForUpdate, MenuInputer.InputFilePath(FieldName, currentValue));
        return ValueTask.CompletedTask;
    }

    private static string? DetectGitExecutablePath()
    {
        Result<(string, int)> runProcessWithOutputResult = SystemStat.IsWindows()
            ? StShared.RunProcessWithOutput(false, null, "powershell",
                "-NoProfile -Command \"(Get-Command git).Source\"")
            : StShared.RunProcessWithOutput(false, null, "which", "git");

        if (runProcessWithOutputResult.IsFailure)
        {
            return null;
        }

        string gitPath = runProcessWithOutputResult.Value.Item1.Trim('\0', ' ', '\t', '\r', '\n');
        return !string.IsNullOrWhiteSpace(gitPath) && File.Exists(gitPath) ? gitPath : null;
    }
}

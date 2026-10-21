using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;

//ProjectReference-ის პაკეტით ჩანაცვლება. dotnet add package-ის restore NuGet-ის HTTP ქეშს (30 წუთი) იყენებს და
//ამავე გაშვებაში ატვირთულ დამოკიდებულებას ვერ ხედავს (NU1102, რასაც "incompatible with 'all' frameworks" მოსდევს),
//ამიტომ პაკეტი restore-ის გარეშე ჩაიწერება, restore კი ცალკე, ქეშის გარეშე ეშვება.
//ჩავარდნისას ჩასმული პაკეტი იშლება და წაშლილი რეფერენსი უბრუნდება პროექტს, რომ შეცვლილი პროექტი შეცდომაზე არ გავიდეს
internal sealed class ProjectReferenceReplacer
{
    private readonly IPackageReferenceCommands _commands;
    private readonly ILogger? _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectReferenceReplacer(IPackageReferenceCommands commands, ILogger? logger)
    {
        _commands = commands;
        _logger = logger;
    }

    public bool Replace(string csprojFile, string refFullPath, string packageId, string packageVersion)
    {
        if (_commands.RemoveReferenceFromProject(csprojFile, refFullPath).IsFailure)
        {
            StShared.WriteErrorLine($"Cannot remove reference {refFullPath} from {csprojFile}", true, _logger);
            return false;
        }

        if (_commands.AddPackageToProjectWithoutRestore(csprojFile, packageId, packageVersion).IsFailure)
        {
            RestoreReference(csprojFile, refFullPath, $"Cannot add package {packageId} to {csprojFile}");
            return false;
        }

        if (_commands.RestoreWithoutHttpCache(csprojFile).IsSuccess)
        {
            return true;
        }

        string failure = $"Cannot restore {csprojFile} with package {packageId} {packageVersion}";
        if (_commands.RemovePackageFromProject(csprojFile, packageId).IsFailure)
        {
            StShared.WriteErrorLine(
                $"{failure} and cannot remove the package and restore removed reference {refFullPath}. Recover the file from git",
                true, _logger);
            return false;
        }

        RestoreReference(csprojFile, refFullPath, failure);
        return false;
    }

    private void RestoreReference(string csprojFile, string refFullPath, string failure)
    {
        if (_commands.AddReferenceToProject(csprojFile, refFullPath).IsFailure)
        {
            StShared.WriteErrorLine(
                $"{failure} and cannot restore removed reference {refFullPath}. Recover the file from git", true,
                _logger);
            return;
        }

        StShared.WriteErrorLine($"{failure}. Removed reference was restored", true, _logger);
    }
}

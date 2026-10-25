using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ServerInstallations;

//grub.cfg და autoinstall.yaml ფოლდერში იწერება: FilesFolder, ან {WorkFolder}\ServerInstallations\{სახელი}
public sealed class GenerateServerInstallationFilesCliMenuCommand : CliMenuCommand
{
    private readonly string _installationName;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GenerateServerInstallationFilesCliMenuCommand(IParametersManager parametersManager,
        string installationName) : base("Generate grub.cfg And autoinstall.yaml", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
        _installationName = installationName;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ServerInstallationModel? installation = GetValidInstallation(parameters, _installationName);
        if (installation is null)
        {
            return ValueTask.FromResult(false);
        }

        string? folder = installation.FilesFolder;
        if (string.IsNullOrWhiteSpace(folder) && !string.IsNullOrWhiteSpace(parameters.WorkFolder))
        {
            folder = Path.Combine(parameters.WorkFolder, "ServerInstallations", _installationName);
        }

        if (string.IsNullOrWhiteSpace(folder))
        {
            StShared.WriteErrorLine(
                $"{nameof(ServerInstallationModel.FilesFolder)} and {nameof(SupportToolsParameters.WorkFolder)} are empty",
                true);
            return ValueTask.FromResult(false);
        }

        Directory.CreateDirectory(folder);
        string grubCfgPath = Path.Combine(folder, ServerInstallationFilesGenerator.GrubCfgFileName);
        string autoinstallPath = Path.Combine(folder, ServerInstallationFilesGenerator.AutoinstallFileName);
        InstallationUsb.WriteLinuxText(grubCfgPath, ServerInstallationFilesGenerator.CreateGrubCfg(installation));
        InstallationUsb.WriteLinuxText(autoinstallPath,
            ServerInstallationFilesGenerator.CreateAutoinstallYaml(installation));

        Console.WriteLine($"Created {grubCfgPath}");
        Console.WriteLine($"Created {autoinstallPath}");
        StShared.Pause();
        return ValueTask.FromResult(true);
    }

    //შეცდომები ყველა ერთად იბეჭდება, რომ ერთი რედაქტირებით გასწორდეს
    internal static ServerInstallationModel? GetValidInstallation(SupportToolsParameters parameters,
        string installationName)
    {
        if (!parameters.ServerInstallations.TryGetValue(installationName, out ServerInstallationModel? installation))
        {
            StShared.WriteErrorLine($"Server Installation {installationName} not found", true);
            return null;
        }

        List<string> errors = ServerInstallationFilesGenerator.Validate(installation);
        if (errors.Count == 0)
        {
            return installation;
        }

        foreach (string error in errors)
        {
            StShared.WriteErrorLine(error, true, null, false);
        }

        StShared.Pause();
        return null;
    }
}

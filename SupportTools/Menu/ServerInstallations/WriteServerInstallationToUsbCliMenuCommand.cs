using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ServerInstallations;

public sealed class WriteServerInstallationToUsbCliMenuCommand : CliMenuCommand
{
    private readonly Func<string, bool> _confirm;
    private readonly Func<List<InstallationUsbDrive>> _findDrives;
    private readonly Func<string, CliMenuSet, string?, string?> _inputFromMenuList;
    private readonly string _installationName;
    private readonly IParametersManager _parametersManager;

    public WriteServerInstallationToUsbCliMenuCommand(IParametersManager parametersManager,
        string installationName) : this(parametersManager, installationName, InstallationUsb.FindDrives,
        MenuInputer.InputFromMenuList, Confirm)
    {
    }

    //დისკების ძებნა და კონსოლიდან შეყვანა პარამეტრადაა გამოტანილი, რომ ტესტებმა ისინი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal WriteServerInstallationToUsbCliMenuCommand(IParametersManager parametersManager,
        string installationName, Func<List<InstallationUsbDrive>> findDrives,
        Func<string, CliMenuSet, string?, string?> inputFromMenuList, Func<string, bool> confirm) : base(
        "Write To Installation USB...", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
        _installationName = installationName;
        _findDrives = findDrives;
        _inputFromMenuList = inputFromMenuList;
        _confirm = confirm;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ServerInstallationModel? installation =
            GenerateServerInstallationFilesCliMenuCommand.GetValidInstallation(parameters, _installationName);
        if (installation is null)
        {
            return ValueTask.FromResult(false);
        }

        List<InstallationUsbDrive> drives = _findDrives();
        if (drives.Count == 0)
        {
            StShared.WriteErrorLine(
                "No Ubuntu installation USB found (a local or RDP redirected drive with boot\\grub\\grub.cfg and a casper folder)",
                true);
            return ValueTask.FromResult(false);
        }

        var drivesMenuSet = new CliMenuSet();
        foreach (InstallationUsbDrive drive in drives)
        {
            drivesMenuSet.AddMenuItem(new CliMenuCommand(drive.Description));
        }

        string? selected = _inputFromMenuList("Installation USB", drivesMenuSet, null);
        InstallationUsbDrive? selectedDrive = drives.FirstOrDefault(x => x.Description == selected);
        if (selectedDrive is null)
        {
            return ValueTask.FromResult(false);
        }

        string root = selectedDrive.RootPath;
        Console.WriteLine(
            $"A computer booted from {root} installs Ubuntu without asking and erases the disk with serial {installation.InstallDiskSerial}.");
        if (!_confirm($"Write grub.cfg and autoinstall.yaml of {_installationName} to {root}?"))
        {
            return ValueTask.FromResult(false);
        }

        InstallationUsb.Write(root, installation);
        StShared.WriteSuccessMessage($"Written to {root}. Remove autoinstall.yaml from the USB after the installation");
        StShared.Pause();
        return ValueTask.FromResult(true);
    }

    //Enter არაფერს წერს
    internal static bool Confirm(string question)
    {
        return Inputer.InputBool(question, false, false);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibMenuInput;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ServerInstallations;

//AutoinstallUserDataFilePath-იდან InstallDiskSerial-სა და NetworkInterfaceName-ს ავსებს. რამდენიმე დისკის ან
//ინტერფეისის შემთხვევაში არჩევას სთავაზობს
public sealed class ReadFromAutoinstallUserDataCliMenuCommand : CliMenuCommand
{
    private readonly Func<string, string?, string?> _inputFilePath;
    private readonly Func<string, CliMenuSet, string?, string?> _inputFromMenuList;
    private readonly string _installationName;
    private readonly IParametersManager _parametersManager;

    public ReadFromAutoinstallUserDataCliMenuCommand(IParametersManager parametersManager, string installationName) :
        this(parametersManager, installationName, InputFilePath, MenuInputer.InputFromMenuList)
    {
    }

    //კონსოლიდან შეყვანა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხები თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal ReadFromAutoinstallUserDataCliMenuCommand(IParametersManager parametersManager, string installationName,
        Func<string, string?, string?> inputFilePath, Func<string, CliMenuSet, string?, string?> inputFromMenuList) :
        base("Read Disk And Network From autoinstall-user-data...", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
        _installationName = installationName;
        _inputFilePath = inputFilePath;
        _inputFromMenuList = inputFromMenuList;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        if (!parameters.ServerInstallations.TryGetValue(_installationName, out ServerInstallationModel? installation))
        {
            StShared.WriteErrorLine($"Server Installation {_installationName} not found", true);
            return false;
        }

        string? filePath = installation.AutoinstallUserDataFilePath;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            filePath = _inputFilePath(nameof(ServerInstallationModel.AutoinstallUserDataFilePath), filePath);
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                StShared.WriteErrorLine($"File {filePath} not found", true);
                return false;
            }
        }

        AutoinstallUserData userData =
            AutoinstallUserDataReader.Read(await File.ReadAllTextAsync(filePath, cancellationToken));

        installation.AutoinstallUserDataFilePath = filePath;
        List<string> diskDescriptions = [.. userData.Disks.Select(x => $"{x.Serial} {x.Path}".TrimEnd())];
        string? selectedDisk = Select("Install Disk", diskDescriptions, installation.InstallDiskSerial);
        int diskIndex = diskDescriptions.FindIndex(x => x == selectedDisk);
        if (diskIndex >= 0)
        {
            installation.InstallDiskSerial = userData.Disks[diskIndex].Serial;
            Console.WriteLine($"{nameof(ServerInstallationModel.InstallDiskSerial)}: {installation.InstallDiskSerial}");
        }

        string? interfaceName = Select("Network Interface", userData.EthernetNames,
            installation.NetworkInterfaceName);
        if (interfaceName is not null)
        {
            installation.NetworkInterfaceName = interfaceName;
            Console.WriteLine($"{nameof(ServerInstallationModel.NetworkInterfaceName)}: {interfaceName}");
        }

        bool saved = await _parametersManager.Save(parameters, $"Server Installation {_installationName} updated",
            null, cancellationToken);
        StShared.Pause();
        return saved;
    }

    //MenuInputer.InputFilePath-ს მესამე, არასავალდებულო პარამეტრიც აქვს, ამიტომ Func-ად პირდაპირ ვერ გადაეცემა
    internal static string? InputFilePath(string fieldName, string? defaultValue)
    {
        return MenuInputer.InputFilePath(fieldName, defaultValue);
    }

    private string? Select(string fieldName, List<string> values, string? currentValue)
    {
        switch (values.Count)
        {
            case 0:
                StShared.WriteWarningLine($"No {fieldName} found in the file", true);
                return null;
            case 1:
                return values[0];
            default:
                var menuSet = new CliMenuSet();
                foreach (string value in values)
                {
                    menuSet.AddMenuItem(new CliMenuCommand(value));
                }

                return _inputFromMenuList(fieldName, menuSet,
                    values.FirstOrDefault(x =>
                        currentValue is not null && x.StartsWith(currentValue, StringComparison.Ordinal)));
        }
    }
}

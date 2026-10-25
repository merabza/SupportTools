using System.Collections.Generic;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.FieldEditors;
using ParametersManagement.LibParameters;
using SupportTools.FieldEditors;
using SupportTools.Menu.ServerInstallations;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Cruders;

public sealed class ServerInstallationCruder : ParCruder<ServerInstallationModel>
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ServerInstallationCruder(IParametersManager parametersManager,
        Dictionary<string, ServerInstallationModel> currentValuesDictionary) : base(parametersManager,
        currentValuesDictionary, "Server Installation", "Server Installations")
    {
        FieldEditors.Add(new TextFieldEditor(nameof(ServerInstallationModel.Hostname)));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerInstallationModel.DomainName)));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerInstallationModel.RealName)));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerInstallationModel.UserName)));
        FieldEditors.Add(new PasswordHashFieldEditor(nameof(ServerInstallationModel.PasswordHash),
            PasswordHashers.LinuxSha512Crypt));
        FieldEditors.Add(new SimpleNamesListFieldEditor<ServerInstallationListCruder>(
            nameof(ServerInstallationModel.SshAuthorizedKeys),
            x => new ServerInstallationListCruder(parametersManager, x, "SSH Authorized Key", "SSH Authorized Keys")));
        FieldEditors.Add(new BoolFieldEditor(nameof(ServerInstallationModel.SshAllowPassword)));

        FieldEditors.Add(new TextFieldEditor(nameof(ServerInstallationModel.NetworkInterfaceName)));
        FieldEditors.Add(new SimpleNamesListFieldEditor<ServerInstallationListCruder>(
            nameof(ServerInstallationModel.IpV4Addresses),
            x => new ServerInstallationListCruder(parametersManager, x, "IPv4 Address", "IPv4 Addresses")));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerInstallationModel.Gateway)));
        FieldEditors.Add(new SimpleNamesListFieldEditor<ServerInstallationListCruder>(
            nameof(ServerInstallationModel.NameServers),
            x => new ServerInstallationListCruder(parametersManager, x, "Name Server", "Name Servers")));

        FieldEditors.Add(new TextFieldEditor(nameof(ServerInstallationModel.Timezone),
            ServerInstallationModel.DefaultTimezone));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerInstallationModel.Locale),
            ServerInstallationModel.DefaultLocale));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerInstallationModel.KeyboardLayout),
            ServerInstallationModel.DefaultKeyboardLayout));
        FieldEditors.Add(new IntFieldEditor(nameof(ServerInstallationModel.GrubTimeoutSeconds),
            ServerInstallationModel.DefaultGrubTimeoutSeconds, true));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ServerInstallationModel.AutoinstallUserDataFilePath)));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerInstallationModel.InstallDiskSerial)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(ServerInstallationModel.FilesFolder)));

        FieldEditors.Add(new BoolFieldEditor(nameof(ServerInstallationModel.InstallVirtualmin)));
        FieldEditors.Add(new BoolFieldEditor(nameof(ServerInstallationModel.InstallWebmin)));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerInstallationModel.DnsPrimaryNameServer)));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerInstallationModel.EmailToAddress)));
        FieldEditors.Add(new BoolFieldEditor(nameof(ServerInstallationModel.InstallMariaDb)));
        FieldEditors.Add(new PasswordHashFieldEditor(nameof(ServerInstallationModel.MariaDbRootPasswordHash),
            PasswordHashers.MariaDbNativePassword));
        FieldEditors.Add(new BoolFieldEditor(nameof(ServerInstallationModel.InstallPostgreSql)));
        FieldEditors.Add(new BoolFieldEditor(nameof(ServerInstallationModel.InstallMsSqlServer)));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerInstallationModel.SqlServerEdition),
            ServerInstallationModel.DefaultSqlServerEdition));
        FieldEditors.Add(new PasswordHashFieldEditor(nameof(ServerInstallationModel.SqlServerSaPasswordHash),
            PasswordHashers.SqlServerPassword));
        FieldEditors.Add(new IntFieldEditor(nameof(ServerInstallationModel.SqlServerPort), 0, true));
        FieldEditors.Add(new BoolFieldEditor(nameof(ServerInstallationModel.InstallSqlServerClient)));
    }

    //ველი ჩანს მხოლოდ მაშინ, როცა მას რამე იყენებს: Virtualmin-ს Webmin თვითონ მოაქვს, SQL Server-ს კი sqlcmd
    protected override void CheckFieldsEnables(ItemData itemData, string? lastEditedFieldName = null)
    {
        var installation = (ServerInstallationModel)itemData;
        EnableFieldByName(nameof(ServerInstallationModel.InstallWebmin), !installation.InstallVirtualmin);
        EnableFieldByName(nameof(ServerInstallationModel.DnsPrimaryNameServer), installation.InstallVirtualmin);
        EnableFieldByName(nameof(ServerInstallationModel.EmailToAddress), installation.InstallVirtualmin);
        EnableFieldByName(nameof(ServerInstallationModel.MariaDbRootPasswordHash), installation.InstallMariaDb);
        EnableFieldByName(nameof(ServerInstallationModel.SqlServerEdition), installation.InstallMsSqlServer);
        EnableFieldByName(nameof(ServerInstallationModel.SqlServerSaPasswordHash), installation.InstallMsSqlServer);
        EnableFieldByName(nameof(ServerInstallationModel.SqlServerPort), installation.InstallMsSqlServer);
        EnableFieldByName(nameof(ServerInstallationModel.InstallSqlServerClient), !installation.InstallMsSqlServer);
    }

    public override string? GetStatusFor(string name)
    {
        var installation = (ServerInstallationModel?)GetItemByName(name);
        return installation is null || string.IsNullOrWhiteSpace(installation.Hostname)
            ? null
            : ServerInstallationFilesGenerator.GetFqdn(installation);
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string itemName)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, itemName);

        itemSubMenuSet.AddMenuItem(
            new ReadFromAutoinstallUserDataCliMenuCommand(ParametersManager, itemName));
        itemSubMenuSet.AddMenuItem(new GenerateServerInstallationFilesCliMenuCommand(ParametersManager, itemName));
        itemSubMenuSet.AddMenuItem(new WriteServerInstallationToUsbCliMenuCommand(ParametersManager, itemName));
    }

    public static ServerInstallationCruder Create(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        return new ServerInstallationCruder(parametersManager, parameters.ServerInstallations);
    }
}

using CliParameters;
using CliParameters.FieldEditors;
using CliParametersEdit.FieldEditors;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using ToolsManagement.Installer.Models;

namespace SupportTools.ParametersEditors;

public sealed class InstallerSettingsParametersEditor : ParametersEditor
{
    public InstallerSettingsParametersEditor(ILogger logger, IParameters parameters,
        IParametersManager parametersManager) : base("Installer Settings Editor", parameters, parametersManager)
    {
        FieldEditors.Add(new FolderPathFieldEditor(nameof(InstallerSettings.InstallerWorkFolder)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(InstallerSettings.InstallFolder)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(InstallerSettings.DotnetRunner)));
        FieldEditors.Add(new TextFieldEditor(nameof(InstallerSettings.ProgramArchiveDateMask)));
        FieldEditors.Add(new TextFieldEditor(nameof(InstallerSettings.ProgramArchiveExtension)));
        FieldEditors.Add(new TextFieldEditor(nameof(InstallerSettings.ParametersFileDateMask)));
        FieldEditors.Add(new TextFieldEditor(nameof(InstallerSettings.ParametersFileExtension)));
        FieldEditors.Add(new FileStorageNameFieldEditor(logger,
            nameof(InstallerSettings.ProgramExchangeFileStorageName), parametersManager));
        FieldEditors.Add(new TextFieldEditor(nameof(InstallerSettings.ServiceUserName)));
        FieldEditors.Add(new TextFieldEditor(nameof(InstallerSettings.DownloadTempExtension)));
        FieldEditors.Add(new TextFieldEditor(nameof(InstallerSettings.FilesUserName)));
        FieldEditors.Add(new TextFieldEditor(nameof(InstallerSettings.FilesUsersGroupName)));
    }
}
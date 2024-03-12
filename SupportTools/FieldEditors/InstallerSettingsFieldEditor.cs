using CliMenu;
using CliParameters.FieldEditors;
using Installer.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.ParametersEditors;

namespace SupportTools.FieldEditors;

public sealed class InstallerSettingsFieldEditor : FieldEditor<InstallerSettings>
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public InstallerSettingsFieldEditor(ILogger logger, string propertyName, ParametersManager parametersManager) :
        base(propertyName, false, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    public override string GetValueStatus(object? record)
    {
        var val = GetValue(record);
        return val == null ? "(empty)" : "(some parameters)";
    }

    public override CliMenuSet GetSubMenu(object record)
    {
        var currentInstallerSettings = GetValue(record);
        if (currentInstallerSettings is null)
        {
            currentInstallerSettings = new InstallerSettings();
            SetValue(record, currentInstallerSettings);
        }

        var installerSettingsParametersEditor =
            new InstallerSettingsParametersEditor(_logger, currentInstallerSettings, _parametersManager);
        var foldersSet = installerSettingsParametersEditor.GetParametersMainMenu();
        return foldersSet;
    }
}
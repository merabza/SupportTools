using CliParameters.FieldEditors;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.ParametersEditors;
using ToolsManagement.Installer.Models;

namespace SupportTools.FieldEditors;

public sealed class
    InstallerSettingsFieldEditor : ParametersFieldEditor<InstallerSettings, InstallerSettingsParametersEditor>
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public InstallerSettingsFieldEditor(string propertyName, ILogger logger, IParametersManager parametersManager) :
        base(propertyName, logger, parametersManager)
    {
    }

    protected override InstallerSettingsParametersEditor CreateEditor(object record, InstallerSettings currentValue)
    {
        return new InstallerSettingsParametersEditor(Logger, currentValue, ParametersManager);
    }
}
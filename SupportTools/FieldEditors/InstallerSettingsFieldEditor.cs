using CliParameters.FieldEditors;
using Installer.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.ParametersEditors;

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
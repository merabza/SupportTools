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
    public InstallerSettingsFieldEditor(ILogger logger, string propertyName, IParametersManager parametersManager) :
        base(logger, propertyName, parametersManager)
    {
    }

    protected override InstallerSettingsParametersEditor CreateEditor(InstallerSettings currentValue)
    {
        return new InstallerSettingsParametersEditor(Logger, currentValue, ParametersManager);
    }
}
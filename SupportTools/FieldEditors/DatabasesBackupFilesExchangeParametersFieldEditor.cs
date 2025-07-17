using CliParameters.FieldEditors;
using LibDatabaseParameters;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.ParametersEditors;

namespace SupportTools.FieldEditors;

public sealed class DatabasesBackupFilesExchangeParametersFieldEditor : ParametersFieldEditor<
    DatabasesBackupFilesExchangeParameters, DatabasesBackupFilesExchangeParametersEditor>
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabasesBackupFilesExchangeParametersFieldEditor(string propertyName, ILogger logger,
        IParametersManager parametersManager) : base(propertyName, logger, parametersManager)
    {
    }

    protected override DatabasesBackupFilesExchangeParametersEditor CreateEditor(object record,
        DatabasesBackupFilesExchangeParameters currentValue)
    {
        return new DatabasesBackupFilesExchangeParametersEditor(Logger, currentValue, ParametersManager);
    }
}
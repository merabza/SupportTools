using CliMenu;
using CliParameters.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.ParametersEditors;
using SupportToolsData.Models;

namespace SupportTools.FieldEditors;

//DatabasesBackupFilesExchangeParametersFieldEditor
public sealed class
    DatabasesBackupFilesExchangeParametersFieldEditor : FieldEditor<DatabasesBackupFilesExchangeParameters>
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabasesBackupFilesExchangeParametersFieldEditor(ILogger logger, string propertyName,
        ParametersManager parametersManager) :
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
        var currentDatabasesBackupFilesExchangeParameters = GetValue(record);
        if (currentDatabasesBackupFilesExchangeParameters is null)
        {
            currentDatabasesBackupFilesExchangeParameters = new DatabasesBackupFilesExchangeParameters();
            SetValue(record, currentDatabasesBackupFilesExchangeParameters);
        }

        var databasesBackupFilesExchangeParametersFieldEditor =
            new DatabasesBackupFilesExchangeParametersEditor(_logger,
                currentDatabasesBackupFilesExchangeParameters, _parametersManager);
        var foldersSet = databasesBackupFilesExchangeParametersFieldEditor.GetParametersMainMenu();
        return foldersSet;
    }
}
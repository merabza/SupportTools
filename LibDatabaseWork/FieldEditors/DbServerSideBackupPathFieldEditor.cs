using CliParameters.FieldEditors;
using LibDatabaseParameters;
using LibMenuInput;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibDatabaseWork.FieldEditors;

public sealed class DbServerSideBackupPathFieldEditor : FieldEditor<string>
{
    private readonly string _databaseConnectionNamePropertyName;
    private readonly string _databaseWebAgentNamePropertyName;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DbServerSideBackupPathFieldEditor(string propertyName, IParametersManager parametersManager,
        string databaseWebAgentNamePropertyName, string databaseConnectionNamePropertyName) : base(propertyName)
    {
        _parametersManager = parametersManager;
        _databaseWebAgentNamePropertyName = databaseWebAgentNamePropertyName;
        _databaseConnectionNamePropertyName = databaseConnectionNamePropertyName;
    }

    public override void UpdateField(string? recordKey, object recordForUpdate) //, object currentRecord
    {
        var databaseWebAgentName = GetValue<string>(recordForUpdate, _databaseWebAgentNamePropertyName);
        var databaseConnectionName = GetValue<string>(recordForUpdate, _databaseConnectionNamePropertyName);
        var currentPath = GetValue(recordForUpdate);

        if (!string.IsNullOrWhiteSpace(databaseWebAgentName))
        {
            StShared.WriteWarningLine("Cannot set Db Server Side Backup Path, because Web Agent is used", true,
                null, true);
            return;
        }

        if (string.IsNullOrWhiteSpace(currentPath) && !string.IsNullOrWhiteSpace(databaseConnectionName))
        {
            var parameters = (SupportToolsParameters)_parametersManager.Parameters;
            var databaseServerConnections =
                new DatabaseServerConnections(parameters.DatabaseServerConnections);
            var databaseServerConnection =
                databaseServerConnections.GetDatabaseServerConnectionByKey(databaseConnectionName);
            currentPath = databaseServerConnection?.BackupFolderName;
        }

        var newValue = MenuInputer.InputFolderPath(FieldName, currentPath);

        SetValue(recordForUpdate, string.IsNullOrWhiteSpace(newValue) ? null : newValue);
    }
}
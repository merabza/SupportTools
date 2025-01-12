using DatabasesManagement;
using FileManagersMain;
using LibFileParameters.Models;

namespace LibDatabaseWork.Models;

public sealed class BackupRestoreParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public BackupRestoreParameters(IDatabaseManager databaseManager, FileManager fileManager, SmartSchema? smartSchema,
        string databaseName, string dbServerFoldersSetName)
    {
        DatabaseManager = databaseManager;
        FileManager = fileManager;
        SmartSchema = smartSchema;
        DatabaseName = databaseName;
        DbServerFoldersSetName = dbServerFoldersSetName;
    }

    public IDatabaseManager DatabaseManager { get; }
    public FileManager FileManager { get; }
    public SmartSchema? SmartSchema { get; }
    public string DatabaseName { get; }
    public string DbServerFoldersSetName { get; set; }
}
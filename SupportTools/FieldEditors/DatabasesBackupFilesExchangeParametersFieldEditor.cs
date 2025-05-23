﻿using CliParameters.FieldEditors;
using LibDatabaseParameters;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.ParametersEditors;

namespace SupportTools.FieldEditors;

public sealed class DatabasesBackupFilesExchangeParametersFieldEditor : ParametersFieldEditor<
    DatabasesBackupFilesExchangeParameters, DatabasesBackupFilesExchangeParametersEditor>
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabasesBackupFilesExchangeParametersFieldEditor(ILogger logger, string propertyName,
        IParametersManager parametersManager) : base(logger, propertyName, parametersManager)
    {
    }

    protected override DatabasesBackupFilesExchangeParametersEditor CreateEditor(object record,
        DatabasesBackupFilesExchangeParameters currentValue)
    {
        return new DatabasesBackupFilesExchangeParametersEditor(Logger, currentValue, ParametersManager);
    }
}
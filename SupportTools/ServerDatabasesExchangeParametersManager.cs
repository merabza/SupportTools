using CliParameters.FieldEditors;
using LibParameters;
using SupportToolsData.Models;

namespace SupportTools;

public sealed class ServerDatabasesExchangeParametersManager : IParametersManager
{
    private readonly FieldEditor<DatabasesExchangeParameters> _fieldEditor;
    private readonly IParametersManager _parentParametersManager;
    private readonly object _record;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServerDatabasesExchangeParametersManager(IParameters parameters,
        IParametersManager parentParametersManager, FieldEditor<DatabasesExchangeParameters> fieldEditor,
        object record)
    {
        _parentParametersManager = parentParametersManager;
        _fieldEditor = fieldEditor;
        _record = record;
        Parameters = parameters;
    }

    public IParameters Parameters { get; set; }

    public void Save(IParameters parameters, string message, string? saveAsFilePath = null)
    {
        Parameters = parameters;
        if (parameters is not DatabasesExchangeParameters par)
            return;

        _fieldEditor.SetValue(_record, par);

        _parentParametersManager.Save(_parentParametersManager.Parameters, message, saveAsFilePath);
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;

namespace SupportTools.FieldEditors;

public sealed class EditorConfigPatternNameFieldEditor : FieldEditor<string>
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly Func<string, string?, CancellationToken, ValueTask<string?>> _selectPatternName;

    //.editorconfig შაბლონის მითითება აუცილებელი არ არის, ამიტომ სიაში (None) ვარიანტიც არის
    public EditorConfigPatternNameFieldEditor(ILogger logger, string propertyName,
        IParametersManager parametersManager, bool enterFieldDataOnCreate = false) : this(logger, propertyName,
        parametersManager, enterFieldDataOnCreate,
        (fieldName, currentName, cancellationToken) => EditorConfigPatternsCruder.Create(logger, parametersManager)
            .GetNameWithPossibleNewName(fieldName, currentName, null, true, cancellationToken))
    {
    }

    //კონსოლიდან შეყვანა პარამეტრებადაა გამოტანილი, რომ ტესტებმა პასუხები თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal EditorConfigPatternNameFieldEditor(ILogger logger, string propertyName,
        IParametersManager parametersManager, bool enterFieldDataOnCreate,
        Func<string, string?, CancellationToken, ValueTask<string?>> selectPatternName) : base(propertyName,
        enterFieldDataOnCreate)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _selectPatternName = selectPatternName;
    }

    public override async ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        SetValue(recordForUpdate, await _selectPatternName(FieldName, GetValue(recordForUpdate), cancellationToken));
    }

    public override string GetValueStatus(object? record)
    {
        string? val = GetValue(record);

        if (val == null)
        {
            return string.Empty;
        }

        var editorConfigPatternsCruder = EditorConfigPatternsCruder.Create(_logger, _parametersManager);

        return $"{val} ({editorConfigPatternsCruder.GetStatusFor(val)})";
    }
}

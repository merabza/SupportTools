using CliParameters.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;

namespace SupportTools.FieldEditors;

public sealed class GitIgnorePathNameFieldEditor : FieldEditor<string>
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitIgnorePathNameFieldEditor(ILogger logger, string propertyName, IParametersManager parametersManager,
        bool enterFieldDataOnCreate = false) : base(propertyName, enterFieldDataOnCreate)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    public override void UpdateField(string? recordKey, object recordForUpdate)
    {
        var currentGitIgnorePathName = GetValue(recordForUpdate);

        var gitIgnorePathsCruder = new GitIgnoreFilePathsCruder(_logger, _parametersManager);

        SetValue(recordForUpdate, gitIgnorePathsCruder.GetNameWithPossibleNewName(FieldName, currentGitIgnorePathName));
    }

    public override string GetValueStatus(object? record)
    {
        var val = GetValue(record);

        if (val == null)
            return string.Empty;

        var gitIgnorePathsCruder = new GitIgnoreFilePathsCruder(_logger, _parametersManager);

        var status = gitIgnorePathsCruder.GetStatusFor(val);
        return $"{val} {(string.IsNullOrWhiteSpace(status) ? string.Empty : $"({status})")}";
    }
}
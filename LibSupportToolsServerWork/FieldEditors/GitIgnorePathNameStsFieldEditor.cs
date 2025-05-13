using System.Net.Http;
using CliParameters.FieldEditors;
using LibParameters;
using LibSupportToolsServerWork.Cruders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LibSupportToolsServerWork.FieldEditors;

public sealed class GitIgnorePathNameStsFieldEditor : FieldEditor<string>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitIgnorePathNameStsFieldEditor(ILogger logger, string propertyName, IParametersManager parametersManager,
        IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, bool enterFieldDataOnCreate = false) : base(
        propertyName, enterFieldDataOnCreate)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
    }

    public override void UpdateField(string? recordKey, object recordForUpdate)
    {
        var currentGitIgnorePathName = GetValue(recordForUpdate);

        var gitIgnorePathsCruder =
            new GitIgnoreFilePathsStsCruder(_logger, _httpClientFactory, _memoryCache, _parametersManager);

        SetValue(recordForUpdate, gitIgnorePathsCruder.GetNameWithPossibleNewName(FieldName, currentGitIgnorePathName));
    }

    public override string GetValueStatus(object? record)
    {
        var val = GetValue(record);

        if (val == null)
            return string.Empty;

        var gitIgnorePathsCruder =
            new GitIgnoreFilePathsStsCruder(_logger, _httpClientFactory, _memoryCache, _parametersManager);

        var status = gitIgnorePathsCruder.GetStatusFor(val);
        return $"{val} {(string.IsNullOrWhiteSpace(status) ? string.Empty : $"({status})")}";
    }
}
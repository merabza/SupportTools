using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using LibSupportToolsServerWork.Cruders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

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

    public override async ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        string? currentGitIgnorePathName = GetValue(recordForUpdate);

        var gitIgnorePathsCruder =
            new GitIgnoreFileTypesStsCruder(_logger, _httpClientFactory, _memoryCache, _parametersManager);

        SetValue(recordForUpdate,
            await gitIgnorePathsCruder.GetNameWithPossibleNewName(FieldName, currentGitIgnorePathName, null, false,
                cancellationToken));
    }

    public override string GetValueStatus(object? record)
    {
        string? val = GetValue(record);

        if (val == null)
        {
            return string.Empty;
        }

        var gitIgnorePathsCruder =
            new GitIgnoreFileTypesStsCruder(_logger, _httpClientFactory, _memoryCache, _parametersManager);

        string? status = gitIgnorePathsCruder.GetStatusFor(val);
        return $"{val} {(string.IsNullOrWhiteSpace(status) ? string.Empty : $"({status})")}";
    }
}

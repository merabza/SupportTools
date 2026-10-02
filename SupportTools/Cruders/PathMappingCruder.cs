using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.CliParameters.FieldEditors;
using LibSupportToolsServerWork.Registry.Paths;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Cruders;

//გზების გარდაქმნის წესები (PathMappings). ჩანაწერის გასაღები მისი კანონიკური prefix-ია, ამიტომ ცალკე სახელი არ აქვს.
//ჩანაწერები ადგილზე იცვლება და ყოველი ცვლილება parametersManager-ის ძირეული ობიექტის შენახვით სრულდება
public sealed class PathMappingCruder : Cruder
{
    private const string EmptyCanonicalPrefixKey = "(empty)";

    private readonly IParametersManager _parametersManager;
    private readonly List<PathMappingModel> _pathMappings;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PathMappingCruder(IParametersManager parametersManager, List<PathMappingModel> pathMappings) : base(
        "Path Mapping", "Path Mappings", true)
    {
        _parametersManager = parametersManager;
        _pathMappings = pathMappings;
        FieldEditors.Add(new TextFieldEditor(nameof(PathMappingModel.CanonicalPrefix)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(PathMappingModel.LocalPrefix), null, true));
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return GetKeyedPathMappings().ToDictionary(k => k.Key, ItemData (v) => v.Value);
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        return GetKeyedPathMappings().ContainsKey(recordKey);
    }

    protected override ValueTask RemoveRecordWithKey(string recordKey, CancellationToken cancellationToken = default)
    {
        if (GetKeyedPathMappings().TryGetValue(recordKey, out PathMappingModel? pathMapping))
        {
            _pathMappings.Remove(pathMapping);
        }

        return ValueTask.CompletedTask;
    }

    protected override ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        if (newRecord is PathMappingModel pathMapping)
        {
            _pathMappings.Add(pathMapping);
        }

        return ValueTask.CompletedTask;
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new PathMappingModel();
    }

    public override string? GetStatusFor(string name)
    {
        return GetKeyedPathMappings().GetValueOrDefault(name)?.LocalPrefix;
    }

    public override bool CheckValidation(ItemData item)
    {
        if (item is not PathMappingModel pathMapping)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(pathMapping.CanonicalPrefix) ||
            string.IsNullOrWhiteSpace(pathMapping.LocalPrefix))
        {
            StShared.WriteErrorLine("Canonical prefix and local prefix must be specified", true, null, false);
            return false;
        }

        //კანონიკური ფორმა Windows-ის აბსოლუტური გზაა, როგორც მთავარ კომპიუტერზე
        if (!PathMapper.IsWindowsRooted(pathMapping.CanonicalPrefix))
        {
            StShared.WriteErrorLine($"Canonical prefix {pathMapping.CanonicalPrefix} is not a Windows absolute path",
                true, null, false);
            return false;
        }

        //ერთი და იგივე კანონიკური prefix-ის ორი წესიდან რომელიმე შემთხვევით გაიმარჯვებდა
        if (_pathMappings.Any(x =>
                !ReferenceEquals(x, pathMapping) &&
                PathMapper.IsSamePrefix(x.CanonicalPrefix, pathMapping.CanonicalPrefix)))
        {
            StShared.WriteErrorLine($"Path mapping for {pathMapping.CanonicalPrefix} already exists", true, null,
                false);
            return false;
        }

        return true;
    }

    public override ValueTask<bool> Save(string message, CancellationToken cancellationToken = default)
    {
        return _parametersManager.Save(_parametersManager.Parameters, message, null, cancellationToken);
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        cruderSubMenuSet.AddMenuItem(new SuggestPathMappingsCliMenuCommand(_parametersManager));
    }

    //სიაში გასაღებად კანონიკური prefix ჩანს. დუბლიკატი ან ცარიელი prefix-ი მენიუს არ უნდა ამტვრევდეს,
    //ამიტომ განმეორებულ გასაღებს რიგითი ნომერი ემატება
    private Dictionary<string, PathMappingModel> GetKeyedPathMappings()
    {
        Dictionary<string, PathMappingModel> keyedPathMappings = [];
        foreach (PathMappingModel pathMapping in _pathMappings)
        {
            string key = string.IsNullOrWhiteSpace(pathMapping.CanonicalPrefix)
                ? EmptyCanonicalPrefixKey
                : pathMapping.CanonicalPrefix;
            string candidate = key;
            var counter = 2;
            while (keyedPathMappings.ContainsKey(candidate))
            {
                candidate = $"{key} ({counter})";
                counter++;
            }

            keyedPathMappings.Add(candidate, pathMapping);
        }

        return keyedPathMappings;
    }
}

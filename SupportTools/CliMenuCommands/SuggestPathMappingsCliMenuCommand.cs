using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using LibSupportToolsServerWork.Registry.Paths;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.CliMenuCommands;

//პარამეტრების გზებიდან კანონიკურ ფესვებს კრებს (CanonicalPathRoots), თითოზე ლოკალურ prefix-ს ეკითხება და შედეგს
//PathMappings-ში ინახავს. ნაგულისხმევი პასუხი ის გზაა, რასაც ახლანდელი წესები იძლევა, ამიტომ Enter არაფერს ცვლის.
//თვითონ ფესვის ან ცარიელი ტექსტის შეყვანა ამ ფესვის წესს შლის, რადგან იგივე განლაგებისას mapping საჭირო არ არის
public sealed class SuggestPathMappingsCliMenuCommand : CliMenuCommand
{
    private readonly Func<string, string?, string?> _inputText;
    private readonly IParametersManager _parametersManager;

    public SuggestPathMappingsCliMenuCommand(IParametersManager parametersManager) : this(parametersManager,
        (fieldName, defaultValue) => Inputer.InputText(fieldName, defaultValue))
    {
    }

    //კონსოლიდან შეყვანა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხები თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal SuggestPathMappingsCliMenuCommand(IParametersManager parametersManager,
        Func<string, string?, string?> inputText) : base("Suggest Path Mappings...", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
        _inputText = inputText;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        List<string> roots = CanonicalPathRoots.Collect(parameters);
        if (roots.Count == 0)
        {
            StShared.WriteErrorLine("No canonical paths found in parameters", true);
            return false;
        }

        Console.WriteLine("Enter keeps the current local prefix. The root itself or empty text means no mapping.");

        //ნაგულისხმევი პასუხები ბრძანების დაწყებამდე არსებული წესებით ითვლება. ცვლილებები ყველა პასუხის შემდეგ
        //სრულდება, რომ Escape-ის დროს PathMappings უცვლელი დარჩეს. null ნიშნავს, რომ ფესვს წესი არ სჭირდება
        var currentPathMapper = new PathMapper(parameters.PathMappings);
        List<(string Root, string? LocalPrefix)> changes = [];
        foreach (string root in roots)
        {
            string? currentLocalPrefix = currentPathMapper.ToLocal(root);
            string? localPrefix = _inputText($"Local prefix for {root}", currentLocalPrefix)?.Trim();
            if (PathMapper.IsSamePrefix(localPrefix, currentLocalPrefix))
            {
                continue;
            }

            changes.Add((root,
                string.IsNullOrEmpty(localPrefix) || PathMapper.IsSamePrefix(localPrefix, root) ? null : localPrefix));
        }

        var changedRootsCount = 0;
        foreach ((string root, string? localPrefix) in changes)
        {
            int removedCount = parameters.PathMappings.RemoveAll(x => PathMapper.IsSamePrefix(x.CanonicalPrefix, root));
            if (localPrefix is not null)
            {
                parameters.PathMappings.Add(new PathMappingModel { CanonicalPrefix = root, LocalPrefix = localPrefix });
            }

            if (localPrefix is not null || removedCount > 0)
            {
                changedRootsCount++;
            }
        }

        if (changedRootsCount == 0)
        {
            return true;
        }

        return await _parametersManager.Save(parameters, $"Path mappings changed: {changedRootsCount}", null,
            cancellationToken);
    }
}

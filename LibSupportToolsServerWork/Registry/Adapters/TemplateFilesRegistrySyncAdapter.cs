using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//.gitignore და .editorconfig შაბლონები: ლოკალური ჩანაწერი სახელების სიის (GitIgnorePatterns, EditorConfigPatterns)
//ელემენტია და მისი ფაილის შიგთავსი შაბლონების ფოლდერში (ფოლდერი ამ კომპიუტერისაა). სერვერზე შიგთავსი ინახება.
//- თუ სიაში არსებული სახელის ფაილი არ არსებობს, ეს ლოკალური ჩანაწერის შეცდომაა: იწერება გაფრთხილება და ჩანაწერი
//  ორივე მხარეს გამოირიცხება, რომ სერვერის ჩანაწერი ლოკალურად წაშლილად არ ჩაითვალოს და სერვერიდან არ წაიშალოს.
//  შაბლონების ფოლდერის გარეშე არცერთი შაბლონი არ სინქრონიზდება.
//- ApplyLocal ფაილს წერს (საჭიროებისას ფოლდერსაც ქმნის) და სახელს სიაში ამატებს. RemoveLocal სახელს მხოლოდ სიიდან
//  შლის, ფაილი დისკზე რჩება (მომხმარებლის გადაწყვეტილება, C3): სიაში არარსებულ ფაილს ადაპტერი აღარ კითხულობს.
//- სერვერის endpoint-ები ძველია და ვერსიას არ ამოწმებს: იხ. UpsertCheckingVersionOnClient
public abstract class TemplateFilesRegistrySyncAdapter<TContract> : RegistrySyncAdapter<TContract>
    where TContract : class
{
    // ReSharper disable once ConvertToPrimaryConstructor
    protected TemplateFilesRegistrySyncAdapter(RegistrySyncWarnings warnings) : base(warnings)
    {
    }

    //შაბლონების ფოლდერის ველის სახელი გაფრთხილებისთვის (მაგ. FolderForGitignoreFiles)
    protected abstract string FolderFieldName { get; }

    //ძველი endpoint-ის „ჩანაწერი არ არის“ შეცდომის კოდი
    protected abstract string ServerNotFoundErrorCode { get; }

    public override Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return DeleteCheckingVersionOnClient(key, expectedVersion, () => DeleteServerRecord(key, cancellationToken),
            ServerNotFoundErrorCode, cancellationToken);
    }

    public override void RemoveLocal(string key)
    {
        GetNames().RemoveAll(x => string.Equals(x, key, StringComparison.Ordinal));
    }

    protected override IEnumerable<(string Key, TContract Contract)> GetLocalContracts()
    {
        string? folder = GetFolder();
        if (string.IsNullOrWhiteSpace(folder))
        {
            Warnings.Add(CollectionName, null, $"{FolderFieldName} is not set, the templates are not synced");
            return [];
        }

        List<(string Key, TContract Contract)> contracts = [];
        foreach (string name in GetNames())
        {
            string fileName = GetFilePath(folder, name);
            if (File.Exists(fileName))
            {
                contracts.Add((name, CreateContract(name, File.ReadAllText(fileName))));
            }
            else
            {
                Warnings.Add(CollectionName, name, $"file {fileName} does not exist, the template is not synced");
            }
        }

        return contracts;
    }

    protected override Task<Result<int>> UpsertContract(string key, TContract contract, int expectedVersion,
        CancellationToken cancellationToken)
    {
        return UpsertCheckingVersionOnClient(key, expectedVersion,
            () => WriteServerContract(CreateContract(key, GetContent(contract)), cancellationToken), cancellationToken);
    }

    //key ლოკალური წერილობაა, თუ სახელი სიაში უკვე არის
    protected override void ApplyContract(string key, TContract contract)
    {
        string? folder = GetFolder();
        //ფოლდერის გარეშე ჩანაწერი არ სინქრონიზდება (IsSynced), ამიტომ აქ არ მოდის
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        Directory.CreateDirectory(folder);
        File.WriteAllText(GetFilePath(folder, key), GetContent(contract));
        List<string> names = GetNames();
        if (!names.Contains(key))
        {
            names.Add(key);
        }
    }

    protected override bool IsSynced(string key)
    {
        string? folder = GetFolder();
        if (string.IsNullOrWhiteSpace(folder))
        {
            return false;
        }

        string? name = GetNames().Find(x => string.Equals(x, key, StringComparison.OrdinalIgnoreCase));
        return name is null || File.Exists(GetFilePath(folder, name));
    }

    protected abstract string? GetFolder();

    protected abstract List<string> GetNames();

    protected abstract string GetFilePath(string folder, string name);

    protected abstract TContract CreateContract(string name, string content);

    protected abstract string GetContent(TContract contract);

    //ერთი შაბლონის დამატება ან ჩანაცვლება სერვერზე (SyncUp, merge=true)
    protected abstract Task<Result> WriteServerContract(TContract contract, CancellationToken cancellationToken);

    protected abstract Task<Result> DeleteServerRecord(string key, CancellationToken cancellationToken);
}

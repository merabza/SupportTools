using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Mappers;
using LibSupportToolsServerWork.Registry.Paths;
using LibSupportToolsServerWork.Registry.Sync;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//საიდუმლო ფაილები (C6, B8, README G2): ფაილები, რომლებზეც რეესტრი მიუთითებს, ყველა კომპიუტერზე უნდა გაჩნდეს. სერვერზე
//ისინი ღიად ინახება. ჩანაწერის გასაღები კანონიკური გზაა (ToCanonical, დისკის დიდი ასოთი), ფაილი კი ამ კომპიუტერზე
//ToLocal(გასაღები)-ზეა; ჩამოტანა საჭირო ფოლდერებს ქმნის.
//- რეესტრის ფაილის ველები (მომხმარებლის გადაწყვეტილება): ServerInfo-ების AppSettingsJsonSourceFileName და პროექტის
//  SeedProjectParametersFilePath, PrepareProdCopyDatabaseProjectParametersFilePath, PairedDbObjectsResultFileName.
//  AppSettingsEncoder-ის გენერირებული AppSettingsEncodedJsonFileName არ შედის. რეესტრის მითითებული, მაგრამ ლოკალურად
//  დაკარგული ფაილი სერვერიდან ჩამოდის (GetMissingLocalKeys) და სერვერიდან არ იშლება.
//- ლოკალური ჩანაწერია არსებული ფაილი, რომელზეც რეესტრი მიუთითებს, რომელიც სერვერზეა ან რომელიც უკვე სინქრონიზდა
//  (მდგომარეობაშია). ამიტომ ფაილი სინქრონიზაციაში რჩება, სანამ არსებობს, მაშინაც, როცა რეესტრი მასზე აღარ მიუთითებს
//  (ფაილზე დაფუძნებული წაშლა, მომხმარებლის გადაწყვეტილება): სერვერიდან წაშლა მხოლოდ მაშინ შემოთავაზდება, როცა ლოკალური
//  ფაილი წაშლილია და რეესტრი მასზე არ მიუთითებს. წაშლას ორივე მიმართულებით მომხმარებელი ადასტურებს
//  (Conflict(DeleteNeedsConfirmation), IRegistryFileSyncAdapter): ფაილი ავტომატურად არასოდეს იშლება.
//- ორივე მხარეს, ჩუმად, გამოირიცხება ფაილი git-ის სამუშაო ხეში (რომელიმე ზედა ფოლდერში .git): მას git ატარებს
//  (მომხმარებლის გადაწყვეტილება). გაფრთხილებით გამოირიცხება გზა, რომელსაც სერვერი არ მიიღებს (StoredFilePathRules) ან
//  რომელსაც ამ კომპიუტერზე ფორმა არ აქვს (mapping-ის გარეშე), და ბინარული, ზღვარზე დიდი ან წაუკითხავი ფაილი
//  (LocalStoredFile). ასეთი ფაილი არც იგზავნება, არც ჩამოდის და არც იშლება.
//- სერვერის სია (GET files) შიგთავსს არ შეიცავს. შიგთავსი მხოლოდ ჩამოსატან ჩანაწერზე მოდის (PrepareApplyLocal,
//  GET files/content) და მხოლოდ გასაგზავნ ჩანაწერზე იკითხება (Upsert). ის არსად იბეჭდება
public sealed class StoredFilesRegistrySyncAdapter : RegistrySyncAdapter<StoredFileContract>, IRegistryFileSyncAdapter
{
    private readonly SupportToolsServerApiClient _apiClient;

    //PrepareApplyLocal-ით ჩამოტანილი შიგთავსი ApplyLocal-მდე, გასაღებით
    private readonly Dictionary<string, string> _downloadedContents = new(StringComparer.OrdinalIgnoreCase);

    private readonly SupportToolsParameters _parameters;
    private readonly PathMapper _pathMapper;

    // ReSharper disable once ConvertToPrimaryConstructor
    public StoredFilesRegistrySyncAdapter(SupportToolsServerApiClient apiClient, SupportToolsParameters parameters,
        PathMapper pathMapper, RegistrySyncWarnings warnings) : base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
        _pathMapper = pathMapper;
    }

    public override string CollectionName => RegistryCollections.StoredFiles;
    public override int Order => RegistryCollections.StoredFilesOrder;

    public IReadOnlyCollection<string> GetMissingLocalKeys()
    {
        return [.. GetReferencedKeys().Where(x => GetLocalFile(x).State == ELocalFileState.Missing)];
    }

    public async Task<Result> PrepareApplyLocal(string key, object contract, CancellationToken cancellationToken)
    {
        Result<StsStoredFileDataModel> storedFile =
            await _apiClient.GetStoredFile(((StoredFileContract)contract).Path, cancellationToken);
        if (storedFile.IsFailure)
        {
            return Result.Failure(storedFile.Error);
        }

        _downloadedContents[key] = storedFile.Value.Content;
        return Result.Success();
    }

    public override async Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return await _apiClient.DeleteStoredFile(key, expectedVersion, cancellationToken);
    }

    //მხოლოდ მომხმარებლის გადაწყვეტილი კონფლიქტიდან მოდის (DeleteNeedsConfirmation). თუ ფაილი ვერ წაიშალა, ძრავა
    //ჩანაწერის ხელახალი წაკითხვისას მას Failed-ად აჩვენებს, მიზეზი კი გაფრთხილებაშია
    public override void RemoveLocal(string key)
    {
        string? localPath = ToLocalPath(key);
        if (localPath is null || !File.Exists(localPath))
        {
            return;
        }

        try
        {
            File.Delete(localPath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Warnings.Add(CollectionName, key, $"{localPath} cannot be deleted ({e.Message})");
        }
    }

    //ორივე მხარე Sha256-ს დიდი ასოებით იძლევა (B8), Path კი ჰეშში არ შედის
    protected override StoredFileContract NormalizeContract(StoredFileContract contract)
    {
        return contract;
    }

    protected override IEnumerable<(string Key, StoredFileContract Contract)> GetLocalContracts()
    {
        List<(string Key, StoredFileContract Contract)> contracts = [];
        foreach (string key in GetCandidateKeys())
        {
            LocalStoredFileContent? content = GetLocalFile(key).Content;
            if (content is not null)
            {
                contracts.Add((key,
                    new StoredFileContract { Path = key, Sha256 = content.Sha256, Length = content.Length }));
            }
        }

        return contracts;
    }

    protected override async Task<Result<List<StoredFileContract>>> GetServerContracts(
        CancellationToken cancellationToken)
    {
        Result<List<StsStoredFileInfoDataModel>> storedFiles = await _apiClient.GetStoredFiles(cancellationToken);
        if (storedFiles.IsFailure)
        {
            return Result.Failure<List<StoredFileContract>>(storedFiles.Error);
        }

        List<StoredFileContract> contracts =
        [
            .. storedFiles.Value.Select(x =>
                new StoredFileContract { Path = x.Path, Sha256 = x.Sha256, Length = x.Length, Version = x.Version })
        ];
        return contracts;
    }

    protected override string GetKey(StoredFileContract contract)
    {
        return contract.Path;
    }

    protected override int GetVersion(StoredFileContract contract)
    {
        return contract.Version;
    }

    //ფაილი ახლა იკითხება: თუ გეგმის შემდეგ შეიცვალა, სერვერზე ახალი შიგთავსი მიდის და შემდეგი სინქრონიზაცია ორივე მხარეს
    //ერთნაირს ნახავს. ფაილი, რომელიც აღარ იკითხება, სერვერზე არ მიდის
    protected override async Task<Result<int>> UpsertContract(string key, StoredFileContract contract,
        int expectedVersion, CancellationToken cancellationToken)
    {
        //შიგთავსის გარეშე GetLocalFile ყოველთვის პრობლემას აბრუნებს
        (_, LocalStoredFileContent? content, string? problem) = GetLocalFile(key);
        if (content is null)
        {
            return RegistrySyncErrors.LocalRecordIsInvalid(CollectionName, key, problem!);
        }

        return await _apiClient.UpdateStoredFile(
            new StsStoredFileDataModel { Path = key, Content = content.Content, Version = expectedVersion },
            cancellationToken);
    }

    //key ლოკალური წერილობაა, თუ ფაილი ლოკალურად არსებობს. შიგთავსი PrepareApplyLocal-მა ჩამოიტანა. ჩაწერის შეცდომა
    //გადის: ძველი ფაილი ხელახალ წაკითხვაზე ჩამოტანილად გამოჩნდებოდა და ჩანაწერი აღარასოდეს განახლდებოდა
    protected override void ApplyContract(string key, StoredFileContract contract)
    {
        if (!_downloadedContents.Remove(key, out string? content))
        {
            throw new InvalidOperationException(
                $"{CollectionName}/{key}: the content was not downloaded, PrepareApplyLocal must run first");
        }

        //ჩამოსატანი ჩანაწერი სინქრონიზაციაშია (IsSynced), ანუ ლოკალური გზა აქვს
        string localPath = ToLocalPath(key) ??
                           throw new InvalidOperationException($"{CollectionName}/{key}: no local path");
        LocalStoredFile.Write(localPath, content);
    }

    protected override bool IsSynced(string key)
    {
        return GetLocalFile(key).State != ELocalFileState.Excluded;
    }

    //ფაილი ამ კომპიუტერზე: მდგომარეობა, არსებული და წაკითხვადი ფაილის შიგთავსი და პრობლემა, რის გამოც ფაილი არ
    //სინქრონიზდება ან აღარ იკითხება. გამორიცხვის მიზეზი, git-ის გარდა, გაფრთხილებად იწერება
    private (ELocalFileState State, LocalStoredFileContent? Content, string? Problem) GetLocalFile(string key)
    {
        if (!StoredFilePathRules.IsValidCanonicalPath(key))
        {
            return Exclude(key, @"The path is not a canonical file path (X:\...) that the server accepts");
        }

        string? localPath = ToLocalPath(key);
        if (localPath is null)
        {
            return Exclude(key, "The path has no form on this computer (add a path mapping)");
        }

        if (IsInGitWorkingTree(localPath))
        {
            return (ELocalFileState.Excluded, null, $"{localPath} is in a git working tree");
        }

        if (!File.Exists(localPath))
        {
            return (ELocalFileState.Missing, null, $"{localPath} does not exist");
        }

        (LocalStoredFileContent? content, string? problem) = LocalStoredFile.Read(localPath);
        return content is null ? Exclude(key, $"{localPath} {problem}") : (ELocalFileState.Present, content, null);
    }

    private (ELocalFileState State, LocalStoredFileContent? Content, string? Problem) Exclude(string key,
        string problem)
    {
        Warnings.Add(CollectionName, key, $"{problem}, the file is not synced");
        return (ELocalFileState.Excluded, null, problem);
    }

    //ლოკალური ჩანაწერების კანდიდატები, რეგისტრის გარეშე უნიკალური: ჯერ რეესტრის წერილობა, მერე სერვერის, მერე
    //მდგომარეობის. მხოლოდ რეგისტრით განსხვავებული გზები Windows-ზე ერთი ფაილია
    private List<string> GetCandidateKeys()
    {
        HashSet<string> seenKeys = new(StringComparer.OrdinalIgnoreCase);
        return [.. GetReferencedKeys().Concat(ServerKeys).Concat(GetStateKeys()).Where(seenKeys.Add)];
    }

    //ფაილები, რომლებზეც რეესტრი მიუთითებს, კანონიკური გზით, რეგისტრის გარეშე უნიკალური
    private List<string> GetReferencedKeys()
    {
        HashSet<string> seenKeys = new(StringComparer.OrdinalIgnoreCase);
        return
        [
            .. GetReferencedLocalPaths().OfType<string>().Where(x => !string.IsNullOrWhiteSpace(x)).Select(ToKey)
                .Where(seenKeys.Add)
        ];
    }

    //რეესტრის ფაილის ველები, ამ კომპიუტერის გზებით
    private IEnumerable<string?> GetReferencedLocalPaths()
    {
        foreach (ProjectModel project in _parameters.Projects.Values)
        {
            yield return project.SeedProjectParametersFilePath;
            yield return project.PrepareProdCopyDatabaseProjectParametersFilePath;
            yield return project.PairedDbObjectsResultFileName;
            foreach (ServerInfoModel serverInfo in project.ServerInfos.Values)
            {
                yield return serverInfo.AppSettingsJsonSourceFileName;
            }
        }
    }

    //ფაილები, რომლებიც ამ კომპიუტერზე უკვე სინქრონიზდა
    private List<string> GetStateKeys()
    {
        RegistryCollectionSyncStateModel? collectionState =
            _parameters.RegistrySyncState.Collections.GetValueOrDefault(CollectionName);
        return collectionState is null ? [] : [.. collectionState.Records.Keys];
    }

    //ლოკალური გზის კანონიკური ფორმა, ანუ ჩანაწერის გასაღები
    private string ToKey(string localPath)
    {
        return ContractNormalization.UpperDriveLetter(_pathMapper.ToCanonical(localPath)) ?? localPath;
    }

    //კანონიკური გზის ფორმა ამ კომპიუტერზე; null — ფორმა არ აქვს: Linux-ზე Windows-ის გზა mapping-ის გარეშე (PathMapper-ის
    //issue) ან სხვა გზა, რომელიც ამ OS-ზე სრული არ არის
    private string? ToLocalPath(string key)
    {
        string? localPath = _pathMapper.ToLocal(key);
        return localPath is null ||
               _pathMapper.Issues.Contains(new PathMappingIssue(EPathMappingDirection.ToLocal, key)) ||
               !Path.IsPathFullyQualified(localPath)
            ? null
            : localPath;
    }

    //ფაილი git-ის სამუშაო ხეშია: რომელიმე ზედა ფოლდერში .git ფოლდერი (ან worktree-ის .git ფაილი) არის
    private static bool IsInGitWorkingTree(string localPath)
    {
        for (string? folder = Path.GetDirectoryName(localPath);
             !string.IsNullOrEmpty(folder);
             folder = Path.GetDirectoryName(folder))
        {
            string gitPath = Path.Combine(folder, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return true;
            }
        }

        return false;
    }

    private enum ELocalFileState
    {
        Excluded, //სინქრონიზაციაში არ მონაწილეობს, ორივე მხარეს
        Missing, //ფაილი ამ კომპიუტერზე არ არის
        Present //ფაილი არის და წაიკითხება
    }
}

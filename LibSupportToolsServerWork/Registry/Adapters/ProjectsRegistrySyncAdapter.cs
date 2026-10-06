using System;
using System.Collections.Generic;
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

//პროექტები: SupportToolsParameters.Projects ↔ StsProjectDataModel (B6), ServerInfo-ებით (B7), ერთ აგრეგატად (G7).
//კოლექციებიდან ბოლოა. KeyGuidPart საიდუმლოა და არსად იბეჭდება. ლოკალური პროექტის კონტრაქტის აგებისას იწერება
//გაფრთხილებები (C5 მათ აჩვენებს):
//- პროექტი მიმართავს git-ს, npm პაკეტს, .editorconfig შაბლონს ან DB კავშირს, რომელიც არც ლოკალურად არის და არც
//  სერვერზე (სერვერი ასეთ Push-ს 404 ReferencedRecordsNotFound-ით უარყოფს);
//- გზა ვერ გარდაიქმნა კანონიკურად (PathMapper-ის issue);
//- სიმრავლეში ან ServerInfo-ების ნატურალურ გასაღებებში მნიშვნელობა მეორდება (მათ შორის მხოლოდ რეგისტრით): ასეთ
//  პროექტს Upsert სერვერზე არ აგზავნის და LocalRecordIsInvalid-ს აბრუნებს, Pull კი მუშაობს.
//პროექტები, რომელთა გასაღებებიც ლოკალურად მხოლოდ რეგისტრით განსხვავდება, გაფრთხილებით ორივე მხარეს გამოირიცხება.
//სერვერის პროექტი, რომელშიც ამ კლიენტისთვის უცნობი enum-ის სახელია (მაგ. ახალი ხელსაწყო), ასევე ორივე მხარეს
//გამოირიცხება (RegistrySyncAdapter, FindUnsupportedField): Pull-ით უცნობი სახელი დაიკარგებოდა, Push-ით კი სერვერზე
//წაიშლებოდა, ამიტომ პროექტი SupportTools-ის განახლებამდე არ სინქრონიზდება
public sealed class ProjectsRegistrySyncAdapter : DictionaryRegistrySyncAdapter<ProjectModel, StsProjectDataModel>
{
    private readonly SupportToolsServerApiClient _apiClient;
    private readonly SupportToolsParameters _parameters;
    private readonly PathMapper _pathMapper;
    private readonly ProjectReferenceServerKeys _referenceServerKeys;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectsRegistrySyncAdapter(SupportToolsServerApiClient apiClient, SupportToolsParameters parameters,
        PathMapper pathMapper, ProjectReferenceServerKeys referenceServerKeys, RegistrySyncWarnings warnings) :
        base(warnings)
    {
        _apiClient = apiClient;
        _parameters = parameters;
        _pathMapper = pathMapper;
        _referenceServerKeys = referenceServerKeys;
    }

    public override string CollectionName => RegistryCollections.Projects;
    public override int Order => RegistryCollections.ProjectsOrder;

    public override async Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return await _apiClient.DeleteProject(key, expectedVersion, cancellationToken);
    }

    protected override Dictionary<string, ProjectModel> GetLocalDictionary()
    {
        return _parameters.Projects;
    }

    protected override StsProjectDataModel ToContract(string key, ProjectModel local)
    {
        //ToContract მხოლოდ ToCanonical-ს იძახებს, ამიტომ ყველა ახალი issue ამ პროექტის გზას ეკუთვნის
        int pathIssuesCount = _pathMapper.Issues.Count;
        StsProjectDataModel contract = ProjectMapper.ToContract(key, local, _pathMapper);
        foreach (PathMappingIssue issue in _pathMapper.Issues.Skip(pathIssuesCount))
        {
            Warnings.Add(CollectionName, key, $"The path {issue.Path} has no canonical form (no path mapping)");
        }

        foreach (string missingReference in FindMissingReferences(contract))
        {
            Warnings.Add(CollectionName, key,
                $"{missingReference} is neither in the local parameters nor on the server");
        }

        List<string> repeatedValues = ProjectMapper.FindRepeatedValues(contract);
        if (repeatedValues.Count > 0)
        {
            Warnings.Add(CollectionName, key,
                $"Repeated values ({string.Join("; ", repeatedValues)}), the project is not pushed until they are " +
                "fixed");
        }

        return contract;
    }

    protected override ProjectModel ToLocal(StsProjectDataModel contract, ProjectModel? existing)
    {
        return ProjectMapper.ToLocal(contract, existing, _pathMapper);
    }

    protected override StsProjectDataModel NormalizeContract(StsProjectDataModel contract)
    {
        return ProjectMapper.Normalize(contract);
    }

    protected override Task<Result<List<StsProjectDataModel>>> GetServerContracts(CancellationToken cancellationToken)
    {
        return _apiClient.GetProjects(cancellationToken);
    }

    protected override string GetKey(StsProjectDataModel contract)
    {
        return contract.Name;
    }

    protected override int GetVersion(StsProjectDataModel contract)
    {
        return contract.Version;
    }

    //კონტრაქტის Version upsert-ის მოსალოდნელი ვერსიაა (B1). განმეორებული მნიშვნელობების მქონე პროექტი სერვერზე არ მიდის
    protected override Task<Result<int>> UpsertContract(string key, StsProjectDataModel contract, int expectedVersion,
        CancellationToken cancellationToken)
    {
        List<string> repeatedValues = ProjectMapper.FindRepeatedValues(contract);
        if (repeatedValues.Count > 0)
        {
            return Task.FromResult(Result.Failure<int>(
                RegistrySyncErrors.LocalRecordIsInvalid(CollectionName, key, string.Join("; ", repeatedValues))));
        }

        contract.Version = expectedVersion;
        return _apiClient.UpdateProject(key, contract, cancellationToken);
    }

    protected override string? FindUnsupportedField(StsProjectDataModel contract)
    {
        return ProjectMapper.FindUnknownEnumField(contract);
    }

    //ლოკალური გასაღებები, რომლებიც მხოლოდ რეგისტრით განსხვავდება, ვერ დგინდება, რომელს შეესაბამება სერვერის ჩანაწერი.
    //ძრავა ასეთ გასაღებებზე მთელ გეგმას შეცდომით აჩერებს (DuplicateKeys), ამიტომ ადაპტერი მათ ორივე მხარეს გამორიცხავს
    protected override bool IsSynced(string key)
    {
        List<string> sameKeys =
        [
            .. _parameters.Projects.Keys.Where(x => string.Equals(x, key, StringComparison.OrdinalIgnoreCase))
        ];
        if (sameKeys.Count < 2)
        {
            return true;
        }

        Warnings.Add(CollectionName, key,
            $"The local project keys {string.Join('/', sameKeys)} differ only by case, the project is not synced");
        return false;
    }

    //მითითებები, რომლებიც არც ლოკალურად არის და არც სერვერზე: "<ველი> <სახელი>"
    private IEnumerable<string> FindMissingReferences(StsProjectDataModel contract)
    {
        HashSet<string> gits = KnownNames(_parameters.Gits.Keys, _referenceServerKeys.Gits);
        HashSet<string> npmPackages = KnownNames(_parameters.NpmPackages.Keys, _referenceServerKeys.NpmPackages);
        HashSet<string> editorConfigPatterns =
            KnownNames(_parameters.EditorConfigPatterns, _referenceServerKeys.EditorConfigPatterns);
        HashSet<string> databaseConnections = KnownNames(_parameters.DatabaseServerConnections.Keys,
            _referenceServerKeys.DatabaseServerConnections);

        List<(string Field, string? Name, HashSet<string> KnownNames)> references =
        [
            .. contract.GitProjectNames.Select(x => (nameof(StsProjectDataModel.GitProjectNames), (string?)x, gits)),
            .. contract.ScaffoldSeederGitProjectNames.Select(x =>
                (nameof(StsProjectDataModel.ScaffoldSeederGitProjectNames), (string?)x, gits)),
            .. contract.FrontNpmPackageNames.Select(x =>
                (nameof(StsProjectDataModel.FrontNpmPackageNames), (string?)x, npmPackages)),
            (nameof(StsProjectDataModel.EditorConfigPatternName), contract.EditorConfigPatternName,
                editorConfigPatterns),
            .. GetDatabaseConnectionNames(contract).Select(x => (x.Field, x.Name, databaseConnections))
        ];

        return references.Where(x => !string.IsNullOrEmpty(x.Name) && !x.KnownNames.Contains(x.Name))
            .Select(x => $"{x.Field} {x.Name}");
    }

    private static HashSet<string> KnownNames(IEnumerable<string> localNames, IRegistryServerKeys serverKeys)
    {
        return new HashSet<string>(localNames.Concat(serverKeys.ServerKeys), StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<(string Field, string? Name)> GetDatabaseConnectionNames(StsProjectDataModel contract)
    {
        yield return ($"{nameof(StsProjectDataModel.DevDatabaseParameters)}.DbConnectionName",
            contract.DevDatabaseParameters?.DbConnectionName);
        yield return ($"{nameof(StsProjectDataModel.ProdCopyDatabaseParameters)}.DbConnectionName",
            contract.ProdCopyDatabaseParameters?.DbConnectionName);
        foreach (StsServerInfoDataModel serverInfo in contract.ServerInfos)
        {
            string naturalKey = ServerInfoMapper.NaturalKey(serverInfo.ServerName, serverInfo.EnvironmentName);
            string prefix = $"{nameof(StsProjectDataModel.ServerInfos)}.{naturalKey}";
            yield return ($"{prefix}.CurrentDatabaseParameters.DbConnectionName",
                serverInfo.CurrentDatabaseParameters?.DbConnectionName);
            yield return ($"{prefix}.NewDatabaseParameters.DbConnectionName",
                serverInfo.NewDatabaseParameters?.DbConnectionName);
        }
    }
}

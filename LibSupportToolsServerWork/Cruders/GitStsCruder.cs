using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.CliParameters.FieldEditors;
using LibGitData.Models;
using LibGitWork;
using LibSupportToolsServerWork.FieldEditors;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace LibSupportToolsServerWork.Cruders;

public sealed class GitStsCruder : Cruder
{
    private const string GitsList = nameof(GitsList);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IParametersManager _parametersManager;

    //სერვერიდან წაშლილი git-ის სახელი, რომლის ლოკალური ასლი ჯერ არ დამუშავებულა. Cruder-ში გადარქმევა წაშლა და
    //დამატებაა: თუ წაშლას დამატება მოსდევს, ლოკალური git ახალ სახელზე გადადის, თუ არა, ოპერაციის ბოლოს (Save) იშლება
    private string? _removedGitKey;

    private GitStsCruder(ILogger logger, IHttpClientFactory httpClientFactory, IMemoryCache memoryCache,
        IParametersManager parametersManager) : base("GitFromServer", "GitsFromServer")
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _parametersManager = parametersManager;
        FieldEditors.Add(new TextFieldEditor(nameof(GitDataModel.GitProjectAddress)));
        FieldEditors.Add(new TextFieldEditor(nameof(GitDataModel.GitProjectFolderName)));
        //სერვერი git-ს gitignore ტიპის გარეშე არ ინახავს და ტიპი სერვერზე უნდა არსებობდეს, ამიტომ ის სერვერის
        //ტიპებიდან აირჩევა და ახალი ჩანაწერის შექმნისასაც მოითხოვება
        FieldEditors.Add(new GitIgnorePathNameStsFieldEditor(logger, nameof(GitDataModel.GitIgnorePatternName),
            parametersManager, httpClientFactory, memoryCache, true));
    }

    public static GitStsCruder Create(ILogger logger, IHttpClientFactory httpClientFactory, IMemoryCache memoryCache,
        IParametersManager parametersManager)
    {
        return new GitStsCruder(logger, httpClientFactory, memoryCache, parametersManager);
    }

    //სერვერზე ჩანაწერები სხვა ბრძანებებითაც იცვლება, ამიტომ სია ყოველ აწყობაზე სერვერიდან თავიდან იტვირთება.
    //ქეში მხოლოდ აწყობილი სიის ჩანაწერებთან მუშაობისას გამოიყენება (მაგალითად, თითოეული ჩანაწერის სტატუსისთვის)
    protected override void BeforeGetListMenu()
    {
        _memoryCache.Remove(GitsList);
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return GetGitReposFromServer().ToDictionary(k => k.GitProjectName,
            ItemData (v) => new GitDataModel
            {
                GitProjectAddress = v.GitProjectAddress,
                GitIgnorePatternName = v.GitIgnorePatternName,
                GitProjectFolderName = v.GitProjectFolderName
            });
    }

    private SupportToolsServerApiClient? GetSupportToolsServerApiClient()
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;

        return supportToolsParameters.GetSupportToolsServerApiClient(_logger, _httpClientFactory);
    }

    private List<StsGitDataModel> GetGitReposFromServer()
    {
        return _memoryCache.GetOrCreate(GitsList, _ =>
        {
            //სია GetSubMenu-დან იკითხება, საიდანაც ამოვარდნილი გამონაკლისი მთელ პროგრამას დაასრულებდა. არასწორი
            //პარამეტრები (აპი კლიენტის სახელი, სერვერის მისამართი) გამონაკლისს იწვევს, ამიტომ კლიენტიც try-ში იქმნება.
            //მენიუ ეკრანს ასუფთავებს, ამიტომ შეცდომა პაუზით იბეჭდება
            try
            {
                SupportToolsServerApiClient? supportToolsServerApiClient = GetSupportToolsServerApiClient();

                if (supportToolsServerApiClient is null)
                {
                    return [];
                }

                Result<List<StsGitDataModel>> remoteGitReposResult = supportToolsServerApiClient.GetGitRepos().Result;
                if (remoteGitReposResult.IsSuccess)
                {
                    return remoteGitReposResult.Value;
                }

                StShared.WriteErrorLine("could not received remoteGits", true, _logger);
                remoteGitReposResult.Error.PrintErrorsOnConsole();
            }
            catch (Exception e)
            {
                StShared.WriteException(e, true, _logger);
                //throw;
            }

            return [];
        }) ?? [];
    }

    //სახელი უკვე ჩამოტვირთულ სიაში მოწმდება. გასაღებით მოთხოვნაზე არარსებული სახელისთვის სერვერი 404-ს აბრუნებს,
    //რომელსაც ApiClient შეცდომად ბეჭდავდა, თუმცა ახალი ჩანაწერისთვის სწორედ ეს არის მოსალოდნელი პასუხი.
    //სერვერი გასაღებებს რეგისტრის გარეშე ადარებს
    public override bool ContainsRecordWithKey(string recordKey)
    {
        return GetGitReposFromServer().Exists(x =>
            string.Equals(x.GitProjectName, recordKey, StringComparison.OrdinalIgnoreCase));
    }

    public override ValueTask UpdateRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        AddOrUpdateRecordWithKey(recordKey, newRecord);
        return ValueTask.CompletedTask;
    }

    //აბრუნებს, მიიღო თუ არა სერვერმა ჩანაწერი
    private bool AddOrUpdateRecordWithKey(string recordKey, ItemData newRecord)
    {
        SupportToolsServerApiClient? supportToolsServerApiClient = GetSupportToolsServerApiClient();

        if (supportToolsServerApiClient is null)
        {
            StShared.WriteErrorLine("supportToolsServerApiClient is null", true);
            return false;
        }

        if (newRecord is not GitDataModel model)
        {
            StShared.WriteErrorLine("newRecord is not GitDataModel", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(model.GitIgnorePatternName))
        {
            StShared.WriteErrorLine("GitIgnorePatternName is not entered", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(model.GitProjectAddress))
        {
            StShared.WriteErrorLine("GitProjectAddress is not entered", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(model.GitProjectFolderName))
        {
            StShared.WriteErrorLine("GitProjectFolderName is not entered", true);
            return false;
        }

        var gitDataDomain = new StsGitDataModel
        {
            GitIgnorePatternName = model.GitIgnorePatternName,
            GitProjectAddress = model.GitProjectAddress,
            GitProjectFolderName = model.GitProjectFolderName,
            GitProjectName = recordKey
        };

        bool updated = false;
        try
        {
            Result updateGitRepoByKeyResult = supportToolsServerApiClient
                .UpdateGitRepoByKey(recordKey, gitDataDomain).Result;
            updated = updateGitRepoByKeyResult.IsSuccess;
            if (updateGitRepoByKeyResult.IsFailure)
            {
                updateGitRepoByKeyResult.Error.PrintErrorsOnConsole();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        //სერვერზე ცვლილების შემდეგ ქეშში დარჩენილი სია აღარ გამოდგება
        _memoryCache.Remove(GitsList);
        return updated;
    }

    protected override async ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        //წაშლის შემდეგ დამატება გადარქმევაა
        string? renamedGitKey = _removedGitKey;
        _removedGitKey = null;

        if (AddOrUpdateRecordWithKey(recordKey, newRecord) && renamedGitKey is not null)
        {
            await RenameLocalGit(renamedGitKey, recordKey, cancellationToken);
        }
    }

    protected override async ValueTask RemoveRecordWithKey(string recordKey,
        CancellationToken cancellationToken = default)
    {
        SupportToolsServerApiClient? supportToolsServerApiClient = GetSupportToolsServerApiClient();

        if (supportToolsServerApiClient is null)
        {
            StShared.WriteErrorLine("supportToolsServerApiClient is null", true);
            return;
        }

        try
        {
            Result updateGitRepoByKeyResult =
                await supportToolsServerApiClient.RemoveGitRepoByKey(recordKey, cancellationToken);
            if (updateGitRepoByKeyResult.IsFailure)
            {
                updateGitRepoByKeyResult.Error.PrintErrorsOnConsole();
            }
            else
            {
                //ლოკალური git-ის ბედი მომდევნო ნაბიჯზეა დამოკიდებული: დამატება (გადარქმევა) თუ Save (წაშლა)
                _removedGitKey = recordKey;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        //სერვერზე ცვლილების შემდეგ ქეშში დარჩენილი სია აღარ გამოდგება
        _memoryCache.Remove(GitsList);
    }

    //Cruder-ის ყოველი ცვლილება Save-ით სრულდება. თუ სერვერიდან წაშლას დამატება არ მოჰყოლია, ეს წაშლა იყო
    public override async ValueTask<bool> Save(string message, CancellationToken cancellationToken = default)
    {
        if (_removedGitKey is null)
        {
            return true;
        }

        string removedGitKey = _removedGitKey;
        _removedGitKey = null;
        return await RemoveLocalGit(removedGitKey, cancellationToken);
    }

    //სერვერიდან წაშლილი git ლოკალურადაც იშლება, ოღონდ მხოლოდ მაშინ, თუ მას არცერთი პროექტი არ იყენებს
    private async ValueTask<bool> RemoveLocalGit(string gitKey, CancellationToken cancellationToken)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        string? localGitKey = FindLocalGitKey(parameters, gitKey);
        if (localGitKey is null)
        {
            return true;
        }

        List<string> projectNames = GetProjectNamesUsingGit(parameters, localGitKey);
        if (projectNames.Count > 0)
        {
            StShared.WriteWarningLine(
                $"Local git {localGitKey} is used by projects {string.Join(", ", projectNames)} and was not removed",
                true, _logger, true);
            return true;
        }

        parameters.Gits.Remove(localGitKey);
        return await _parametersManager.Save(parameters, $"Local git {localGitKey} removed", null,
            cancellationToken);
    }

    //სერვერზე გადარქმეული git ლოკალურადაც გადაერქმევა, მასზე მიმთითებელ პროექტებთან და GitProjects-თან ერთად
    private async ValueTask RenameLocalGit(string gitKey, string newGitKey, CancellationToken cancellationToken)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        string? localGitKey = FindLocalGitKey(parameters, gitKey);
        if (localGitKey is null)
        {
            return;
        }

        string? existingLocalGitKey = FindLocalGitKey(parameters, newGitKey);
        if (existingLocalGitKey is not null && existingLocalGitKey != localGitKey)
        {
            StShared.WriteWarningLine(
                $"Local git {existingLocalGitKey} already exists, local git {localGitKey} was not renamed", true,
                _logger, true);
            return;
        }

        GitDataModel localGit = parameters.Gits[localGitKey];
        parameters.Gits.Remove(localGitKey);
        parameters.Gits.Add(newGitKey, localGit);

        foreach (ProjectModel project in parameters.Projects.Values)
        {
            RenameGitName(project.GitProjectNames, localGitKey, newGitKey);
            RenameGitName(project.ScaffoldSeederGitProjectNames, localGitKey, newGitKey);
        }

        foreach (GitProjectDataModel gitProject in parameters.GitProjects.Values.Where(x =>
                     string.Equals(x.GitName, localGitKey, StringComparison.OrdinalIgnoreCase)))
        {
            gitProject.GitName = newGitKey;
        }

        await _parametersManager.Save(parameters, $"Local git {localGitKey} renamed to {newGitKey}", null,
            cancellationToken);
    }

    //ლოკალური git სერვერის სახელს რეგისტრის გაუთვალისწინებლად ემთხვევა, როგორც Gits-ის სინქრონიზაციისას
    private static string? FindLocalGitKey(SupportToolsParameters parameters, string gitKey)
    {
        return parameters.Gits.Keys.FirstOrDefault(x => string.Equals(x, gitKey, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> GetProjectNamesUsingGit(SupportToolsParameters parameters, string gitKey)
    {
        return
        [
            .. parameters.Projects
                .Where(p => p.Value.GitProjectNames.Concat(p.Value.ScaffoldSeederGitProjectNames)
                    .Contains(gitKey, StringComparer.OrdinalIgnoreCase)).Select(p => p.Key).Order()
        ];
    }

    private static void RenameGitName(List<string> gitNames, string gitName, string newGitName)
    {
        for (int i = 0; i < gitNames.Count; i++)
        {
            if (string.Equals(gitNames[i], gitName, StringComparison.OrdinalIgnoreCase))
            {
                gitNames[i] = newGitName;
            }
        }
    }

    public override bool CheckValidation(ItemData item)
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;
        var gitApi = new GitApi(true, _logger, supportToolsParameters.GitExecutablePath);
        try
        {
            if (item is not GitDataModel gitDataModel)
            {
                StShared.WriteErrorLine("item is not GitDataModel in GitCruder.CheckValidation", true, _logger);
                return false;
            }

            if (gitDataModel.GitProjectAddress is not null)
            {
                return gitApi.IsGitRemoteAddressValid(gitDataModel.GitProjectAddress);
            }

            StShared.WriteErrorLine("gitDataModel.GitProjectAddress is null in GitCruder.CheckValidation", true,
                _logger);
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while validating GitDataModel");
            return false;
        }
    }

    public override string GetStatusFor(string name)
    {
        var git = (GitDataModel?)GetItemByName(name);
        if (git is null)
        {
            return "ERROR: Git address Not found";
        }

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        Dictionary<string, ProjectModel> projects = parameters.Projects;

        int usageCount = projects.Values.Count(project => project.GitProjectNames.Contains(name)) +
                         projects.Values.Count(project => project.ScaffoldSeederGitProjectNames.Contains(name));

        return $"{git.GitProjectAddress} Usage count is: {usageCount}";
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new GitDataModel();
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string itemName)
    {
        //base.FillDetailsSubMenu(itemSubMenuSet, recordKey);

        //UpdateGitProjectCliMenuCommand updateGitProjectCommand = new(_logger, recordKey, _parametersManager);
        //itemSubMenuSet.AddMenuItem(updateGitProjectCommand);

        //var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        //var projects = parameters.Projects;

        ////Main Menu/
        //foreach (var itemSubMenuCommand in projects
        //             .Where(projectKvp => projectKvp.Value.GitProjectNames.Contains(recordKey)).Select(projectKvp =>
        //                 new InfoCliMenuCommand(projectKvp.Key,
        //                     $"{projectKvp.Value.ProjectGroupName}/{projectKvp.Key}")).OrderBy(x => x.Name))
        //    itemSubMenuSet.AddMenuItem(itemSubMenuCommand);
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        //var updateGitProjectsCommand = new UpdateGitProjectsCliMenuCommand(_logger, _parametersManager);
        //cruderSubMenuSet.AddMenuItem(updateGitProjectsCommand);

        //var uploadGitProjectsToSupportToolsServerCliMenuCommand =
        //    new UploadGitProjectsToSupportToolsServerCliMenuCommand(_logger, _httpClientFactory, _parametersManager);
        //cruderSubMenuSet.AddMenuItem(uploadGitProjectsToSupportToolsServerCliMenuCommand);
    }
}

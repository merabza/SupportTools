using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibMenuInput;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Paths;
using LibSupportToolsServerWork.Registry.Sync;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportTools.AutoSync;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.ApiContracts.Errors;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.SyncRegistry;

//რეესტრის ხელით სინქრონიზაცია SupportToolsServer-თან (C5, README §4.3) C2–C4-ის ძრავით, ფაბრიკის ყველა ადაპტერზე
//(RegistrySyncAdapterFactory). მთავარ კომპიუტერზე პირველი გამოყენება სერვერს ავსებს (seed). ნაბიჯები:
//1. კავშირი SupportToolsServerWebApiClientName-ის ApiClient-ით (bootstrap, G6). ვერ დაკავშირებისას ჩანს ApiClient-ის
//   სახელი, მისამართი და, 401-ისას, რჩევა ApiKey-ის შესამოწმებლად;
//2. წინასწარი შემოწმება: ლოკალური გასაღებები, რომლებიც მხოლოდ რეგისტრით განსხვავდება, სინქრონიზაციას აჩერებს
//   (RegistrySyncPreflight);
//3. გეგმა, არჩევანი და შესრულება: RegistrySyncSession. შესრულება მდგომარეობაში სერვერის მისამართს წერს, ბრძანების
//   შენახვები კი ავტომატურ Push-ს არ იწვევს (D2).
//შესრულების შემდეგ მენიუ თავიდან იგება (Reload), რომ ჩამოტანილი პროექტები და სხვა ჩანაწერები მენიუშიც გამოჩნდეს.
//ForCollection იგივე ბრძანებაა ერთ კოლექციაზე (D4): Gits-ის, .gitignore და .editorconfig შაბლონების სიების მალსახმობი,
//რომელმაც ძველი Merge/Sync Up/Down ბრძანებები შეცვალა, რომ სინქრონიზაციის სემანტიკა (ვერსიები, კონფლიქტები) ერთი იყოს
public sealed class SyncRegistryCliMenuCommand : CliMenuCommand
{
    public const string CommandName = "Sync Registry With SupportToolsServer...";

    //null — ყველა კოლექცია
    private readonly string? _collectionName;

    private readonly Func<SupportToolsServerApiClient, SupportToolsParameters, PathMapper, RegistrySyncWarnings,
        IEnumerable<IRegistrySyncAdapter>> _createAdapters;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Func<string, CliMenuSet, int> _inputIdFromMenuList;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    public SyncRegistryCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : this(logger, httpClientFactory, parametersManager,
        (fieldName, listSet) => MenuInputer.InputIdFromMenuList(fieldName, listSet),
        RegistrySyncAdapterFactory.CreateAdapters)
    {
    }

    //კონსოლიდან არჩევა და ადაპტერების აწყობა პარამეტრებადაა გამოტანილი, რომ ტესტებმა პასუხები და fake ადაპტერები
    //თვითონ მიაწოდონ
    internal SyncRegistryCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, Func<string, CliMenuSet, int> inputIdFromMenuList,
        Func<SupportToolsServerApiClient, SupportToolsParameters, PathMapper, RegistrySyncWarnings,
            IEnumerable<IRegistrySyncAdapter>> createAdapters) : this(logger, httpClientFactory, parametersManager,
        inputIdFromMenuList, createAdapters, null)
    {
    }

    // ReSharper disable once ConvertToPrimaryConstructor
    internal SyncRegistryCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, Func<string, CliMenuSet, int> inputIdFromMenuList,
        Func<SupportToolsServerApiClient, SupportToolsParameters, PathMapper, RegistrySyncWarnings,
            IEnumerable<IRegistrySyncAdapter>> createAdapters, string? collectionName) : base(
        collectionName is null ? CommandName : GetCollectionCommandName(collectionName), EMenuAction.Reload)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _inputIdFromMenuList = inputIdFromMenuList;
        _createAdapters = createAdapters;
        _collectionName = collectionName;
    }

    //ერთი კოლექციის სინქრონიზაცია (RegistryCollections-ის სახელით), მაგალითად Gits-ის სიის მენიუდან
    public static SyncRegistryCliMenuCommand ForCollection(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, string collectionName)
    {
        return new SyncRegistryCliMenuCommand(logger, httpClientFactory, parametersManager,
            (fieldName, listSet) => MenuInputer.InputIdFromMenuList(fieldName, listSet),
            RegistrySyncAdapterFactory.CreateAdapters, collectionName);
    }

    internal static string GetCollectionCommandName(string collectionName)
    {
        return $"Sync {collectionName} With SupportToolsServer...";
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        (SupportToolsServerApiClient ApiClient, string ServerAddress)? connection =
            await Connect(parameters, cancellationToken);
        if (connection is null)
        {
            return false;
        }

        List<string> keysDifferingByCase =
            RegistrySyncPreflight.FindKeysDifferingOnlyByCase(parameters, _collectionName);
        if (keysDifferingByCase.Count > 0)
        {
            WriteError("These local keys differ only by case, so they cannot be matched with the server records. " +
                       "Rename or remove the extra records, then sync again:");
            foreach (string keys in keysDifferingByCase)
            {
                WriteError($"  {keys}");
            }

            return false;
        }

        var warnings = new RegistrySyncWarnings();
        var pathMapper = new PathMapper(parameters.PathMappings);
        //ბრძანება თვითონ ირჩევს, რა გაიგზავნოს, ამიტომ მისი შენახვები ავტომატურ Push-ს (D2) არ იწვევს
        IParametersManager parametersManager = AutoSyncParametersManager.WithoutAutoSync(_parametersManager);
        List<IRegistrySyncAdapter> adapters =
        [
            .. _createAdapters(connection.Value.ApiClient, parameters, pathMapper, warnings).Where(x =>
                _collectionName is null ||
                string.Equals(x.CollectionName, _collectionName, StringComparison.OrdinalIgnoreCase))
        ];
        var engine = new RegistrySyncEngine(adapters, parametersManager)
        {
            //მდგომარეობა ამ სერვერისაა: ავტომატური სინქრონიზაცია მხოლოდ მასთან მუშაობს
            ServerAddress = connection.Value.ServerAddress
        };
        var session = new RegistrySyncSession(engine, warnings, pathMapper, parametersManager, _inputIdFromMenuList,
            _logger);
        return await session.Run(cancellationToken);
    }

    //SupportToolsServer-ის ApiClient და კავშირის შემოწმება მსუბუქი მოთხოვნით. კლიენტი კონსოლის გარეშე იქმნება:
    //შეცდომისას ის მოთხოვნის ტანს არ ბეჭდავს (ტანში საიდუმლოები და შაბლონების შიგთავსია), შეცდომებს ბრძანება თვითონ
    //აჩვენებს. ApiKey არსად იბეჭდება. ბრუნდება კლიენტი და სერვერის მისამართი
    private async Task<(SupportToolsServerApiClient ApiClient, string ServerAddress)?> Connect(
        SupportToolsParameters parameters, CancellationToken cancellationToken)
    {
        string? apiClientName = parameters.SupportToolsServerWebApiClientName;
        if (string.IsNullOrWhiteSpace(apiClientName))
        {
            WriteError("SupportToolsServerWebApiClientName is not set: choose the ApiClient of SupportToolsServer in " +
                       "Support Tools Parameters Editor");
            return null;
        }

        ApiClientSettings? apiClientSettings = parameters.ApiClients.GetValueOrDefault(apiClientName);
        if (apiClientSettings is null)
        {
            WriteError($"ApiClient {apiClientName} (SupportToolsServerWebApiClientName) does not exist");
            return null;
        }

        string? server = apiClientSettings.Server;
        if (string.IsNullOrWhiteSpace(server))
        {
            WriteError($"The server address of ApiClient {apiClientName} is not set");
            return null;
        }

        var apiClient = new SupportToolsServerApiClient(_logger, _httpClientFactory, server,
            apiClientSettings.ApiKey, false);
        Result<List<StsEnvironmentDataModel>> connectionCheck = await apiClient.GetEnvironments(cancellationToken);
        if (connectionCheck.IsSuccess)
        {
            return (apiClient, server);
        }

        WriteConnectionError(apiClientName, server, connectionCheck.Error);
        return null;
    }

    private void WriteConnectionError(string apiClientName, string server, Error error)
    {
        WriteError(
            $"Cannot connect to SupportToolsServer (ApiClient {apiClientName}, address {server}): {error.Description}");
        if (IsUnauthorized(error))
        {
            WriteError($"The server refused the API key: check the ApiKey of ApiClient {apiClientName}");
        }
        else if (error.Code == RegistrySyncServerErrorCodes.RequestFailed)
        {
            WriteError("The server does not answer: check the address and that SupportToolsServer runs");
        }
    }

    //შეცდომის ყოველ სტრიქონზე პაუზა არ არის: ბრძანების შემდეგ მენიუ თვითონ ჩერდება (Reload)
    private void WriteError(string message)
    {
        StShared.WriteErrorLine(message, true, _logger, false);
    }

    //A3-ის API key-ის შემოწმება 401-ს ცარიელი ტანით აბრუნებს, რასაც ApiClient ApiReturnedAnError-ად კითხულობს
    //("...: 401 Unauthorized"). ProblemDetails-იანი 401-ის title (Unauthorized) კი შეცდომის კოდი ხდება
    private static bool IsUnauthorized(Error error)
    {
        string unauthorizedPrefix = ApiClientErrors
            .ApiReturnedAnError(((int)HttpStatusCode.Unauthorized).ToString(CultureInfo.InvariantCulture)).Description;
        return error.Code == nameof(HttpStatusCode.Unauthorized) ||
               error.Code == nameof(ApiClientErrors.ApiReturnedAnError) &&
               error.Description.StartsWith(unauthorizedPrefix, StringComparison.Ordinal);
    }
}

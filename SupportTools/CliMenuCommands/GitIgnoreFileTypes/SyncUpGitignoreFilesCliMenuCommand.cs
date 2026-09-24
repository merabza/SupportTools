using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace SupportTools.CliMenuCommands.GitIgnoreFileTypes;

public sealed class SyncUpGitignoreFilesCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncUpGitignoreFilesCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : base("Sync Up .gitignore files...", EMenuAction.Reload)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        if (!Inputer.InputBool(
                "This process will upload .gitignore records to server. Not Match records on the server will be deleted, New records will be created. Existing records will be modified as needed. are you sure?",
                false, false))
        {
            return false;
        }

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        string? folderForGitignoreFiles = parameters.FolderForGitignoreFiles;
        if (string.IsNullOrWhiteSpace(folderForGitignoreFiles))
        {
            StShared.WriteErrorLine("supportToolsParameters.FolderForGitignoreFiles is empty", true, _logger);
            return false;
        }

        //ყველა ფაილი უნდა არსებობდეს, რადგან სერვერზე სიაში არარსებული ჩანაწერები წაიშლება
        var gitIgnoreFileTypes = new List<StsGitIgnoreFileTypeDataModel>();
        foreach (string gitIgnoreModelName in parameters.GitIgnorePatterns)
        {
            string fileName =
                SupportToolsParameters.GetGitIgnoreModelFilePath(folderForGitignoreFiles, gitIgnoreModelName);
            if (!File.Exists(fileName))
            {
                StShared.WriteErrorLine($".gitignore file {fileName} does not exists", true, _logger);
                return false;
            }

            string content = await File.ReadAllTextAsync(fileName, cancellationToken);
            //კლიენტზე Id არ ინახება. merge=false-ის დროს სერვერი ძველ ჩანაწერებს წაშლის და ახლებს ჩაწერს
            gitIgnoreFileTypes.Add(new StsGitIgnoreFileTypeDataModel
            {
                Id = Guid.NewGuid(), Name = gitIgnoreModelName, Content = content
            });
        }

        SupportToolsServerApiClient? supportToolsServerApiClient =
            parameters.GetSupportToolsServerApiClient(_logger, _httpClientFactory);
        if (supportToolsServerApiClient is null)
        {
            StShared.WriteErrorLine("supportToolsServerApiClient is null", true, _logger);
            return false;
        }

        Result result =
            await supportToolsServerApiClient.SyncUpGitIgnoreFileTypes(gitIgnoreFileTypes, false, cancellationToken);
        if (result.IsFailure)
        {
            result.Error.PrintErrorsOnConsole();
            return false;
        }

        Console.WriteLine($"{gitIgnoreFileTypes.Count} .gitignore files uploaded to server");
        return true;
    }
}

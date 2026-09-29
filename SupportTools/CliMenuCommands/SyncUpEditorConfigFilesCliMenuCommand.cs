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

namespace SupportTools.CliMenuCommands;

//.editorconfig შაბლონების ატვირთვა SupportToolsServer-ის ბაზაში, ისევე როგორც .gitignore შაბლონებისთვის.
//სერვერზე სიაში არარსებული შაბლონები წაიშლება
public sealed class SyncUpEditorConfigFilesCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Func<string, bool, bool> _inputBool;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    public SyncUpEditorConfigFilesCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : this(logger, httpClientFactory, parametersManager,
        (fieldName, defaultValue) => Inputer.InputBool(fieldName, defaultValue, false))
    {
    }

    //კონსოლიდან შეყვანა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal SyncUpEditorConfigFilesCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, Func<string, bool, bool> inputBool) : base(
        "Sync Up .editorconfig files...", EMenuAction.Reload)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _inputBool = inputBool;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        if (!_inputBool(
                "This process will upload .editorconfig records to server. Not Match records on the server will be deleted, New records will be created. Existing records will be modified as needed. are you sure?",
                false))
        {
            return false;
        }

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        string? folderForEditorConfigFiles = parameters.FolderForEditorConfigFiles;
        if (string.IsNullOrWhiteSpace(folderForEditorConfigFiles))
        {
            StShared.WriteErrorLine("supportToolsParameters.FolderForEditorConfigFiles is empty", true, _logger);
            return false;
        }

        //ყველა ფაილი უნდა არსებობდეს, რადგან სერვერზე სიაში არარსებული ჩანაწერები წაიშლება
        var editorConfigFileTypes = new List<StsEditorConfigFileTypeDataModel>();
        foreach (string editorConfigPatternName in parameters.EditorConfigPatterns)
        {
            string fileName =
                SupportToolsParameters.GetEditorConfigPatternFilePath(folderForEditorConfigFiles,
                    editorConfigPatternName);
            if (!File.Exists(fileName))
            {
                StShared.WriteErrorLine($".editorconfig file {fileName} does not exist", true, _logger);
                return false;
            }

            string content = await File.ReadAllTextAsync(fileName, cancellationToken);
            editorConfigFileTypes.Add(new StsEditorConfigFileTypeDataModel
            {
                Name = editorConfigPatternName, Content = content
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
            await supportToolsServerApiClient.SyncUpEditorConfigFileTypes(editorConfigFileTypes, false,
                cancellationToken);
        if (result.IsFailure)
        {
            result.Error.PrintErrorsOnConsole();
            return false;
        }

        Console.WriteLine($"{editorConfigFileTypes.Count} .editorconfig files uploaded to server");
        return true;
    }
}

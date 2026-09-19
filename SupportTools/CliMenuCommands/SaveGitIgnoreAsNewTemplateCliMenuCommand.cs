using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using LibGitData;
using LibGitWork.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.CliMenuCommands;

//მიმდინარე გიტის .gitignore ფაილის ახალ შაბლონად შენახვა: შაბლონის სახელი ემატება GitIgnorePatterns სიას,
//ფაილი კი კოპირდება შაბლონების ფოლდერში. თვითონ გიტის GitIgnorePatternName არ იცვლება
public sealed class SaveGitIgnoreAsNewTemplateCliMenuCommand : CliMenuCommand
{
    private readonly EGitCol _gitCol;
    private readonly string _gitProjectName;
    private readonly Func<string, bool, bool> _inputBool;
    private readonly Func<string, string?, string?> _inputText;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;

    public SaveGitIgnoreAsNewTemplateCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        string projectName, string gitProjectName, EGitCol gitCol) : this(logger, parametersManager, projectName,
        gitProjectName, gitCol, (fieldName, defaultValue) => Inputer.InputText(fieldName, defaultValue),
        (fieldName, defaultValue) => Inputer.InputBool(fieldName, defaultValue, false))
    {
    }

    //კონსოლიდან შეყვანა პარამეტრებადაა გამოტანილი, რომ ტესტებმა პასუხები თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal SaveGitIgnoreAsNewTemplateCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        string projectName, string gitProjectName, EGitCol gitCol, Func<string, string?, string?> inputText,
        Func<string, bool, bool> inputBool) : base("Save .gitignore as New Template", EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitProjectName = gitProjectName;
        _gitCol = gitCol;
        _inputText = inputText;
        _inputBool = inputBool;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        string? folderForGitignoreFiles = parameters.FolderForGitignoreFiles;
        if (string.IsNullOrWhiteSpace(folderForGitignoreFiles))
        {
            StShared.WriteErrorLine("FolderForGitignoreFiles is not specified", true, _logger);
            return false;
        }

        //გიტის ლოკალური ფოლდერი დგინდება ისევე, როგორც სინქრონიზაციისას
        var gitSyncParameters =
            GitSyncParameters.Create(_logger, parameters, _projectName, _gitCol, _gitProjectName, true);
        if (gitSyncParameters is null)
        {
            return false;
        }

        string gitIgnoreFileName = Path.Combine(gitSyncParameters.GitsFolder,
            gitSyncParameters.GitData.GitProjectFolderName, ".gitignore");
        if (!File.Exists(gitIgnoreFileName))
        {
            StShared.WriteErrorLine($"File {gitIgnoreFileName} does not exist", true, _logger);
            return false;
        }

        string templateName = InputNewTemplateName(parameters.GitIgnorePatterns);

        string templateFileName =
            SupportToolsParameters.GetGitIgnoreModelFilePath(folderForGitignoreFiles, templateName);

        //სახელი სიაში არ არის, მაგრამ ფოლდერში ამ სახელის ფაილი შეიძლება მაინც იყოს
        if (File.Exists(templateFileName) &&
            !_inputBool($"File {templateFileName} exists, overwrite?", false))
        {
            return false;
        }

        if (!FileStat.CreatePrevFolderIfNotExists(templateFileName, true, _logger))
        {
            return false;
        }

        File.Copy(gitIgnoreFileName, templateFileName, true);

        parameters.GitIgnorePatterns.Add(templateName);
        return await _parametersManager.Save(parameters, $".gitignore template {templateName} created", null,
            cancellationToken);
    }

    //სახელი არ შეიძლება იყოს ცარიელი ან უკვე გამოყენებული. შედარება რეგისტრის გარეშეა, რადგან Windows-ზე
    //ფაილის სახელი რეგისტრს არ არჩევს და მხოლოდ რეგისტრით განსხვავებული სახელი არსებულ შაბლონს გადააწერდა
    private string InputNewTemplateName(List<string> gitIgnorePatterns)
    {
        while (true)
        {
            string templateName =
                (_inputText("New .gitignore Template Name", _gitProjectName) ?? string.Empty).Trim();

            if (templateName.Length == 0)
            {
                StShared.WriteErrorLine("Template name is empty", true, null, false);
            }
            else if (templateName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                StShared.WriteErrorLine($"Template name {templateName} contains invalid file name characters", true,
                    null, false);
            }
            else if (gitIgnorePatterns.Contains(templateName, StringComparer.OrdinalIgnoreCase))
            {
                StShared.WriteErrorLine($"Template with name {templateName} already exists", true, null, false);
            }
            else
            {
                return templateName;
            }
        }
    }
}

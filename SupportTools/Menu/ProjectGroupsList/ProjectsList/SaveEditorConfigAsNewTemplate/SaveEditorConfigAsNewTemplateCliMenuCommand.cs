using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SaveEditorConfigAsNewTemplate;

//პროექტის .editorconfig ფაილის ახალ შაბლონად შენახვა: შაბლონის სახელი ემატება EditorConfigPatterns სიას,
//ფაილი კი კოპირდება შაბლონების ფოლდერში. თვითონ პროექტის EditorConfigPatternName არ იცვლება
public sealed class SaveEditorConfigAsNewTemplateCliMenuCommand : CliMenuCommand
{
    private readonly Func<string, bool, bool> _inputBool;
    private readonly Func<string, string?, string?> _inputText;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;

    public SaveEditorConfigAsNewTemplateCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        string projectName) : this(logger, parametersManager, projectName,
        (fieldName, defaultValue) => Inputer.InputText(fieldName, defaultValue),
        (fieldName, defaultValue) => Inputer.InputBool(fieldName, defaultValue, false))
    {
    }

    //კონსოლიდან შეყვანა პარამეტრებადაა გამოტანილი, რომ ტესტებმა პასუხები თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal SaveEditorConfigAsNewTemplateCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        string projectName, Func<string, string?, string?> inputText, Func<string, bool, bool> inputBool) : base(
        "Save .editorconfig as New Template", EMenuAction.Reload, EMenuAction.Reload, projectName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _inputText = inputText;
        _inputBool = inputBool;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        string? folderForEditorConfigFiles = parameters.FolderForEditorConfigFiles;
        if (string.IsNullOrWhiteSpace(folderForEditorConfigFiles))
        {
            StShared.WriteErrorLine("FolderForEditorConfigFiles is not specified", true, _logger);
            return false;
        }

        ProjectModel? project = parameters.GetProject(_projectName);
        if (project is null)
        {
            StShared.WriteErrorLine($"Project {_projectName} does not found", true, _logger);
            return false;
        }

        string? editorConfigFileName = project.EditorConfigFileName();
        if (editorConfigFileName is null)
        {
            StShared.WriteErrorLine($"Project {_projectName} does not have a solution file", true, _logger);
            return false;
        }

        if (!File.Exists(editorConfigFileName))
        {
            StShared.WriteErrorLine($"File {editorConfigFileName} does not exist", true, _logger);
            return false;
        }

        string templateName = InputNewTemplateName(parameters.EditorConfigPatterns);

        string templateFileName =
            SupportToolsParameters.GetEditorConfigPatternFilePath(folderForEditorConfigFiles, templateName);

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

        File.Copy(editorConfigFileName, templateFileName, true);

        parameters.EditorConfigPatterns.Add(templateName);
        return await _parametersManager.Save(parameters, $".editorconfig template {templateName} created", null,
            cancellationToken);
    }

    //სახელი არ შეიძლება იყოს ცარიელი ან უკვე გამოყენებული. შედარება რეგისტრის გარეშეა, რადგან Windows-ზე
    //ფაილის სახელი რეგისტრს არ არჩევს და მხოლოდ რეგისტრით განსხვავებული სახელი არსებულ შაბლონს გადააწერდა
    private string InputNewTemplateName(List<string> editorConfigPatterns)
    {
        while (true)
        {
            string templateName = (_inputText("New .editorconfig Template Name", _projectName) ?? string.Empty).Trim();

            if (templateName.Length == 0)
            {
                StShared.WriteErrorLine("Template name is empty", true, null, false);
            }
            else if (templateName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                StShared.WriteErrorLine($"Template name {templateName} contains invalid file name characters", true,
                    null, false);
            }
            else if (editorConfigPatterns.Contains(templateName, StringComparer.OrdinalIgnoreCase))
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

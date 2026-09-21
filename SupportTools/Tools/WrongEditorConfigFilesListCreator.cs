using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Tools;

//პროექტების .editorconfig ფაილების შედარება პროექტში მითითებულ შაბლონთან (ProjectModel.EditorConfigPatternName)
public sealed class WrongEditorConfigFilesListCreator
{
    private readonly ILogger? _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public WrongEditorConfigFilesListCreator(ILogger? logger, IParametersManager parametersManager)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    //აბრუნებს: .editorconfig ფაილის სახელი => შაბლონის შიგთავსი, რომელიც ამ ფაილში უნდა იყოს
    public Dictionary<string, string> Create()
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;

        string? folderForEditorConfigFiles = supportToolsParameters.FolderForEditorConfigFiles;
        if (string.IsNullOrWhiteSpace(folderForEditorConfigFiles))
        {
            StShared.WriteErrorLine("supportToolsParameters.FolderForEditorConfigFiles is empty", true, _logger,
                false);
            return [];
        }

        List<string> editorConfigPatterns = supportToolsParameters.EditorConfigPatterns;

        //null ნიშნავს, რომ შაბლონის ფაილი არ არსებობს
        var editorConfigTemplateFileContents = new Dictionary<string, string?>();
        var wrongEditorConfigFilesList = new Dictionary<string, string>();

        foreach ((string _, ProjectModel project) in supportToolsParameters.Projects.OrderBy(o => o.Key))
        {
            //შაბლონის მითითება აუცილებელი არ არის. შაბლონის გარეშე პროექტი არ მოწმდება
            string? editorConfigPatternName = project.EditorConfigPatternName;
            if (string.IsNullOrWhiteSpace(editorConfigPatternName) ||
                !editorConfigPatterns.Contains(editorConfigPatternName))
            {
                continue;
            }

            //პროექტი, რომელიც ამ კომპიუტერზე კლონირებული არ არის, არ მოწმდება
            string? editorConfigFileName = project.EditorConfigFileName();
            if (editorConfigFileName is null || !Directory.Exists(Path.GetDirectoryName(editorConfigFileName)))
            {
                continue;
            }

            if (!editorConfigTemplateFileContents.TryGetValue(editorConfigPatternName,
                    out string? editorConfigTemplateFileContent))
            {
                string editorConfigTemplateFileName =
                    SupportToolsParameters.GetEditorConfigPatternFilePath(folderForEditorConfigFiles,
                        editorConfigPatternName);

                if (File.Exists(editorConfigTemplateFileName))
                {
                    editorConfigTemplateFileContent = File.ReadAllText(editorConfigTemplateFileName);
                }
                else
                {
                    StShared.WriteErrorLine($"{editorConfigTemplateFileName} is not exists", true, _logger, false);
                }

                editorConfigTemplateFileContents.Add(editorConfigPatternName, editorConfigTemplateFileContent);
            }

            if (editorConfigTemplateFileContent is null)
            {
                continue;
            }

            if (!File.Exists(editorConfigFileName) ||
                File.ReadAllText(editorConfigFileName) != editorConfigTemplateFileContent)
            {
                wrongEditorConfigFilesList.TryAdd(editorConfigFileName, editorConfigTemplateFileContent);
            }
        }

        return wrongEditorConfigFilesList;
    }
}

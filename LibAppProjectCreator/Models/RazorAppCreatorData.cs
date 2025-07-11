using System.Collections.Generic;
using LibDotnetWork;
using SupportToolsData.Models;

namespace LibAppProjectCreator.Models;

public sealed class RazorAppCreatorData
{
    private RazorAppCreatorData(AppCreatorBaseData appCreatorBaseData, ProjectForCreate mainProjectData)
    {
        MainProjectData = mainProjectData;
        AppCreatorBaseData = appCreatorBaseData;
    }

    public AppCreatorBaseData AppCreatorBaseData { get; }
    public ProjectForCreate MainProjectData { get; }

    public static RazorAppCreatorData Create(AppCreatorBaseData appCreatorBaseData, string projectName,
        TemplateModel template)
    {
        var projectFolders = new List<string> { "Properties" };
        if (!template.UseDatabase)
            projectFolders.Add("Models");
        if (template.UseMenu)
            projectFolders.Add("MenuCommands");

        //მთავარი პროექტი
        var mainProjectData = ProjectForCreate.Create(appCreatorBaseData.SolutionPath, projectName, projectName,
            EDotnetProjectType.Console, string.Empty, "Program", [.. projectFolders]);

        return new RazorAppCreatorData(appCreatorBaseData, mainProjectData);
    }
}
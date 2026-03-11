using System.Collections.Generic;
using LibDotnetWork;
using SupportToolsData.Models;

namespace LibAppProjectCreator.Models;

public sealed class RazorAppCreatorData
{
    private RazorAppCreatorData(AppCreatorBaseData appCreatorBaseData, ProjectForCreate mainProjectData,
        string? mediatRLicenseKey, bool useReact, bool useCarcass, bool useDatabase, bool useIdentity,
        bool useReCounter, bool useSignalR, bool useFluentValidation, string? dbPartProjectName)
    {
        MainProjectData = mainProjectData;
        AppCreatorBaseData = appCreatorBaseData;
        UseReact = useReact;
        UseCarcass = useCarcass;
        UseDatabase = useDatabase;
        UseIdentity = useIdentity;
        UseReCounter = useReCounter;
        UseSignalR = useSignalR;
        UseFluentValidation = useFluentValidation;
        DbPartProjectName = dbPartProjectName;
        MediatRLicenseKey = mediatRLicenseKey;
    }

    public AppCreatorBaseData AppCreatorBaseData { get; }
    public ProjectForCreate MainProjectData { get; }

    public string? MediatRLicenseKey { get; }
    public bool UseReact { get; }
    public bool UseCarcass { get; }
    public bool UseDatabase { get; }
    public bool UseIdentity { get; }
    public bool UseReCounter { get; }
    public bool UseSignalR { get; }
    public bool UseFluentValidation { get; }
    public string? DbPartProjectName { get; }

    public static RazorAppCreatorData Create(AppCreatorBaseData appCreatorBaseData, string projectName,
        string? dbPartProjectName, string? mediatRLicenseKey, TemplateModel template)
    {
        var projectFolders = new List<string> { "Properties" };
        if (!template.UseDatabase)
        {
            projectFolders.Add("Models");
        }

        if (template.UseMenu)
        {
            projectFolders.Add("MenuCommands");
        }

        //მთავარი პროექტი
        var mainProjectData = ProjectForCreate.Create(appCreatorBaseData.SolutionPath, projectName, projectName,
            EDotnetProjectType.Console, string.Empty, "Program", [.. projectFolders]);

        return new RazorAppCreatorData(appCreatorBaseData, mainProjectData, mediatRLicenseKey, template.UseReact,
            template.UseCarcass, template.UseDatabase, template.UseIdentity, template.UseReCounter, template.UseSignalR,
            template.UseFluentValidation, dbPartProjectName);
    }
}

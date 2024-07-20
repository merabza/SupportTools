using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppProjectCreator.Models;

public sealed class ApiAppCreatorData
{
    private ApiAppCreatorData(string? dbPartPath,
        AppCreatorBaseData appCreatorBaseData, ProjectForCreate mainProjectData, bool useReact, bool useCarcass,
        bool useDatabase, bool useDbPartFolderForDatabaseProjects, bool useIdentity, bool useBackgroundTasks,
        bool useSignalR, bool useFluentValidation,
        string? reactTemplateName, ProjectForCreate databaseProjectData, ProjectForCreate dbMigrationProjectData,
        ProjectForCreate libProjectRepositoriesProjectData, ProjectForCreate masterDataLoadersProjectData)
    {
        AppCreatorBaseData = appCreatorBaseData;
        MainProjectData = mainProjectData;
        UseReact = useReact;
        UseCarcass = useCarcass;
        DbPartPath = dbPartPath;
        //ReactClientPath = reactClientPath;
        UseDatabase = useDatabase;
        UseDbPartFolderForDatabaseProjects = useDbPartFolderForDatabaseProjects;
        UseIdentity = useIdentity;
        UseBackgroundTasks = useBackgroundTasks;
        UseSignalR = useSignalR;
        UseFluentValidation = useFluentValidation;
        ReactTemplateName = reactTemplateName;
        DatabaseProjectData = databaseProjectData;
        DbMigrationProjectData = dbMigrationProjectData;
        LibProjectRepositoriesProjectData = libProjectRepositoriesProjectData;
        MasterDataLoadersProjectData = masterDataLoadersProjectData;
    }

    public bool UseReact { get; set; }
    public bool UseCarcass { get; set; }
    public bool UseDatabase { get; set; }
    public bool UseDbPartFolderForDatabaseProjects { get; }
    public bool UseIdentity { get; set; }
    public bool UseBackgroundTasks { get; set; }
    public bool UseSignalR { get; set; }
    public bool UseFluentValidation { get; }
    public string? ReactTemplateName { get; }
    //public string ReactClientPath { get; }
    public string? DbPartPath { get; }
    public AppCreatorBaseData AppCreatorBaseData { get; }
    public ProjectForCreate MainProjectData { get; }
    public ProjectForCreate LibProjectRepositoriesProjectData { get; }
    public ProjectForCreate MasterDataLoadersProjectData { get; }
    public ProjectForCreate DatabaseProjectData { get; }
    public ProjectForCreate DbMigrationProjectData { get; }


    public static ApiAppCreatorData? CreateApiAppCreatorData(ILogger logger,
        AppCreatorBaseData appCreatorBaseData, string projectName, TemplateModel template)
    {
        if (template is { UseCarcass: true, UseDatabase: false })
        {
            StShared.WriteErrorLine("Use Carcass without database is not allowed", true, logger);
            return null;
        }

        if (template is { UseCarcass: true, UseIdentity: false })
        {
            StShared.WriteErrorLine("Use Carcass without Identity is not allowed", true, logger);
            return null;
        }

        if (template.UseReact && string.IsNullOrWhiteSpace(template.ReactTemplateName))
        {
            StShared.WriteErrorLine("if Use React, ReactTemplateName must be specified", true, logger);
            return null;
        }

        //if (par.TempFolderPath is null)
        //{
        //    StShared.WriteErrorLine("Temp folder does not specified", true, logger);
        //    return null;
        //}

        ////დროებით ფოლდერი არ უნდა ემთხვეოდეს სამუშაო ფოლდერს
        //if (FileStat.NormalizePath(par.WorkFolderPath) == FileStat.NormalizePath(par.TempFolderPath))
        //{
        //    StShared.WriteErrorLine($"Work and Temp Folders are same {par.TempFolderPath}.", true, logger);
        //    return null;
        //}

        ////დროებით ფოლდერი არ უნდა ემთხვეოდეს საიდუმლოებების ფოლდერს
        //if (FileStat.NormalizePath(par.SecurityWorkFolderPath) == FileStat.NormalizePath(par.TempFolderPath))
        //{
        //    StShared.WriteErrorLine($"Security and Temp Folders are same {par.TempFolderPath}.", true, logger);
        //    return null;
        //}

        ////დროებით ფოლდერი არ უნდა ემთხვეოდეს პროექტის სამუშაო ფოლდერს
        //if (FileStat.NormalizePath(appCreatorBaseData.WorkPath) == FileStat.NormalizePath(par.TempFolderPath))
        //{
        //    StShared.WriteErrorLine($"project Work and Temp Folders are same {par.TempFolderPath}.", true,
        //        logger);
        //    return null;
        //}

        ////დროებით ფოლდერი არ უნდა ემთხვეოდეს პროექტის საიდუმლოებების ფოლდერს
        //if (FileStat.NormalizePath(appCreatorBaseData.SecurityPath) == FileStat.NormalizePath(par.TempFolderPath))
        //{
        //    StShared.WriteErrorLine($"project Work and Temp Folders are same {par.TempFolderPath}.", true,
        //        logger);
        //    return null;
        //}

        ////შევამოწმოთ და თუ არ არსებობს შევქმნათ დროებითი სამუშაოებისათვის განკუთვნილი ფოლდერი
        //if (!StShared.CreateFolder(par.TempFolderPath, true))
        //{
        //    StShared.WriteErrorLine($"Cannot create temp Folder {par.TempFolderPath}", true, logger);
        //    return null;
        //}

        ////შევამოწმოთ არსებობს თუ არა უკვე პროექტის ფოლდერი
        //var projectTempPath = Path.Combine(par.TempFolderPath, par.ProjectName.ToLower());
        ////foldersForCreate.Add(tempPath);
        //if (!FileStat.CheckRequiredFolder(true, projectTempPath, false))
        //    return null;

        var projectFolders = new List<string>
        {
            "Properties",
            "Models",
            "Installers"
        };

        //if (template.UseReact)
        //{
        //    projectFolders.Add("Pages");
        //    projectFolders.Add("ClientApp");
        //    //projectFolders.Add("ClientApp/public");
        //}

        //მთავარი პროექტი
        var mainProjectData = ProjectForCreate.Create(appCreatorBaseData.SolutionPath, projectName, projectName,
            EDotnetProjectType.Web, template.UseHttps ? string.Empty : "--no-https", "Program", [.. projectFolders],
            template.UseReact);

        //დავიანგარიშოთ კლიენტის ფოლდერის სრული გზა
        //var reactClientPath = Path.Combine(mainProjectData.ProjectFullPath, "ClientApp");
        //შევამოწმოთ არსებობს თუ არა უკვე პროექტის ფოლდერი
        //AppCreatorBaseData.CheckRequiredFolder(reactClientPath, false);


        var libProjectRepositoriesProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            $"Lib{projectName}Repositories", []);

        var databaseProjectFolders = new List<string>
        {
            "Models",
            "Installers"
        };

        if (template.UseCarcass)
            databaseProjectFolders.Add("QueryModels");

        var dbPartFolderName = $"{projectName}DbPart";

        var dbPartPath = template.UseDbPartFolderForDatabaseProjects
            ? Path.Combine(appCreatorBaseData.WorkPath, dbPartFolderName)
            : appCreatorBaseData.SolutionPath;

        var dbPartSolutionFolderName = template.UseDbPartFolderForDatabaseProjects ? dbPartFolderName : null;

        var databaseProjectData = ProjectForCreate.CreateClassLibProject(dbPartPath, $"{projectName}Db",
            [.. databaseProjectFolders], dbPartSolutionFolderName);

        var dbMigrationProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            $"{projectName}DbMigration", ["Migrations"]);

        var masterDataLoadersProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            $"{projectName}MasterDataLoaders", ["Installers"]);

        return new ApiAppCreatorData(dbPartPath, appCreatorBaseData, mainProjectData, template.UseReact,
            template.UseCarcass, template.UseDatabase, template.UseDbPartFolderForDatabaseProjects,
            template.UseIdentity, template.UseBackgroundTasks, template.UseSignalR, template.UseFluentValidation,
            template.ReactTemplateName, databaseProjectData, dbMigrationProjectData, libProjectRepositoriesProjectData,
            masterDataLoadersProjectData);
    }
}
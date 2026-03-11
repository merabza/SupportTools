using SystemTools.SystemToolsShared;

namespace SupportToolsData.Models;

public sealed class TemplateModel : ItemData
{
    public ESupportProjectType SupportProjectType { get; set; }

    public string? TestProjectName { get; set; }

    //public string? TestDbPartProjectName { get; set; }
    public string? TestProjectShortName { get; set; }

    //Console parameters
    public bool UseDatabase { get; set; }
    public bool UseDbPartFolderForDatabaseProjects { get; set; }
    public bool UseMenu { get; set; }

    //Api parameters
    //public string MediatRLicenseKey { get; set; }
    public bool UseHttps { get; set; }
    public bool UseReact { get; set; }
    public bool UseCarcass { get; set; }
    public bool UseIdentity { get; set; }
    public bool UseReCounter { get; set; }
    public bool UseSignalR { get; set; }
    public bool UseFluentValidation { get; set; }

    //react parameters
    public string? ReactTemplateName { get; set; }
}

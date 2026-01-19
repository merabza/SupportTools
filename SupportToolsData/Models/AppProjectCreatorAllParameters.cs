using System.Collections.Generic;
using ParametersManagement.LibParameters;

namespace SupportToolsData.Models;

public sealed class AppProjectCreatorAllParameters : IParameters
{
    public int IndentSize { get; set; }
    public string? FakeHostProjectName { get; set; }
    public string? ProjectsFolderPathReal { get; set; }
    public string? ProductionServerName { get; set; }
    public string? ProductionEnvironmentName { get; set; }
    public string? SecretsFolderPathReal { get; set; }
    public string? DeveloperDbConnectionName { get; set; }
    public string? DatabaseExchangeFileStorageName { get; set; }
    public string? UseSmartSchema { get; set; }

    public Dictionary<string, TemplateModel> Templates { get; set; } = new();

    public bool CheckBeforeSave()
    {
        return true;
    }

    public TemplateModel? GetTemplate(string templateName)
    {
        return Templates.GetValueOrDefault(templateName);
    }
}
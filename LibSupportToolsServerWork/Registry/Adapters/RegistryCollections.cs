namespace LibSupportToolsServerWork.Registry.Adapters;

//რეესტრის კოლექციები. სახელი სინქრონიზაციის მდგომარეობის გასაღებია (RegistrySyncState.Collections), ამიტომ არ
//იცვლება. Order დამოკიდებულების რიგია (README §4.3): მითითებულ კოლექციას ნაკლები აქვს, ვიდრე მიმთითებელს.
//Projects (C4) ყველაზე ბოლოა
public static class RegistryCollections
{
    public const string Environments = "Environments";
    public const string RunTimes = "RunTimes";
    public const string NpmPackages = "NpmPackages";
    public const string ReactAppTemplates = "ReactAppTemplates";
    public const string DotnetTools = "DotnetTools";
    public const string SmartSchemas = "SmartSchemas";
    public const string FileStorages = "FileStorages";
    public const string ApiClients = "ApiClients";
    public const string DatabaseServerConnections = "DatabaseServerConnections";
    public const string Servers = "Servers";
    public const string GitIgnorePatterns = "GitIgnorePatterns";
    public const string Gits = "Gits";
    public const string EditorConfigPatterns = "EditorConfigPatterns";
    public const string ProjectTemplates = "ProjectTemplates";
    public const string GlobalSettings = "GlobalSettings";
    public const string ProjectCreatorSettings = "ProjectCreatorSettings";

    //ცნობარები და რესურსები სხვა კოლექციას არ მიმართავს
    public const int EnvironmentsOrder = 10;
    public const int RunTimesOrder = 20;
    public const int NpmPackagesOrder = 30;
    public const int ReactAppTemplatesOrder = 40;
    public const int DotnetToolsOrder = 50;
    public const int SmartSchemasOrder = 60;
    public const int FileStoragesOrder = 70;
    public const int ApiClientsOrder = 80;

    //DbWebAgentName → ApiClients
    public const int DatabaseServerConnectionsOrder = 90;

    //Runtime → RunTimes; WebAgentName, WebAgentInstallerName → ApiClients
    public const int ServersOrder = 100;

    public const int GitIgnorePatternsOrder = 110;

    //GitIgnorePatternName → GitIgnorePatterns
    public const int GitsOrder = 120;

    public const int EditorConfigPatternsOrder = 130;

    //ReactTemplateName → ReactAppTemplates
    public const int ProjectTemplatesOrder = 140;

    //→ FileStorages, SmartSchemas, ApiClients
    public const int GlobalSettingsOrder = 150;

    //→ Servers, Environments, DatabaseServerConnections, FileStorages, SmartSchemas
    public const int ProjectCreatorSettingsOrder = 160;
}

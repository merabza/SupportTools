using System.Threading;
using SupportToolsServerApiContracts.Models;

namespace LibAppProjectCreator.CodeCreators;

public sealed class Clones
{
    private static Clones? _pInstance;
    private static readonly Lock SyncRoot = new();

    private Clones()
    {
        const string cSharp = "CSharp";
        const string appCliTools = "AppCliTools";
        const string backendCarcass = "BackendCarcass";
        const string databaseTools = "DatabaseTools";
        const string parametersManagement = "ParametersManagement";
        const string systemTools = "SystemTools";
        const string toolsManagement = "ToolsManagement";
        const string webSystemTools = "WebSystemTools";

        AppCliToolsGit = new GitDataDomain
        {
            GitProjectAddress = "git@github.com:merabza/AppCliTools.git",
            GitProjectFolderName = appCliTools,
            GitProjectName = appCliTools,
            GitIgnorePathName = cSharp
        };
        BackendCarcassGit = new GitDataDomain
        {
            GitProjectAddress = "git@github.com:merabza/BackendCarcass.git",
            GitProjectFolderName = backendCarcass,
            GitProjectName = backendCarcass,
            GitIgnorePathName = cSharp
        };
        DatabaseToolsGit = new GitDataDomain
        {
            GitProjectAddress = "git@github.com:merabza/DatabaseTools.git",
            GitProjectFolderName = databaseTools,
            GitProjectName = databaseTools,
            GitIgnorePathName = cSharp
        };
        ParametersManagementGit = new GitDataDomain
        {
            GitProjectAddress = "git@github.com:merabza/ParametersManagement.git",
            GitProjectFolderName = parametersManagement,
            GitProjectName = parametersManagement,
            GitIgnorePathName = cSharp
        };
        SystemToolsGit = new GitDataDomain
        {
            GitProjectAddress = "git@github.com:merabza/SystemTools.git",
            GitProjectFolderName = systemTools,
            GitProjectName = systemTools,
            GitIgnorePathName = cSharp
        };
        ToolsManagementGit = new GitDataDomain
        {
            GitProjectAddress = "git@github.com:merabza/ToolsManagement.git",
            GitProjectFolderName = toolsManagement,
            GitProjectName = toolsManagement,
            GitIgnorePathName = cSharp
        };
        WebSystemToolsGit = new GitDataDomain
        {
            GitProjectAddress = "git@github.com:merabza/WebSystemTools.git",
            GitProjectFolderName = webSystemTools,
            GitProjectName = webSystemTools,
            GitIgnorePathName = cSharp
        };
    }

    public static Clones Instance
    {
        get
        {
            if (_pInstance != null)
                return _pInstance;
            lock (SyncRoot) //thread safe singleton
            {
                _pInstance ??= new Clones();
            }

            return _pInstance;
        }
    }

    public GitDataDomain AppCliToolsGit { get; } //ბრძანებათა სტრიქონთან სამუშაო პროექტები
    public GitDataDomain BackendCarcassGit { get; } //სერვერის კარკასის პროექტები
    public GitDataDomain DatabaseToolsGit { get; } //მონაცემთა ბაზებთან სამუშაო პროექტები
    public GitDataDomain ParametersManagementGit { get; } //პარამეტრებთან სამუშაო პროექტები
    public GitDataDomain SystemToolsGit { get; } //სერთო სისტემური ინსტრუმენტების ნაკრები
    public GitDataDomain ToolsManagementGit { get; } //ინსტრუმენტებთან სამუშაო პროექტები
    public GitDataDomain WebSystemToolsGit { get; } //ინსტალერების ნაკრები
}
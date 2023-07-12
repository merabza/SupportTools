using SupportToolsData.Domain;

namespace LibAppProjectCreator.CodeCreators;

public sealed class Clones
{
    private static Clones? _pInstance;
    private static readonly object SyncRoot = new();

    private Clones()
    {
        AppCliTools = new GitDataDomain("git@github.com:merabza/AppCliTools.git", "AppCliTools");
        BackendCarcass = new GitDataDomain("git@github.com:merabza/BackendCarcass.git", "BackendCarcass");
        DatabaseTools = new GitDataDomain("git@github.com:merabza/DatabaseTools.git", "DatabaseTools");
        ParametersManagement =
            new GitDataDomain("git@github.com:merabza/ParametersManagement.git", "ParametersManagement");
        SystemTools = new GitDataDomain("git@github.com:merabza/SystemTools.git", "SystemTools");
        ToolsManagement = new GitDataDomain("git@github.com:merabza/ToolsManagement.git", "ToolsManagement");
        WebSystemTools = new GitDataDomain("git@github.com:merabza/WebSystemTools.git", "WebSystemTools");
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

    public GitDataDomain AppCliTools { get; } //ბრძანებათა სტრიქონთან სამუშაო პროექტები
    public GitDataDomain BackendCarcass { get; } //სერვერის კარკასის პროექტები
    public GitDataDomain DatabaseTools { get; } //მონაცემთა ბაზებთან სამუშაო პროექტები
    public GitDataDomain ParametersManagement { get; } //პარამეტრებთან სამუშაო პროექტები
    public GitDataDomain SystemTools { get; } //სერთო სისტემური ინსტრუმენტების ნაკრები
    public GitDataDomain ToolsManagement { get; } //ინსტრუმენტებთან სამუშაო პროექტები
    public GitDataDomain WebSystemTools { get; } //ინსტალერების ნაკრები
}
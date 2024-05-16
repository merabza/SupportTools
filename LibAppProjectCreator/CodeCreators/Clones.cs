using LibGitData.Domain;

namespace LibAppProjectCreator.CodeCreators;

public sealed class Clones
{
    private static Clones? _pInstance;
    private static readonly object SyncRoot = new();

    private Clones()
    {
        const string cSharp = "CSharp";

        AppCliTools = new GitDataDomain("git@github.com:merabza/AppCliTools.git", "AppCliTools", cSharp);
        BackendCarcass = new GitDataDomain("git@github.com:merabza/BackendCarcass.git", "BackendCarcass", cSharp);
        DatabaseTools = new GitDataDomain("git@github.com:merabza/DatabaseTools.git", "DatabaseTools", cSharp);
        ParametersManagement = new GitDataDomain("git@github.com:merabza/ParametersManagement.git",
            "ParametersManagement", cSharp);
        SystemTools = new GitDataDomain("git@github.com:merabza/SystemTools.git", "SystemTools", cSharp);
        ToolsManagement = new GitDataDomain("git@github.com:merabza/ToolsManagement.git", "ToolsManagement", cSharp);
        WebSystemTools = new GitDataDomain("git@github.com:merabza/WebSystemTools.git", "WebSystemTools", cSharp);
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
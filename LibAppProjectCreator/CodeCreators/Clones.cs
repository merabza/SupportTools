using SupportToolsData.Domain;

namespace LibAppProjectCreator.CodeCreators;

public sealed class Clones
{
    private static Clones? _pInstance;
    private static readonly object SyncRoot = new();

    private Clones()
    {
        AppCliTools = new GitDataDomain("git@bitbucket.org:mzakalashvili/appclitools.git", "AppCliTools");
        DatabaseTools = new GitDataDomain("git@bitbucket.org:mzakalashvili/databasetools.git", "DatabaseTools");
        ServerCarcassProjects = new GitDataDomain("git@bitbucket.org:mzakalashvili/servercarcassprojects2.git",
            "ServerCarcassProjects");
        SystemTools = new GitDataDomain("git@bitbucket.org:mzakalashvili/systemtools.git", "SystemTools");
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
    public GitDataDomain DatabaseTools { get; } //მონაცემთა ბაზებთან სამუშაო პროექტები
    public GitDataDomain SystemTools { get; } //სერთო სისტემური ინსტრუმენტების ნაკრები
    public GitDataDomain ServerCarcassProjects { get; } //სერვერის კარკასის პროექტები
}
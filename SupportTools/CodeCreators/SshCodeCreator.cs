//using CodeTools;
//using Microsoft.Extensions.Logging;

//// ReSharper disable ConvertToPrimaryConstructor

//namespace SupportTools.CodeCreators;

//public sealed class SshCodeCreator : CodeCreator
//{
//    private readonly string _codeFileName;
//    private readonly string _environmentName;
//    private readonly string _ftpSiteAddress;
//    private readonly string _ftpSiteDirectory;
//    private readonly int _ftpSiteLsFileOffset;
//    private readonly string _ftpSitePassword;
//    private readonly string _ftpSiteUserName;
//    private readonly int _portNumber;
//    private readonly string _projectName;
//    private readonly string _runTime;
//    private readonly string _serverSideDeployFolder;
//    private readonly string _serverSideDownloadFolder;
//    private readonly string _serverSideServiceUserName;
//    private readonly string _settingsFileName;

//    public SshCodeCreator(ILogger logger, string placePath, string codeFileName, int portNumber, string ftpSiteAddress,
//        string ftpSiteUserName, string ftpSitePassword, string ftpSiteDirectory, string projectName, string runTime,
//        string environmentName, string serverSideDownloadFolder, string serverSideDeployFolder,
//        string settingsFileName, string serverSideServiceUserName, int ftpSiteLsFileOffset) : base(logger, placePath,
//        codeFileName)
//    {
//        _codeFileName = codeFileName;
//        _portNumber = portNumber;
//        _ftpSiteAddress = ftpSiteAddress;
//        _ftpSiteUserName = ftpSiteUserName;
//        _ftpSitePassword = ftpSitePassword;
//        _ftpSiteDirectory = ftpSiteDirectory;
//        _projectName = projectName;
//        _runTime = runTime;
//        _environmentName = environmentName;
//        _serverSideDownloadFolder = serverSideDownloadFolder;
//        _serverSideDeployFolder = serverSideDeployFolder;
//        _settingsFileName = settingsFileName;
//        _serverSideServiceUserName = serverSideServiceUserName;
//        _ftpSiteLsFileOffset = ftpSiteLsFileOffset;
//    }

//    public override void CreateFileStructure()
//    {
//        var block = new SshCodeBlock(string.Empty, string.Empty, string.Empty,
//            new SshOneLineComment("! /bin/bash"),
//            string.Empty,
//            new SshOneLineComment(_codeFileName),
//            string.Empty,
//            new SshOneLineComment("The following steps are required for this script to work on the server"),
//            new SshOneLineComment("1. Unzip must be installed"),
//            new SshOneLineComment("sudo apt install unzip"),
//            new SshOneLineComment("2. dotnet must be installed"),
//            new SshOneLineComment("sudo snap install dotnet-sdk --classic"),
//            new SshOneLineComment("3. run which dotnet command to understand where dotnet is"),
//            new SshOneLineComment("which dotnet"),
//            new SshOneLineComment($"4. Open {_portNumber} Port for WebAgent"),
//            new SshOneLineComment($"sudo iptables -A INPUT -p tcp --dport {_portNumber} -j ACCEPT"),
//            new SshOneLineComment("5. Open Other service ports, if need, like as WebAgentInstaller"),
//            new SshOneLineComment("sudo iptables -A INPUT -p tcp --dport 5032 -j ACCEPT"),
//            string.Empty,
//            "dotnetRunner=$(which dotnet)",
//            string.Empty,
//            "result=$(hostname)",
//            "myHostname=${result^}",
//            string.Empty,
//            $"ftpSite={_ftpSiteAddress}",
//            $"user={_ftpSiteUserName}",
//            $"pass={_ftpSitePassword}",
//            $"directory={_ftpSiteDirectory}",
//            $"projectName={_projectName}",
//            $"runTime={_runTime}",
//            $"environmentName={_environmentName}",
//            "downloadFilePrefix=\"$myHostname-$environmentName-$projectName-$runTime-\"",
//            "downloadSettingsFilePrefix=\"$myHostname-$environmentName-$projectName-\"",
//            $"downloadFolder={_serverSideDownloadFolder}",
//            $"deployFolder={_serverSideDeployFolder}",
//            $"ServiceName={_projectName}{_environmentName}",
//            $"SettingsFileName={_settingsFileName}",
//            $"userName={_serverSideServiceUserName}",
//            string.Empty,
//            "projectInstallFullPath=$deployFolder/$projectName",
//            "mainDllFileName=$projectInstallFullPath/$projectName.dll",
//            string.Empty,
//            $"LS_FILE_OFFSET={_ftpSiteLsFileOffset} # Check directory_listing to see where filename begins",
//            string.Empty,
//            "echo downloadFilePrefix is $downloadFilePrefix",
//            string.Empty,
//            string.Empty,
//            string.Empty,
//            string.Empty,
//            string.Empty,
//            string.Empty,
//            string.Empty,
//            string.Empty,
//            string.Empty,
//            string.Empty,
//            string.Empty);
//        CodeFile.AddRange(block.CodeItems);
//        FinishAndSave();
//    }
//}


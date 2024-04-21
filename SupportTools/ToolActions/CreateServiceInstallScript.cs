using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportTools.ToolCommands;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace SupportTools.ToolActions;

public class CreateServiceInstallScript : ToolAction
{
    private readonly string _environmentName;
    private readonly string _ftpSiteAddress;
    private readonly string _ftpSiteDirectory;
    private readonly int _ftpSiteLsFileOffset;
    private readonly string _ftpSitePassword;
    private readonly string _ftpSiteUserName;
    private readonly int _portNumber;
    private readonly string? _projectDescription;
    private readonly string _projectName;
    private readonly string _runTime;
    private readonly ILogger _logger;
    private readonly string _scriptFileName;
    private readonly string _serverSideDeployFolder;
    private readonly string _serverSideDownloadFolder;
    private readonly string _serverSideServiceUserName;
    private readonly string? _serviceDescriptionSignature;
    private readonly string _settingsFileName;

    public CreateServiceInstallScript(ILogger logger, string scriptFileName, int portNumber, string ftpSiteAddress,
        string ftpSiteUserName, string ftpSitePassword, string ftpSiteDirectory, string projectName,
        string? serviceDescriptionSignature, string? projectDescription, string runTime, string environmentName,
        string serverSideDownloadFolder, string serverSideDeployFolder, string settingsFileName,
        string serverSideServiceUserName, int ftpSiteLsFileOffset) : base(logger, nameof(ServiceInstallScriptCreator),
        null, null)
    {
        _logger = logger;
        _scriptFileName = scriptFileName;
        _portNumber = portNumber;
        _ftpSiteAddress = ftpSiteAddress;
        _ftpSiteUserName = ftpSiteUserName;
        _ftpSitePassword = ftpSitePassword;
        _ftpSiteDirectory = ftpSiteDirectory;
        _projectName = projectName;
        _serviceDescriptionSignature = serviceDescriptionSignature;
        _projectDescription = projectDescription;
        _runTime = runTime;
        _environmentName = environmentName;
        _serverSideDownloadFolder = serverSideDownloadFolder;
        _serverSideDeployFolder = serverSideDeployFolder;
        _settingsFileName = settingsFileName;
        _serverSideServiceUserName = serverSideServiceUserName;
        _ftpSiteLsFileOffset = ftpSiteLsFileOffset;
    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var sf = new FileInfo(_scriptFileName);

        var code =
            $$"""
              #!/bin/bash

              # {{sf.Name}}

              #The following steps are required for this script to work on the server
              #1. Unzip must be installed
              #sudo apt install unzip
              #2. dotnet must be installed
              #sudo snap install dotnet-sdk --classic
              #3. run which dotnet command to understand where dotnet is
              #which dotnet
              #4. Open {{_portNumber}} Port for WebAgent
              #sudo iptables -A INPUT -p tcp --dport {{_portNumber}} -j ACCEPT
              #5. Open 5032 Port for WebAgentInstaller
              #sudo iptables -A INPUT -p tcp --dport 5032 -j ACCEPT

              dotnetRunner=$(which dotnet 2>&1)

              result=$(hostname)
              echo result is $result

              myHostname=${result%%.*}

              ftpSite={{_ftpSiteAddress}}
              user={{_ftpSiteUserName}}
              pass={{_ftpSitePassword}}
              directory={{_ftpSiteDirectory}}
              projectName={{_projectName}}
              ServiceDescriptionSignature={{_serviceDescriptionSignature}}
              ProjectDescription="{{_projectDescription}}"
              runTime={{_runTime}}
              environmentName={{_environmentName}}
              downloadFilePrefix="$myHostname-$environmentName-$projectName-$runTime-"
              downloadSettingsFilePrefix="$myHostname-$environmentName-$projectName-"
              downloadFolder={{_serverSideDownloadFolder}}
              deployFolder={{_serverSideDeployFolder}}
              SettingsFileName={{_settingsFileName}}
              userName={{_serverSideServiceUserName}}

              projectInstallFullPath=$deployFolder/$projectName/$environmentName
              mainDllFileName=$projectInstallFullPath/$projectName.dll
              serviceEnvName=$projectName$environmentName

              LS_FILE_OFFSET={{_ftpSiteLsFileOffset}} # Check directory_listing to see where filename begins

              echo download File Prefix is $downloadFilePrefix
              echo download Settings File Prefix is $downloadSettingsFilePrefix

              if [ -z "$dotnetRunner" ]; then
                echo "dotnet runner is not installed!"
                exit 1
              fi

              if [ ! -e $dotnetRunner ]
              then
                echo "dotnet runner $dotnetRunner does not exists"
                dotnetRunner=$(which dotnet 2>&1)
                echo "dotnet runner will use $dotnetRunner"
              fi



              echo The Argument ftpSite is $ftpSite
              if [ -z "$ftpSite" ]; then
                echo "ftpSite not specified, process finished!"
                exit 1
              fi
              echo The Argument user is $user
              if [ -z "$user" ]; then
                echo "user not specified, process finished!"
                exit 1
              fi
              echo The Argument pass is $pass
              if [ -z "$pass" ]; then
                echo "pass not specified, process finished!"
                exit 1
              fi
              echo The Argument directory is $directory
              if [ -z "$directory" ]; then
                echo "directory not specified, process finished!"
                exit 1
              fi
              echo The Argument projectName is $projectName
              if [ -z "$projectName" ]; then
                echo "projectName not specified, process finished!"
                exit 1
              fi
              echo The Argument runTime is $runTime
              if [ -z "$runTime" ]; then
                echo "runTime not specified, process finished!"
                exit 1
              fi
              echo The Argument downloadFilePrefix is $downloadFilePrefix
              if [ -z "$downloadFilePrefix" ]; then
                echo "downloadFilePrefix not specified, process finished!"
                exit 1
              fi
              echo The Argument downloadFolder is $downloadFolder
              if [ -z "$downloadFolder" ]; then
                echo "downloadFolder not specified, process finished!"
                exit 1
              fi
              echo The Argument deployFolder is $deployFolder
              if [ -z "$deployFolder" ]; then
                echo "deployFolder not specified, process finished!"
                exit 1
              fi
              echo The Argument SettingsFileName is $SettingsFileName
              if [ -z "$SettingsFileName" ]; then
                echo "SettingsFileName not specified, process finished!"
                exit 1
              fi
              echo The Argument userName is $userName
              if [ -z "$userName" ]; then
                echo "userName not specified, process finished!"
                exit 1
              fi

              echo make $downloadFolder
              mkdir -p $downloadFolder

              echo make $projectInstallFullPath
              mkdir -p $projectInstallFullPath
              #rm $downloadFolder

              echo remove old directory_listing if it exists
              rm -f directory_listing

              echo Try to get ftp files list to directory_listing
              # get listing from directory sorted by modification date
              ftp -n $ftpSite > directory_listing <<fin
              quote USER $user
              quote PASS $pass
              passive
              cd $directory
              ls -lt
              quit
              fin

              echo parse the file names from the directory listing
              files_to_get=`cut -c $LS_FILE_OFFSET- < directory_listing`

              echo remove directory_listing
              #rm -f directory_listing

              #echo files_to_get is $files_to_get

              if [ -z "$files_to_get" ]; then
                echo "file not found. process finished!"
                exit 1
              fi

              zipfilename=""

              # make a set of get commands from the filename(s)
              for f in $files_to_get; do
                #echo f is $f
                if [[ $f == "$downloadFilePrefix"*.zip ]]; then
                  if [[ $f > $zipfilename ]]; then
                    zipfilename=$f
                  fi
                fi
              done

              echo "zipfilename=$zipfilename"

              if [ -z "$zipfilename" ]; then
                echo "zip file with mask not found. process finished!"
                exit 1
              fi

              downloadzipfilename=$downloadFolder/$zipfilename


              cmd="get $zipfilename $downloadzipfilename
              "

              echo cmd is $cmd




              rm $downloadzipfilename


              # go back and get the file(s)
              ftp -n $ftpSite <<fin
              quote USER $user
              quote PASS $pass
              passive
              cd $directory
              binary
              $cmd
              quit
              fin

              expandedFolderName=${downloadzipfilename%.*}
              echo expandedFolderName is $expandedFolderName

              rm -rf $expandedFolderName

              unzip -q $downloadzipfilename -d $expandedFolderName


              #Get latest settings json file

              latestSettingsJsonFileName=""

              for f in $files_to_get; do
                #echo f is $f
                if [[ $f == "$downloadSettingsFilePrefix"*.json ]]; then
                  if [[ $f > $latestSettingsJsonFileName ]]; then
                    latestSettingsJsonFileName=$f
                  fi
                fi
              done

              echo "latestSettingsJsonFileName=$latestSettingsJsonFileName"

              if [ -z "$latestSettingsJsonFileName" ]; then
                echo "Settings json file with mask not found. process finished!"
                exit 2
              fi

              downloadSettingsJsonFileName=$expandedFolderName/$SettingsFileName

              cmd="get $latestSettingsJsonFileName $downloadSettingsJsonFileName
              "

              echo cmd is $cmd


              #rm $downloadSettingsJsonFileName


              # go back and get the file(s)
              ftp -n $ftpSite <<fin
              quote USER $user
              quote PASS $pass
              passive
              cd $directory
              binary
              $cmd
              quit
              fin

              if (( $(ps -ef | grep -v grep | grep $serviceEnvName | wc -l) > 0 ))
              then
                echo Stop Service...
                sudo systemctl stop $serviceEnvName.service
              fi

              serviceConfigFileName=/etc/systemd/system/$serviceEnvName.service
              echo serviceConfigFileName is $serviceConfigFileName

              rm $serviceConfigFileName

              if [ ! -e $serviceConfigFileName ]; then
                echo Service config file $serviceConfigFileName does not exists
              
                cat >$serviceConfigFileName <<fin
              [Unit]
              Description=$projectName service $serviceDescriptionSignature $ProjectDescription

              [Service]
              WorkingDirectory=$projectInstallFullPath
              ExecStart=$dotnetRunner $mainDllFileName
              Restart=always
              # Restart service after 10 seconds if the dotnet service crashes:
              RestartSec=10
              KillSignal=SIGINT
              SyslogIdentifier=$projectName
              User=$userName
              Environment=ASPNETCORE_ENVIRONMENT=Production
              Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

              [Install]
              WantedBy=multi-user.target
              fin

              fi

              echo remove old files
              rm -rf $projectInstallFullPath/*

              echo move files
              sudo mv -v $expandedFolderName/* $projectInstallFullPath/ > /dev/null 2>&1

              sudo systemctl enable $serviceEnvName.service

              sudo systemctl start $serviceEnvName.service
              sudo systemctl status $serviceEnvName.service

              echo Remove Archive...
              rm $downloadzipfilename

              echo remove extracted folder...
              rm -rf $expandedFolderName

              echo for logs
              echo sudo journalctl -fu $serviceEnvName.service

              exit 0


              """;
        if (FileStat.CreatePrevFolderIfNotExists(_scriptFileName, true, _logger))
        {
            File.WriteAllText(_scriptFileName, code.Replace("\r\n", "\n"));
            return Task.FromResult(true);
        }

        StShared.WriteErrorLine("File did not created", true);
        return Task.FromResult(false);
    }
}
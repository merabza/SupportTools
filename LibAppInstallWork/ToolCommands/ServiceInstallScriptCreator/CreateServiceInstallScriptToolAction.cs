using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands.ServiceInstallScriptCreator;

public sealed class CreateServiceInstallScriptToolAction : ToolAction
{
    private readonly string _environmentName;
    private readonly string _ftpSiteAddress;
    private readonly string _ftpSiteDirectory;
    private readonly string _ftpSiteUserName;
    private readonly ILogger _logger;
    private readonly IReadOnlyCollection<int> _ports;
    private readonly string? _projectDescription;
    private readonly string _projectName;
    private readonly string _runTime;
    private readonly string _scriptFileName;
    private readonly string _serverSideDeployFolder;
    private readonly string _serverSideDownloadFolder;
    private readonly string _serverSideServiceUserName;
    private readonly string? _serviceDescriptionSignature;
    private readonly string _settingsFileName;

    public CreateServiceInstallScriptToolAction(ILogger logger, string scriptFileName, IReadOnlyCollection<int> ports,
        string ftpSiteAddress, string ftpSiteUserName, string ftpSiteDirectory, string projectName,
        string? serviceDescriptionSignature, string? projectDescription, string runTime, string environmentName,
        string serverSideDownloadFolder, string serverSideDeployFolder, string settingsFileName,
        string serverSideServiceUserName) : base(logger,
        nameof(ServiceInstallScriptCreatorToolCommand), null, null)
    {
        _logger = logger;
        _scriptFileName = scriptFileName;
        _ports = ports;
        _ftpSiteAddress = ftpSiteAddress;
        _ftpSiteUserName = ftpSiteUserName;
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
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var sf = new FileInfo(_scriptFileName);

        //პროექტის თითოეული პორტის გახსნის ბრძანება
        string openPortsCode = string.Join("\n", _ports.Select(port => $"open_port {port}"));
        string portsList = _ports.Count == 0 ? "none, Open Port is off" : string.Join(", ", _ports);

        string code = $$"""
                        #!/bin/bash

                        # {{sf.Name}}

                        #This script installs with apt-get the programs it needs, when they are missing:
                        #curl, unzip, the .NET runtime of the project (from its runtimeconfig.json)
                        #and nftables (when firewalld does not run)
                        #It opens the port of the project (from its appsettings Kestrel endpoints: {{portsList}}):
                        #in firewalld if it runs, else in the tcp ports set of Webmin's nftables table, in nftables otherwise
                        #The following step is required for this script to work on the server
                        #Open 5032 Port for WebAgentInstaller
                        #sudo iptables -A INPUT -p tcp --dport 5032 -j ACCEPT

                        aptUpdated=0

                        # install an apt package; apt-get update runs once, before the first install
                        install_package() {
                          if ! command -v apt-get > /dev/null 2>&1; then
                            echo "apt-get not found, install $1 manually. process finished!"
                            exit 1
                          fi
                          if [ $aptUpdated -eq 0 ]; then
                            echo apt-get update...
                            if ! sudo apt-get update; then
                              echo "apt-get update failed. process finished!"
                              exit 1
                            fi
                            aptUpdated=1
                          fi
                          echo install $1...
                          if ! sudo DEBIAN_FRONTEND=noninteractive apt-get install -y "$1"; then
                            echo "$1 install failed. process finished!"
                            exit 1
                          fi
                        }

                        # install the package when the command is missing
                        require_command() {
                          if ! command -v "$1" > /dev/null 2>&1; then
                            echo "$1 is not installed"
                            install_package "$2"
                            if ! command -v "$1" > /dev/null 2>&1; then
                              echo "$1 is not found after the install of $2. process finished!"
                              exit 1
                            fi
                          fi
                        }

                        # open a tcp port: in firewalld if it runs, in nftables otherwise
                        # Webmin's (Virtualmin's) nftables table drops every port that is not in its tcp ports set,
                        # so there the port goes into that set; a rule in another table would not help
                        webminTable=webmin_profile_hosting
                        webminTcpPorts=profile_hosting_tcp_ports

                        # is the port among the elements of Webmin's tcp ports set in /etc/nftables.conf
                        port_in_nftables_conf() {
                          sudo perl -0777 -ne 'BEGIN { ($port, $set) = splice(@ARGV, 0, 2) } exit(/set \Q$set\E \{[^}]*?elements = \{[^}]*?(?<![0-9-])\Q$port\E(?![0-9-])/ ? 0 : 1)' "$1" "$webminTcpPorts" /etc/nftables.conf
                        }

                        open_port() {
                          echo open port $1...
                          if command -v firewall-cmd > /dev/null 2>&1 && sudo firewall-cmd --state > /dev/null 2>&1; then
                            if ! sudo firewall-cmd --permanent --add-port=$1/tcp || ! sudo firewall-cmd --reload; then
                              echo "port $1 open in firewalld failed. process finished!"
                              exit 1
                            fi
                          elif command -v nft > /dev/null 2>&1 && sudo nft list set inet $webminTable $webminTcpPorts > /dev/null 2>&1; then
                            if ! sudo nft get element inet $webminTable $webminTcpPorts "{ $1 }" > /dev/null 2>&1; then
                              if ! sudo nft add element inet $webminTable $webminTcpPorts "{ $1 }"; then
                                echo "port $1 add to the nftables set $webminTcpPorts failed. process finished!"
                                exit 1
                              fi
                              # keep it after a restart: add it to the elements of the set in /etc/nftables.conf
                              # (only here: a port that the set already holds may be inside a range of the file)
                              if ! port_in_nftables_conf $1; then
                                sudo perl -0777 -pi -e 'BEGIN { ($port, $set) = splice(@ARGV, 0, 2) } s/(set \Q$set\E \{[^}]*?elements = \{[^}]*?)\s*\}/$1, $port }/' "$1" "$webminTcpPorts" /etc/nftables.conf
                                if ! port_in_nftables_conf $1; then
                                  echo "port $1 is open now, but it could not be added to $webminTcpPorts in /etc/nftables.conf: add it there by hand"
                                fi
                              fi
                            else
                              echo "port $1 is already open in the nftables set $webminTcpPorts"
                            fi
                          else
                            if ! dpkg -s nftables > /dev/null 2>&1; then
                              install_package nftables
                            fi
                            if ! sudo grep -q "tcp dport $1 accept" /etc/nftables.conf 2>/dev/null; then
                              echo "add rule inet filter input tcp dport $1 accept" | sudo tee -a /etc/nftables.conf > /dev/null
                            fi
                            sudo systemctl enable nftables
                            if ! sudo systemctl restart nftables; then
                              echo "nftables restart failed, check /etc/nftables.conf. process finished!"
                              exit 1
                            fi
                          fi
                        }

                        require_command curl curl
                        require_command unzip unzip

                        result=$(hostname)
                        echo result is $result

                        myHostname=${result%%.*}

                        ftpSite={{_ftpSiteAddress}}
                        user={{_ftpSiteUserName}}
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

                        echo download File Prefix is $downloadFilePrefix
                        echo download Settings File Prefix is $downloadSettingsFilePrefix

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
                        # ask the ftp password, show * for every typed character
                        pass=""
                        prompt="Enter ftp password for $user: "
                        while IFS= read -r -s -n 1 -p "$prompt" char; do
                          if [ -z "$char" ]; then
                            break
                          fi
                          if [[ $char == $'\177' || $char == $'\b' ]]; then
                            if [ -n "$pass" ]; then
                              pass="${pass%?}"
                              prompt=$'\b \b'
                            else
                              prompt=""
                            fi
                          else
                            pass+="$char"
                            prompt="*"
                          fi
                        done
                        echo

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

                        echo Try to get ftp files list
                        # get the file names only; the newest file is chosen by the date in its name
                        if ! directory_listing=$(curl -sS --fail --list-only -u "$user:$pass" "ftp://$ftpSite$directory"); then
                          echo "ftp files list download failed. process finished!"
                          exit 1
                        fi

                        # some ftp servers end the lines with CRLF
                        files_to_get=$(printf '%s\n' "$directory_listing" | tr -d '\r')

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

                        echo download $zipfilename to $downloadzipfilename

                        rm -f $downloadzipfilename

                        # go back and get the file
                        if ! curl -sS --fail -u "$user:$pass" "ftp://$ftpSite$directory$zipfilename" -o "$downloadzipfilename"; then
                          echo "zip file $zipfilename download failed. process finished!"
                          exit 1
                        fi

                        if [ ! -s "$downloadzipfilename" ]; then
                          echo "downloaded zip file $downloadzipfilename is missing or empty. process finished!"
                          exit 1
                        fi

                        expandedFolderName=${downloadzipfilename%.*}
                        echo expandedFolderName is $expandedFolderName

                        rm -rf $expandedFolderName

                        if ! unzip -q $downloadzipfilename -d $expandedFolderName; then
                          echo "unzip of $downloadzipfilename failed. process finished!"
                          exit 1
                        fi

                        if [ ! -e "$expandedFolderName/$projectName.dll" ]; then
                          echo "$projectName.dll not found in $expandedFolderName. process finished!"
                          exit 1
                        fi

                        # the .NET runtime that the project needs, from its runtimeconfig.json
                        # a self-contained project (includedFrameworks) brings its own runtime
                        runtimeConfigFileName=$expandedFolderName/$projectName.runtimeconfig.json
                        if [ -e "$runtimeConfigFileName" ] && ! grep -q '"includedFrameworks"' "$runtimeConfigFileName"; then
                          if grep -q '"Microsoft.AspNetCore.App"' "$runtimeConfigFileName"; then
                            frameworkName=Microsoft.AspNetCore.App
                            runtimePackagePrefix=aspnetcore-runtime
                          else
                            frameworkName=Microsoft.NETCore.App
                            runtimePackagePrefix=dotnet-runtime
                          fi
                          frameworkVersion=$(grep -A1 "\"$frameworkName\"" "$runtimeConfigFileName" | grep -o '"version": *"[0-9]*\.[0-9]*' | grep -o '[0-9]*\.[0-9]*$')
                          if [ -n "$frameworkVersion" ]; then
                            echo "the project needs $frameworkName $frameworkVersion"
                            if ! dotnet --list-runtimes 2>/dev/null | grep -q "^$frameworkName $frameworkVersion\."; then
                              echo "$frameworkName $frameworkVersion is not installed"
                              install_package "$runtimePackagePrefix-$frameworkVersion"
                              if ! dotnet --list-runtimes 2>/dev/null | grep -q "^$frameworkName $frameworkVersion\."; then
                                echo "$frameworkName $frameworkVersion is not found after the install. process finished!"
                                exit 1
                              fi
                            fi
                          fi
                        fi

                        dotnetRunner=$(command -v dotnet)
                        if [ -z "$dotnetRunner" ]; then
                          echo "dotnet runner is not installed! process finished!"
                          exit 1
                        fi
                        echo "dotnet runner is $dotnetRunner"


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

                        echo download $latestSettingsJsonFileName to $downloadSettingsJsonFileName

                        #rm $downloadSettingsJsonFileName

                        # go back and get the file
                        if ! curl -sS --fail -u "$user:$pass" "ftp://$ftpSite$directory$latestSettingsJsonFileName" -o "$downloadSettingsJsonFileName"; then
                          echo "Settings json file $latestSettingsJsonFileName download failed. process finished!"
                          exit 2
                        fi

                        if [ ! -s "$downloadSettingsJsonFileName" ]; then
                          echo "downloaded settings file $downloadSettingsJsonFileName is missing or empty. process finished!"
                          exit 2
                        fi

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
                        SyslogIdentifier=$serviceEnvName
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

                        {{openPortsCode}}

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
            await File.WriteAllTextAsync(_scriptFileName, code.Replace("\r\n", "\n"), cancellationToken);
            return true;
        }

        StShared.WriteErrorLine("File did not created", true);
        return false;
    }
}

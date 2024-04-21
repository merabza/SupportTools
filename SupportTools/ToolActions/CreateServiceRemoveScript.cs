using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportTools.ToolCommands;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace SupportTools.ToolActions;

public class CreateServiceRemoveScript : ToolAction
{
    private readonly string _environmentName;
    private readonly string _projectName;
    private readonly ILogger _logger;
    private readonly string _scriptFileName;
    private readonly string _serverSideDeployFolder;

    public CreateServiceRemoveScript(ILogger logger, string scriptFileName, string projectName, string environmentName,
        string serverSideDeployFolder) : base(logger, nameof(ServiceRemoveScriptCreator), null,
        null)
    {
        _logger = logger;
        _scriptFileName = scriptFileName;
        _projectName = projectName;
        _environmentName = environmentName;
        _serverSideDeployFolder = serverSideDeployFolder;
    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var sf = new FileInfo(_scriptFileName);

        var code =
            $"""
              #!/bin/bash

              # {sf.Name}

              deployFolder={_serverSideDeployFolder}
              projectName={_projectName}
              ServiceName={_projectName}{_environmentName}
              environmentName={_environmentName}

              projectInstallFullPath=$deployFolder/$projectName/$environmentName
              ServiceConfigurationFileName=/etc/systemd/system/$ServiceName.service

              if [ ! -e $serviceConfigFileName ]
              then
                echo "Servise configfile $serviceConfigFileName does not exists"
                exit 1
              fi

              #systemctl is-active $ServiceName.service
              if (systemctl -q is-active $ServiceName.service)
              then
                echo "Application is running. try to stop"
                if ( ! systemctl stop $ServiceName.service )
                then
                  echo "Application can not stopped"
                  exit 1
                fi
              else
                echo "Application is not running."
              fi


              echo "try to disable"
              if ( ! systemctl disable $ServiceName.service )
              then
                echo "Application can not disable"
                exit 1
              fi

              echo "Deleting files..."
              rm -rf $projectInstallFullPath

              echo "Success"

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
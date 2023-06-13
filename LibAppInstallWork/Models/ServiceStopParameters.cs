////Created by ProjectParametersClassCreator at 5/10/2021 16:04:08

//using System.Collections.Generic;
//using SystemToolsShared;
//using CliParameters;
//using CliParametersApiClientsEdit.Models;
//using CliToolsData.Models;
//using SupportToolsData;
//using SupportToolsData.Models;

//namespace LibAppInstallWork.Models;

//public sealed class ServiceStopParameters : IParameters
//{
//    public string ServiceName { get; }
//    public string WebAgentNameForInstall { get; }
//    public Dictionary<string, ApiClientSettings> WebAgents { get; }
//    public InstallerSettings InstallerSettings { get; }

//    public ServiceStopParameters(string serviceName, string webAgentNameForInstall,
//        Dictionary<string, ApiClientSettings> webAgents, InstallerSettings installerSettings)
//    {
//        ServiceName = serviceName;
//        WebAgentNameForInstall = webAgentNameForInstall;
//        WebAgents = webAgents;
//        InstallerSettings = installerSettings;
//    }

//    public static ServiceStopParameters? Create(SupportToolsParameters supportToolsParameters, string projectName,
//        string serverName)
//    {

//        SettingsGetter webAgentsGetter = new SettingsGetter(supportToolsParameters, projectName, serverName);
//        if (!webAgentsGetter.Run(true, false, false, false, false))
//            return null;

//        ProjectModel project = webAgentsGetter.Project;

//        if (!project.IsService)
//        {
//            StShared.WriteErrorLine($"Project {projectName} is not service", true);
//            return null;
//        }

//        ServiceStopParameters serviceStopParameters = new ServiceStopParameters(serviceName: project.ServiceName,
//            webAgentNameForInstall: webAgentsGetter.WebAgentNameForInstall, webAgents: webAgentsGetter.WebAgents,
//            installerSettings: webAgentsGetter.LocalInstallerSettings);

//        return serviceStopParameters;
//    }

//    public bool CheckBeforeSave()
//    {
//        return true;
//    }

//}


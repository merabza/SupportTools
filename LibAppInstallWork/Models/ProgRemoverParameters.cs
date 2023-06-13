////Created by ProjectParametersClassCreator at 12/20/2020 00:00:56

//using System.Collections.Generic;
//using CliParameters;
//using CliParametersApiClientsEdit.Models;
//using CliToolsData.Models;
//using SupportToolsData;
//using SupportToolsData.Models;

//namespace LibAppInstallWork.Models;

//public sealed class ProgRemoverParameters : IParameters
//{
//    public string ProjectName { get; }
//    public string ServiceName { get; }
//    public string WebAgentNameForInstall { get; }
//    public Dictionary<string, ApiClientSettings> WebAgents { get; }
//    public InstallerSettings InstallerSettings { get; }

//    public ProgRemoverParameters(string projectName, string serviceName, string webAgentNameForInstall,
//        Dictionary<string, ApiClientSettings> webAgents, InstallerSettings installerSettings)
//    {
//        ProjectName = projectName;
//        ServiceName = serviceName;
//        WebAgentNameForInstall = webAgentNameForInstall;
//        WebAgents = webAgents;
//        InstallerSettings = installerSettings;
//    }
//    public static ProgRemoverParameters? Create(SupportToolsParameters supportToolsParameters, string projectName,
//        string serverName)
//    {

//        SettingsGetter webAgentsGetter =
//            new SettingsGetter(supportToolsParameters, projectName, serverName);
//        if (!webAgentsGetter.Run(true, false, false, false, false))
//            return null;

//        ProjectModel project = webAgentsGetter.Project;

//        ProgRemoverParameters progRemoverParameters = new ProgRemoverParameters(projectName,
//            project.ServiceName, webAgentsGetter.WebAgentNameForInstall,
//            webAgentsGetter.WebAgents, webAgentsGetter.LocalInstallerSettings);

//        return progRemoverParameters;
//    }

//    public bool CheckBeforeSave()
//    {
//        return true;
//    }

//}


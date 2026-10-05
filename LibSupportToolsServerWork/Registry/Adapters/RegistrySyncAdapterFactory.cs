using System.Collections.Generic;
using LibSupportToolsServerWork.Registry.Paths;
using LibSupportToolsServerWork.Registry.Sync;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;

namespace LibSupportToolsServerWork.Registry.Adapters;

//რეესტრის ყველა ადაპტერის აწყობა ერთ ადგილას (C3). C4 აქ Projects-ს დაამატებს, სინქრონიზაციის ბრძანება (C5) კი სიას
//RegistrySyncEngine-ს გადასცემს. pathMapper (parameters.PathMappings-ით შექმნილი) და warnings ბრძანებას ეკუთვნის:
//გეგმის აგებისა და შესრულების შემდეგ ის მათ Issues-სა და Items-ს აჩვენებს
public static class RegistrySyncAdapterFactory
{
    public static List<IRegistrySyncAdapter> CreateAdapters(SupportToolsServerApiClient apiClient,
        SupportToolsParameters parameters, PathMapper pathMapper, RegistrySyncWarnings warnings)
    {
        return
        [
            new EnvironmentsRegistrySyncAdapter(apiClient, parameters, warnings),
            new RunTimesRegistrySyncAdapter(apiClient, parameters, warnings),
            new NpmPackagesRegistrySyncAdapter(apiClient, parameters, warnings),
            new ReactAppTemplatesRegistrySyncAdapter(apiClient, parameters, warnings),
            new DotnetToolsRegistrySyncAdapter(apiClient, parameters, warnings),
            new SmartSchemasRegistrySyncAdapter(apiClient, parameters, warnings),
            new FileStoragesRegistrySyncAdapter(apiClient, parameters, pathMapper, warnings),
            new ApiClientsRegistrySyncAdapter(apiClient, parameters, warnings),
            new DatabaseServerConnectionsRegistrySyncAdapter(apiClient, parameters, warnings),
            new ServersRegistrySyncAdapter(apiClient, parameters, warnings),
            new GitIgnorePatternsRegistrySyncAdapter(apiClient, parameters, warnings),
            new GitsRegistrySyncAdapter(apiClient, parameters, pathMapper, warnings),
            new EditorConfigPatternsRegistrySyncAdapter(apiClient, parameters, warnings),
            new ProjectTemplatesRegistrySyncAdapter(apiClient, parameters, warnings),
            new GlobalSettingsRegistrySyncAdapter(apiClient, parameters, warnings),
            new ProjectCreatorSettingsRegistrySyncAdapter(apiClient, parameters, pathMapper, warnings)
        ];
    }
}

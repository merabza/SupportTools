//Created by ProjectParametersClassCreator at 12/22/2020 19:46:17

using System;
using System.Collections.Generic;
using LibFileParameters.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class ProgramPublisherParameters : IParameters
{
    private ProgramPublisherParameters(string runtime, string projectName, string mainProjectFileName,
        ServerInfoModel serverInfo, string workFolder, string uploadTempExtension, string dateMask,
        FileStorageData fileStorageForExchange, SmartSchema smartSchemaForExchange, SmartSchema smartSchemaForLocal,
        List<string> redundantFileNames)
    {
        Runtime = runtime;
        ProjectName = projectName;
        MainProjectFileName = mainProjectFileName;
        ServerInfo = serverInfo;
        WorkFolder = workFolder;
        UploadTempExtension = uploadTempExtension;
        DateMask = dateMask;
        FileStorageForExchange = fileStorageForExchange;
        SmartSchemaForExchange = smartSchemaForExchange;
        SmartSchemaForLocal = smartSchemaForLocal;
        RedundantFileNames = redundantFileNames;
    }

    public string Runtime { get; }
    public string ProjectName { get; }
    public string MainProjectFileName { get; }
    public ServerInfoModel ServerInfo { get; }
    public string WorkFolder { get; }
    public string UploadTempExtension { get; }
    public string DateMask { get; }
    public FileStorageData FileStorageForExchange { get; }
    public SmartSchema SmartSchemaForExchange { get; }

    public SmartSchema SmartSchemaForLocal { get; }

    public List<string> RedundantFileNames { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static ProgramPublisherParameters? Create(ILogger logger, SupportToolsParameters supportToolsParameters,
        string projectName, ServerInfoModel serverInfo)
    {
        try
        {
            var project = supportToolsParameters.GetProjectRequired(projectName);

            var gitProjects = GitProjects.Create(logger, supportToolsParameters.GitProjects);

            if (string.IsNullOrWhiteSpace(serverInfo.ServerName))
            {
                StShared.WriteErrorLine("Server name is not specified", true);
                return null;
            }

            var server = supportToolsParameters.GetServerDataRequired(serverInfo.ServerName);

            var mainProjectFileName = project.MainProjectFileName(gitProjects);
            if (mainProjectFileName == null)
            {
                StShared.WriteErrorLine($"Main project does not specified for {projectName}", true, null, false);
                return null;
            }


            if (string.IsNullOrWhiteSpace(supportToolsParameters.SmartSchemaNameForExchange))
            {
                StShared.WriteErrorLine("SmartSchemaNameForLocal does not specified", true);
                return null;
            }

            var smartSchemaForExchange =
                supportToolsParameters.GetSmartSchemaRequired(supportToolsParameters.SmartSchemaNameForExchange);

            if (string.IsNullOrWhiteSpace(supportToolsParameters.SmartSchemaNameForLocal))
            {
                StShared.WriteErrorLine("SmartSchemaNameForLocal does not specified", true);
                return null;
            }

            var smartSchemaForLocal =
                supportToolsParameters.GetSmartSchemaRequired(supportToolsParameters.SmartSchemaNameForLocal);

            if (string.IsNullOrWhiteSpace(supportToolsParameters.FileStorageNameForExchange))
            {
                StShared.WriteErrorLine("FileStorageNameForExchange does not specified", true);
                return null;
            }

            var fileStorageForUpload =
                supportToolsParameters.GetFileStorageRequired(supportToolsParameters.FileStorageNameForExchange);

            if (string.IsNullOrWhiteSpace(server.Runtime))
            {
                StShared.WriteErrorLine($"server.Runtime does not specified for server {serverInfo.ServerName}", true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(supportToolsParameters.PublisherWorkFolder))
            {
                StShared.WriteErrorLine("supportToolsParameters.PublisherWorkFolder does not specified", true);
                return null;
            }

            var parametersFileDateMask =
                project.ParametersFileDateMask ?? supportToolsParameters.ParametersFileDateMask;
            if (string.IsNullOrWhiteSpace(parametersFileDateMask))
            {
                StShared.WriteErrorLine("parametersFileDateMask does not specified", true);
                return null;
            }

            var programPublisherParameters = new ProgramPublisherParameters(server.Runtime, projectName,
                mainProjectFileName, serverInfo, supportToolsParameters.PublisherWorkFolder,
                supportToolsParameters.GetUploadTempExtension(), parametersFileDateMask, fileStorageForUpload,
                smartSchemaForExchange, smartSchemaForLocal, project.RedundantFileNames);

            return programPublisherParameters;
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}
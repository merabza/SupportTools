//using LibParameters;
//using SupportToolsData.Models;
//using SystemToolsShared;

//namespace LibGitWork.ToolCommandParameters;

//public class UpdateGitProjectsParameters : IParameters
//{
//    public string WorkFolder { get; }

//    // ReSharper disable once ConvertToPrimaryConstructor
//    public UpdateGitProjectsParameters(string workFolder)
//    {
//        WorkFolder = workFolder;
//    }

//    public static UpdateGitProjectsParameters? Create(IParametersManager parametersManager)
//    {
//        var parametersWithGits = (SupportToolsParameters)parametersManager.Parameters;

//        var workFolder = parametersWithGits.WorkFolder;
//        if (string.IsNullOrWhiteSpace(workFolder))
//        {
//            StShared.WriteErrorLine("supportToolsParameters.WorkFolder does not specified", true);
//            return null;
//        }
        
//        return new UpdateGitProjectsParameters(workFolder);
//    }

//    public bool CheckBeforeSave()
//    {
//        return true;
//    }
//}
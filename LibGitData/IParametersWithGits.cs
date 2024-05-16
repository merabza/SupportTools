//using System.Collections.Generic;
//using LibGitData.Models;

//namespace LibGitData;

//public interface IParametersWithGits : IParameters
//{
//    string? WorkFolder { get; }
//    Dictionary<string, string> GitIgnoreModelFilePaths { get; }
//    Dictionary<string, GitDataModel> Gits { get; }
//    Dictionary<string, GitProjectDataModel> GitProjects { get; }

//    bool DeleteGitFromProjectByNames(string projectName, string gitName, EGitCol gitCol);
//    Option<List<string>> GetGitProjectNames(string projectName, EGitCol gitCol);
//    string? GetGitsFolder(string projectName, EGitCol gitCol);

//}
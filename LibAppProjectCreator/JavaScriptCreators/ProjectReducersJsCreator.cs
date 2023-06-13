using System;
using System.IO;

namespace LibAppProjectCreator.JavaScriptCreators;

public sealed class ProjectReducersJsCreator
{
    private readonly string _fileName;
    private readonly string _folderPathForSave;

    public ProjectReducersJsCreator(string folderPathForSave, string fileName)
    {
        _folderPathForSave = folderPathForSave;
        _fileName = fileName;
    }

    public bool Create()
    {
        const string code = @"import * as IssuesStore from '../issues/IssuesStore';

const projectReducers = {
  issuesStore: IssuesStore.reducer
};
  
export default projectReducers;
";

        File.WriteAllText(Path.Combine(_folderPathForSave, _fileName), @$"//{_fileName}
//Created by {GetType().Name} at {DateTime.Now}
{code}");

        return true;
    }
}
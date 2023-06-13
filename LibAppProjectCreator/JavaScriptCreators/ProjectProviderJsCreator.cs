using System;
using System.IO;

namespace LibAppProjectCreator.JavaScriptCreators;

public sealed class ProjectProviderJsCreator
{
    private readonly string _fileName;
    private readonly string _folderPathForSave;
    private readonly string _projectNamespace;

    public ProjectProviderJsCreator(string folderPathForSave, string projectNamespace, string fileName)
    {
        _folderPathForSave = folderPathForSave;
        _projectNamespace = projectNamespace;
        _fileName = fileName;
    }

    public bool Create()
    {
        var code = @"import React from 'react';

import TopNavRoutes from './TopNavRoutes';
import AuthAppRoutes from './AuthAppRoutes';

const ProjectContext = React.createContext();

const ProjectProvider = (props) => {
const appName = ""ენის მოდელის რედაქტორი"";
      const baseUrl = process.env.NODE_ENV === 'development'
        //? window.location.origin + '/api'
        // ? 'http://192.168.10.119:5011/api'
        ? 'http://cyberia.ge:5011/api'
        : 'https://www." + _projectNamespace + @"/api';

      // console.log(""ProjectProvider baseUrl="", baseUrl);

      return (
        <ProjectContext.Provider value={{ TopNavRoutes, AuthAppRoutes, appName, baseUrl }}>
      {props.children}
      </ProjectContext.Provider>
        );
    }

    export { ProjectContext, ProjectProvider };
";

        File.WriteAllText(Path.Combine(_folderPathForSave, _fileName), @$"//{_fileName}
//Created by {GetType().Name} at {DateTime.Now}
{code}");

        return true;
    }
}
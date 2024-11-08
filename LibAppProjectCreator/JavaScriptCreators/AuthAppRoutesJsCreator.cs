using System;
using System.IO;

namespace LibAppProjectCreator.JavaScriptCreators;

public sealed class AuthAppRoutesJsCreator
{
    private readonly string _fileName;
    private readonly string _folderPathForSave;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AuthAppRoutesJsCreator(string folderPathForSave, string fileName)
    {
        _folderPathForSave = folderPathForSave;
        _fileName = fileName;
    }

    public bool Create()
    {
        const string code = @"import React from 'react';
import { Route } from 'react-router-dom';

import ForConfirmRootsList from './derivationTreeEditor/ForConfirmRootsList';

import RecountsDashboard from './recounters/RecountsDashboard';

import Issues from './issues/Issues';
import IssueWork from './issues/IssueWork';



const AuthAppRoutes = (props) => {
  return (
    <div>

      <Route path='/forConfirmRootsList/:page?' component={ForConfirmRootsList} />

      <Route path='/recountsDashboard' component={RecountsDashboard} />

      <Route path='/issues' component={Issues} />
      <Route path='/issuework/:issueId' component={IssueWork} />
      
    </div>
  );
}

export default AuthAppRoutes;
";

        File.WriteAllText(Path.Combine(_folderPathForSave, _fileName), @$"//{_fileName}
//Created by {GetType().Name} at {DateTime.Now}
{code}");

        return true;
    }
}
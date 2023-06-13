using System;
using System.IO;

namespace LibAppProjectCreator.JavaScriptCreators;

public sealed class TopNavRoutesJsCreator
{
    private readonly string _fileName;
    private readonly string _folderPathForSave;

    public TopNavRoutesJsCreator(string folderPathForSave, string fileName)
    {
        _folderPathForSave = folderPathForSave;
        _fileName = fileName;
    }

    public bool Create()
    {
        const string code = @"import React from 'react';
import { Route } from 'react-router-dom';
import { Nav } from 'react-bootstrap';

import ForIssuesTopForm from './issues/ForIssuesTopForm';

const TopNavRoutes = (props) => {
  //console.log(""TopNavRoutes props="", props);
  //const infId = props.match && props.match.params ? props.match.params.infId : null;

      return (
        <Nav>
          <Route path='/issues' component={ForIssuesTopForm} />
        </Nav>
      );
    }

    export default TopNavRoutes;
";

        File.WriteAllText(Path.Combine(_folderPathForSave, _fileName), @$"//{_fileName}
//Created by {GetType().Name} at {DateTime.Now}
{code}");

        return true;
    }
}
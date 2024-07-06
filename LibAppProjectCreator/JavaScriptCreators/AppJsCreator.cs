using System;
using System.IO;

namespace LibAppProjectCreator.JavaScriptCreators;

public sealed class AppJsCreator
{
    private readonly string _fileName;
    private readonly string _folderPathForSave;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AppJsCreator(string folderPathForSave, string fileName)
    {
        _folderPathForSave = folderPathForSave;
        _fileName = fileName;
    }

    public bool Create()
    {
        const string code = @"import './App.css';

import React from 'react';
import {BrowserRouter, Route, Switch } from 'react-router-dom';
import { QueryParamProvider } from 'use-query-params';
import { connect } from 'react-redux';

import AuthApp from '../carcass/common/AuthApp';
import LoginPage from '../carcass/common/LoginPage';
import PrivateApp from '../carcass/common/PrivateApp';
import RegistrationPage from '../carcass/common/RegistrationPage';

//import { history } from '../carcass/common/History';

import { ProjectProvider } from './ProjectProvider';

import { library } from '@fortawesome/fontawesome-svg-core'
import { faCheckSquare, faCoffee, faSync, faSave, faSignOutAlt, faHome, faAlignLeft, faBarcode, faArrowsAltH, faLongArrowAltDown,
  faSquare, faMicroscope, faFolder, faFolderOpen, faEdit, faTrash, faPlus, faPlusSquare, faMinusSquare, faWindowClose,
  faSignInAlt, faUserPlus, faUser, faUserMinus, faKey, faFileAlt, faMinus, faBezierCurve, faShapes, faArrowDown, faArrowUp, 
  faChevronLeft, faFileExport, faTimes, faCheck, faUsersCog, faStream, faCheckCircle, faRobot, faPaperclip, 
  faAngleLeft, faAngleDoubleLeft, faAngleRight, faAngleDoubleRight, faSort, faSortUp, faSortDown
} from '@fortawesome/free-solid-svg-icons';
library.add(faCheckSquare, faCoffee, faSync, faSave, faSignOutAlt, faHome, faAlignLeft, faBarcode, faArrowsAltH, faLongArrowAltDown,
  faSquare, faMicroscope, faFolder, faFolderOpen, faEdit, faTrash, faPlus, faPlusSquare, faMinusSquare, faWindowClose,
  faSignInAlt, faUserPlus, faUser, faUserMinus, faKey, faFileAlt, faMinus, faBezierCurve, faShapes, faArrowDown, faArrowUp, 
  faChevronLeft, faFileExport, faTimes, faCheck, faUsersCog, faStream, faCheckCircle, faRobot, faPaperclip, 
  faAngleLeft, faAngleDoubleLeft, faAngleRight, faAngleDoubleRight, faSort, faSortUp, faSortDown);

const App = () => {

  // console.log(""App start"");

  return (
    <ProjectProvider>
      <BrowserRouter>
        <QueryParamProvider ReactRouterRoute={Route}>
          <div>
            <PrivateApp><AuthApp /></PrivateApp>
            <Switch>
              <Route path=""/login"" component={LoginPage} />
              <Route path=""/registration"" component={RegistrationPage} />
            </Switch>
          </div>
        </QueryParamProvider>
      </BrowserRouter>
    </ProjectProvider>
  );

};

//ეს საჭიროა ავტორიზაციის გავლის შემდეგ ავტორიზებულ ნაწილზე გადასასვლელად და პირიქით
function mapStateToProps(state) {
  const { userValidationChecked, user } = state.authentication;
  return { userValidationChecked, user };
}

export default connect(
  mapStateToProps
)(App);
";

        File.WriteAllText(Path.Combine(_folderPathForSave, _fileName), @$"//{_fileName}
//Created by {GetType().Name} at {DateTime.Now}
{code}");

        return true;
    }
}
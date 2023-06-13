using System;
using System.IO;

namespace LibAppProjectCreator.JavaScriptCreators;

public sealed class IndexJsCreator
{
    private readonly string _fileName;
    private readonly string _folderPathForSave;

    public IndexJsCreator(string folderPathForSave, string fileName)
    {
        _folderPathForSave = folderPathForSave;
        _fileName = fileName;
    }

    public bool Create()
    {
        const string code =
            @"//import store უნდა ოყოს პირველ ადგილას, წინააღმდეგ შემთხვევაში, გამოიტანს შეცდომას, რომ რედუსერები გამოყენებულია ინიციალიზაციამდე.
import store from './carcass/store/ReduxStore';
import React from 'react';
import ReactDOM from 'react-dom';
import './index.css';
import { Provider } from 'react-redux';
import App from './project/App';
import reportWebVitals from './reportWebVitals';

const rootElement = document.getElementById('root');

ReactDOM.render(
  <Provider store={store}>
    <React.StrictMode>
      <App />
    </React.StrictMode>
  </Provider>, 
  rootElement);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();
";

        File.WriteAllText(Path.Combine(_folderPathForSave, _fileName), @$"//{_fileName}
//Created by {GetType().Name} at {DateTime.Now}
{code}");

        return true;
    }
}
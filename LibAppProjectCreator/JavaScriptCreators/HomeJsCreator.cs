using System;
using System.IO;

namespace LibAppProjectCreator.JavaScriptCreators;

public sealed class HomeJsCreator
{
    private readonly string _fileName;
    private readonly string _folderPathForSave;

    // ReSharper disable once ConvertToPrimaryConstructor
    public HomeJsCreator(string folderPathForSave, string fileName)
    {
        _folderPathForSave = folderPathForSave;
        _fileName = fileName;
    }

    public bool Create()
    {
        const string code = @"import React from 'react';

const Home = () => {

  return (
    <div>
    <h1>გამარჯობა</h1>
    <p>კეთილი იყოს შენი მობრძანება</p>
    <p>ეს არის მთავარი გვერდი</p>
    <p>მომავალში აქ დაემატება ხშირად გამოყენებადი ბმულები</p>
  </div>
  );
}

export default Home;
";

        File.WriteAllText(Path.Combine(_folderPathForSave, _fileName), @$"//{_fileName}
//Created by {GetType().Name} at {DateTime.Now}
{code}");

        return true;
    }
}

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Moq;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Tests;

//temp folder layout and parameters shared by the .editorconfig tests:
//{root}\templates\CSharp.editorconfig and {root}\projects\MyProject\MyProject\.editorconfig (beside MyProject.slnx),
//both with the same content. Console output is captured, so the test class must be in ConsoleCaptureCollection
internal sealed class EditorConfigTestEnvironment : IDisposable
{
    public const string ProjectName = "MyProject";
    public const string PatternName = "CSharp";
    public const string TemplateContent = "root = true\r\n\r\n[*.cs]\r\n# ქართული\r\nindent_size = 4\r\n";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;

    public EditorConfigTestEnvironment()
    {
        RootFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
        TemplatesFolder = Path.Combine(RootFolder, "templates");
        TemplateFileName = Path.Combine(TemplatesFolder, $"{PatternName}.editorconfig");
        Directory.CreateDirectory(TemplatesFolder);
        File.WriteAllText(TemplateFileName, TemplateContent);

        Parameters = new SupportToolsParameters
        {
            FolderForEditorConfigFiles = TemplatesFolder, EditorConfigPatterns = { PatternName }
        };
        EditorConfigFileName = AddProject(ProjectName, PatternName, TemplateContent);

        ParametersManager.SetupGet(x => x.Parameters).Returns(Parameters);
        ParametersManager
            .Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public string RootFolder { get; }
    public string TemplatesFolder { get; }
    public string EditorConfigFileName { get; }
    public string TemplateFileName { get; }
    public SupportToolsParameters Parameters { get; }
    public Mock<IParametersManager> ParametersManager { get; } = new();

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(RootFolder, true);
    }

    //registers a project whose solution is {root}\projects\{name}\{name}\{name}.slnx and returns the name of the
    //.editorconfig file beside that solution. The file is created only when its content is given
    public string AddProject(string projectName, string? editorConfigPatternName, string? editorConfigContent)
    {
        string projectFolder = Path.Combine(RootFolder, "projects", projectName);
        string solutionFolder = Path.Combine(projectFolder, projectName);
        string editorConfigFileName = Path.Combine(solutionFolder, ".editorconfig");
        Directory.CreateDirectory(solutionFolder);
        if (editorConfigContent is not null)
        {
            File.WriteAllText(editorConfigFileName, editorConfigContent);
        }

        Parameters.Projects[projectName] = new ProjectModel
        {
            ProjectFolderName = projectFolder,
            SolutionFileName = Path.Combine(solutionFolder, $"{projectName}.slnx"),
            EditorConfigPatternName = editorConfigPatternName
        };
        return editorConfigFileName;
    }

    public string ConsoleText()
    {
        return _consoleOutput.ToString();
    }
}

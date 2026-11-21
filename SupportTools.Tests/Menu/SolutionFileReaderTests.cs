using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SupportTools.Menu;
using Xunit;

namespace SupportTools.Tests.Menu;

public sealed class SolutionFileReaderTests : IDisposable
{
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public void Dispose()
    {
        Directory.Delete(_tempFolder, true);
    }

    [Theory]
    [InlineData("App.sln", true)]
    [InlineData("App.SLNX", true)]
    [InlineData("App.csproj", false)]
    [InlineData("App.slnx.txt", false)]
    public void IsSolutionFile_WhenCalled_AcceptsOnlySlnAndSlnx(string fileName, bool expected)
    {
        // Act
        bool result = SolutionFileReader.IsSolutionFile(fileName);

        // Assert
        Assert.Equal(expected, result);
    }

    //the projects inside solution folders count too; the solution items (File) are not projects
    [Fact]
    public void ReadProjectPaths_WhenSlnxHasFoldersAndSolutionItems_ReturnsEveryProjectPath()
    {
        // Arrange
        string solutionFile = WriteFile("App.slnx", """
                                                    <Solution>
                                                      <Folder Name="/Lib/">
                                                        <Project Path="../Lib/Lib.Core/Lib.Core.csproj" />
                                                      </Folder>
                                                      <Folder Name="/Solution Items/">
                                                        <File Path="Directory.Build.props" />
                                                      </Folder>
                                                      <Project Path="../Front/front/front.esproj" />
                                                      <Project Path="App.Api/App.Api.csproj" Id="c78f030b-aaef-4d9b-81a7-822edd393117" />
                                                    </Solution>
                                                    """);

        // Act
        List<string> result = SolutionFileReader.ReadProjectPaths(solutionFile);

        // Assert
        Assert.Equal(["../Lib/Lib.Core/Lib.Core.csproj", "../Front/front/front.esproj", "App.Api/App.Api.csproj"],
            result);
    }

    [Fact]
    public void ReadProjectPaths_WhenSlnHasSolutionFolders_ReturnsOnlyTheProjects()
    {
        // Arrange
        string solutionFile = WriteFile("App.sln", """
                                                   Microsoft Visual Studio Solution File, Format Version 12.00
                                                   Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App.Api", "App.Api\App.Api.csproj", "{11111111-1111-1111-1111-111111111111}"
                                                   EndProject
                                                   Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Libs", "Libs", "{22222222-2222-2222-2222-222222222222}"
                                                   EndProject
                                                   Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Lib.Core", "..\Lib\Lib.Core\Lib.Core.csproj", "{33333333-3333-3333-3333-333333333333}"
                                                   EndProject
                                                   """);

        // Act
        List<string> result = SolutionFileReader.ReadProjectPaths(solutionFile);

        // Assert
        Assert.Equal([@"App.Api\App.Api.csproj", @"..\Lib\Lib.Core\Lib.Core.csproj"], result);
    }

    [Fact]
    public void ReadProjectPaths_WhenSlnxIsNotXml_ThrowsXmlException()
    {
        // Arrange
        string solutionFile = WriteFile("App.slnx", "not xml");

        // Act & Assert
        Assert.Throws<XmlException>(() => SolutionFileReader.ReadProjectPaths(solutionFile));
    }

    private string WriteFile(string fileName, string content)
    {
        string filePath = Path.Combine(_tempFolder, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }
}

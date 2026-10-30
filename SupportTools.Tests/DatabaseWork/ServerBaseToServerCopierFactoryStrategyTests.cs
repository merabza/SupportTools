using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using AppCliTools.LibDataInput;
using LibDatabaseWork.ToolCommands.BaseCopier.ServerBaseToServerCopier;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.DatabaseWork;

//შეცდომის ხაზები და შეჯამება კონსოლზე იწერება
[Collection(ConsoleCaptureCollection.Name)]
public sealed class ServerBaseToServerCopierFactoryStrategyTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new();
    private readonly TextWriter _originalOut = Console.Out;

    public ServerBaseToServerCopierFactoryStrategyTests()
    {
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void SelectDestinationDatabaseParameters_AsksDestination_ReturnsItsNewDatabaseParameters()
    {
        // Arrange
        ServerInfoModel source = CreateServerInfo("Riga", "Prod", "RigaDb", "RigaNewDb");
        ServerInfoModel destination = CreateServerInfo("Oslo", "Prod", "OsloDb", "OsloNewDb");
        var withoutNewDatabase = new ServerInfoModel { ServerName = "Paris", EnvironmentName = "Prod" };
        ProjectModel project = CreateProject(source, destination, withoutNewDatabase);
        string? question = null;
        List<string>? offered = null;
        ServerBaseToServerCopierFactoryStrategy strategy = CreateStrategy((caption, keys) =>
        {
            question = caption;
            offered = keys;
            return "Oslo|Prod";
        });

        // Act
        DatabaseParameters? result = strategy.SelectDestinationDatabaseParameters(project, source);

        // Assert
        Assert.Equal("Copy database RigaDb from server Riga|Prod to which server?", question);
        Assert.Equal(["Oslo|Prod", "Riga|Prod"], offered);
        Assert.Same(destination.NewDatabaseParameters, result);
        Assert.Contains("Database RigaDb of server Riga|Prod will be copied to database OsloNewDb of server Oslo|Prod",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SelectDestinationDatabaseParameters_NoDestination_ReturnsNullWithoutAsking()
    {
        // Arrange
        var source = new ServerInfoModel
        {
            ServerName = "Riga",
            EnvironmentName = "Prod",
            CurrentDatabaseParameters = new DatabaseParameters { DatabaseName = "RigaDb" }
        };
        ProjectModel project = CreateProject(source);
        bool asked = false;
        ServerBaseToServerCopierFactoryStrategy strategy = CreateStrategy((_, _) =>
        {
            asked = true;
            return null;
        });

        // Act
        DatabaseParameters? result = strategy.SelectDestinationDatabaseParameters(project, source);

        // Assert
        Assert.Null(result);
        Assert.False(asked);
    }

    [Fact]
    public void SelectDestinationDatabaseParameters_InputEscaped_ReturnsNull()
    {
        // Arrange
        ServerInfoModel source = CreateServerInfo("Riga", "Prod", "RigaDb", "RigaNewDb");
        ProjectModel project = CreateProject(source, CreateServerInfo("Oslo", "Prod", "OsloDb", "OsloNewDb"));
        ServerBaseToServerCopierFactoryStrategy strategy =
            CreateStrategy((_, _) => throw new DataInputException("Escaped"));

        // Act
        DatabaseParameters? result = strategy.SelectDestinationDatabaseParameters(project, source);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ToolCommandName_IsEnumName()
    {
        // Act
        string name = CreateStrategy((_, _) => null).ToolCommandName;

        // Assert
        Assert.Equal(nameof(EProjectServerTools.ServerBaseToServerCopier), name);
    }

    private static ServerBaseToServerCopierFactoryStrategy CreateStrategy(
        Func<string, List<string>, string?> selectDestinationServer)
    {
        return new ServerBaseToServerCopierFactoryStrategy(Mock.Of<IApplication>(), NullLogger.Instance,
            Mock.Of<IHttpClientFactory>(), selectDestinationServer);
    }

    private static ServerInfoModel CreateServerInfo(string serverName, string environmentName,
        string currentDatabaseName, string newDatabaseName)
    {
        return new ServerInfoModel
        {
            ServerName = serverName,
            EnvironmentName = environmentName,
            CurrentDatabaseParameters = new DatabaseParameters { DatabaseName = currentDatabaseName },
            NewDatabaseParameters = new DatabaseParameters { DatabaseName = newDatabaseName }
        };
    }

    private static ProjectModel CreateProject(params ServerInfoModel[] serverInfos)
    {
        var project = new ProjectModel();
        foreach (ServerInfoModel serverInfo in serverInfos)
        {
            project.ServerInfos.Add(Guid.NewGuid().ToString(), serverInfo);
        }

        return project;
    }
}

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.Cruders;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Cruders;

//the simple names cruders edit lists inside the parameters: every change ends with saving the whole parameters
//object through the cruder's IParametersManager
public sealed class SimpleNamesCrudersSaveTests
{
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public SimpleNamesCrudersSaveTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Theory]
    [InlineData(nameof(EditorConfigPatternsCruder))]
    [InlineData(nameof(EnvironmentCruder))]
    [InlineData(nameof(GitIgnoreModelsCruder))]
    [InlineData(nameof(KeyFieldNamesLisCruder))]
    [InlineData(nameof(NpmPackagesCruder))]
    [InlineData(nameof(ProjectNpmPackagesLisCruder))]
    [InlineData(nameof(ReactAppTypeCruder))]
    [InlineData(nameof(RedundantFileNameCruder))]
    [InlineData(nameof(RunTimeCruder))]
    public async Task Save_WhenCalled_SavesTheWholeParameters(string cruderName)
    {
        // Arrange
        Cruder sut = CreateCruder(cruderName);

        // Act
        bool result = await sut.Save("Saved");

        // Assert
        Assert.True(result);
        _parametersManager.Verify(x => x.Save(_parameters, "Saved", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    private Cruder CreateCruder(string cruderName)
    {
        ILogger logger = new Mock<ILogger>().Object;
        IHttpClientFactory httpClientFactory = new Mock<IHttpClientFactory>().Object;
        IParametersManager parametersManager = _parametersManager.Object;
        return cruderName switch
        {
            nameof(EditorConfigPatternsCruder) => EditorConfigPatternsCruder.Create(logger, httpClientFactory,
                parametersManager),
            nameof(EnvironmentCruder) => EnvironmentCruder.Create(parametersManager),
            nameof(GitIgnoreModelsCruder) => GitIgnoreModelsCruder.Create(logger, httpClientFactory, parametersManager),
            nameof(KeyFieldNamesLisCruder) => new KeyFieldNamesLisCruder(parametersManager, []),
            nameof(NpmPackagesCruder) => NpmPackagesCruder.Create(parametersManager),
            nameof(ProjectNpmPackagesLisCruder) => new ProjectNpmPackagesLisCruder(logger, httpClientFactory,
                parametersManager, []),
            nameof(ReactAppTypeCruder) => ReactAppTypeCruder.Create(logger, parametersManager),
            nameof(RedundantFileNameCruder) => new RedundantFileNameCruder(parametersManager, "Project"),
            nameof(RunTimeCruder) => RunTimeCruder.Create(parametersManager),
            _ => throw new ArgumentOutOfRangeException(nameof(cruderName), cruderName, null)
        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibSupportToolsServerWork.CliMenuCommands;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class EditorConfigFileTypesStsCliMenuCommandTests : IDisposable
{
    private const string ApiClientName = "SupportToolsServer";
    private const string ListRequest = "GET /api/v1/git/editorconfigfiletypeslist";

    private readonly EditorConfigTestEnvironment _env = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly RoutingHttpMessageHandler _server = new();

    public EditorConfigFileTypesStsCliMenuCommandTests()
    {
        _env.Parameters.SupportToolsServerWebApiClientName = ApiClientName;
        _env.Parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "http://127.0.0.1:0/api/v1" };
        _server.Respond(ListRequest, HttpStatusCode.OK,
            JsonSerializer.Serialize(new List<StsEditorConfigFileTypeDataModel>
            {
                new() { Name = "default", Content = EditorConfigTestEnvironment.TemplateContent }
            }));
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(_server, false));
    }

    public void Dispose()
    {
        _server.Dispose();
        _env.Dispose();
    }

    [Fact]
    public void Constructor_WhenCreated_SetsMenuNameAndLoadsSubMenu()
    {
        // Act
        EditorConfigFileTypesStsCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal("EditorConfig File Types", sut.Name);
        Assert.Equal(EMenuAction.LoadSubMenu, sut.MenuActionOnBodySuccess);
    }

    [Fact]
    public void GetSubMenu_WhenCalled_ListsTheServerRecords()
    {
        // Act
        CliMenuSet subMenu = CreateSut().GetSubMenu();

        // Assert
        Assert.Equal("EditorConfig File Types", subMenu.Caption);
        Assert.Contains(CliMenuTestAccess.GetMenuItems(subMenu), x => x.MenuItemName == "default");
    }

    //the menu loop calls GetSubMenu again after every change, so every call must read the server again
    [Fact]
    public void GetSubMenu_WhenCalledAgain_DownloadsTheListAgain()
    {
        // Arrange
        EditorConfigFileTypesStsCliMenuCommand sut = CreateSut();
        sut.GetSubMenu();

        // Act
        sut.GetSubMenu();

        // Assert
        Assert.Equal([ListRequest, ListRequest], _server.Requests.Select(x => x.Request));
    }

    private EditorConfigFileTypesStsCliMenuCommand CreateSut()
    {
        return new EditorConfigFileTypesStsCliMenuCommand(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _env.ParametersManager.Object);
    }

    //remembers every request ("METHOD path" and body) and answers by "METHOD path" (200 without a body by default)
    private sealed class RoutingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode StatusCode, string Body)> _responses = [];

        public List<(string Request, string? Body)> Requests { get; } = [];

        public void Respond(string request, HttpStatusCode statusCode, string body)
        {
            _responses[request] = (statusCode, body);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string key = $"{request.Method} {request.RequestUri!.AbsolutePath}";
            Requests.Add((key,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));

            if (!_responses.TryGetValue(key, out (HttpStatusCode StatusCode, string Body) response))
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };
            }

            return new HttpResponseMessage(response.StatusCode)
            {
                RequestMessage = request,
                Content = new StringContent(response.Body, Encoding.UTF8,
                    response.StatusCode == HttpStatusCode.OK ? "application/json" : "application/problem+json")
            };
        }
    }
}

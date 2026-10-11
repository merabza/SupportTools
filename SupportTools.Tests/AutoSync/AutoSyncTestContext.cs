using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using LibSupportToolsServerWork.Registry.Sync;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using SupportTools.AutoSync;
using SupportTools.Tests.Registry.Sync;

namespace SupportTools.Tests.AutoSync;

//auto-sync (D2) over fake adapters (FakeRegistrySyncAdapter, an in-memory server with B1's version rules) and the real
//engine. The parameters have auto-sync on and the bootstrap ApiClient of SupportToolsServer; the inner parameters
//manager is the mock of RegistrySyncTestContext, whose Save succeeds. The console output is captured, so the test
//classes that use the context belong to ConsoleCaptureCollection
internal sealed class AutoSyncTestContext : IDisposable
{
    public const string ApiClientName = "SupportToolsServer";
    public const string ServerAddress = "http://sts.example.test/api/v1";
    public const string Environments = "Environments";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly TextWriter _originalConsoleOutput;

    public AutoSyncTestContext()
    {
        Sync.Parameters.AutoSyncWithSupportToolsServer = true;
        Sync.Parameters.SupportToolsServerWebApiClientName = ApiClientName;
        Sync.Parameters.ApiClients[ApiClientName] =
            new ApiClientSettings { Server = ServerAddress, ApiKey = "fake-api-key-of-the-tests" };
        //the fake adapters never send a request; the API client only needs an HttpClient to exist
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() =>
        {
            var client = new HttpClient();
            CreatedHttpClients.Add(client);
            return client;
        });
        EnvironmentsAdapter = Sync.CreateAdapter(Environments, 10);
        Adapters.Add(EnvironmentsAdapter);

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public RegistrySyncTestContext Sync { get; } = new();
    public FakeRegistrySyncAdapter EnvironmentsAdapter { get; }
    public List<IRegistrySyncAdapter> Adapters { get; } = [];

    //the clients that the API client of every sync took from the factory
    public List<HttpClient> CreatedHttpClients { get; } = [];

    //warnings that the adapters of every sync report, as the real adapters do (a missing path mapping, …)
    public List<(string CollectionName, string? Key, string Message)> AdapterWarnings { get; } = [];

    //everything written to the console since the context was created
    public string ConsoleOutput => _consoleOutput.ToString();

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        CreatedHttpClients.ForEach(x => x.Dispose());
    }

    public RegistryAutoSync CreateAutoSync()
    {
        return new RegistryAutoSync(new Mock<ILogger>().Object, _httpClientFactory.Object,
            Sync.ParametersManager.Object, (_, _, _, warnings) =>
            {
                AdapterWarnings.ForEach(x => warnings.Add(x.CollectionName, x.Key, x.Message));
                return Adapters;
            });
    }

    //the decorator over the mock inner manager, with its own auto-sync
    public AutoSyncParametersManager CreateParametersManager()
    {
        return new AutoSyncParametersManager(Sync.ParametersManager.Object, CreateAutoSync());
    }

    public List<string> ConsoleLines()
    {
        return [.. ConsoleOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)];
    }
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibAppInstallWork.ToolCommands.ServiceInstallScriptCreator;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SupportTools.Tests.ServiceInstallScriptCreator;

public sealed class CreateServiceInstallScriptToolActionTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"ServiceInstallScript-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    [Fact]
    public async Task Run_WithPorts_OpensEachPortBeforeTheServiceStarts()
    {
        string script = await CreateScript([5051, 5443]);

        int open5051 = script.IndexOf("\nopen_port 5051\n", StringComparison.Ordinal);
        int open5443 = script.IndexOf("\nopen_port 5443\n", StringComparison.Ordinal);
        int moveFiles = script.IndexOf("sudo mv -v $expandedFolderName/*", StringComparison.Ordinal);
        int serviceStart = script.IndexOf("sudo systemctl start $serviceEnvName.service", StringComparison.Ordinal);
        Assert.True(moveFiles > 0 && open5051 > moveFiles && open5443 > open5051 && serviceStart > open5443);
        Assert.Contains("open_port() {", script, StringComparison.Ordinal);
        Assert.Contains("sudo nft add element inet $webminTable $webminTcpPorts \"{ $1 }\"", script,
            StringComparison.Ordinal);
        Assert.Contains("Kestrel endpoints: 5051, 5443", script, StringComparison.Ordinal);
        Assert.DoesNotContain("\r", script, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WithoutPorts_OpensNoPort()
    {
        string script = await CreateScript([]);

        Assert.DoesNotContain("\nopen_port ", script, StringComparison.Ordinal);
        Assert.Contains("Kestrel endpoints: none, Open Port is off", script, StringComparison.Ordinal);
    }

    private async Task<string> CreateScript(int[] ports)
    {
        string scriptFileName = Path.Combine(_folder, "AppInstall.sh");
        var sut = new CreateServiceInstallScriptToolAction(NullLogger.Instance, scriptFileName, ports, "ftp.example",
            "ftpUser", "/install/", "App", "sig", "App description", "linux-x64", "Test", "/home/u/download",
            "/opt/apps", "appsettings.json", "svcUser");

        Assert.True(await sut.Run(CancellationToken.None));
        return await File.ReadAllTextAsync(scriptFileName);
    }
}

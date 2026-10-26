using ToolsManagement.Installer.ServiceInstaller;
using Xunit;

namespace SupportTools.Tests.Installer;

//the Linux installer adds a port to the tcp ports set of Webmin's nftables table in /etc/nftables.conf
public sealed class NftablesConfTests
{
    private const string Set = "profile_hosting_tcp_ports";

    //the shape of oslo's /etc/nftables.conf after Virtualmin set it up
    private const string WebminConf = """
                                      table inet webmin_profile_hosting {
                                              set profile_hosting_tcp_ports {
                                                      type inet_service;
                                                      flags interval;
                                                      elements = { 20, 22, 443, 10000-10100, 49152-65535 }
                                              }
                                              set profile_hosting_udp_ports {
                                                      type inet_service;
                                                      elements = { 53, 443 }
                                              }
                                      }
                                      """;

    [Fact]
    public void AddPortToSetElements_WithNewPort_AddsItAtTheEndOfTheSetOnly()
    {
        string? result = NftablesConf.AddPortToSetElements(WebminConf, Set, 5031);

        Assert.Equal(WebminConf.Replace("49152-65535 }", "49152-65535, 5031 }", System.StringComparison.Ordinal),
            result);
    }

    [Fact]
    public void AddPortToSetElements_WithPortAlreadyListed_ReturnsTheSameText()
    {
        Assert.Same(WebminConf, NftablesConf.AddPortToSetElements(WebminConf, Set, 443));
    }

    [Fact]
    public void AddPortToSetElements_WithPortInsideARange_AddsIt()
    {
        //only a separate element counts; the installer edits the file only when nft says the set lacks the port
        string? result = NftablesConf.AddPortToSetElements(WebminConf, Set, 10000);

        Assert.Contains("49152-65535, 10000 }", result, System.StringComparison.Ordinal);
    }

    [Fact]
    public void AddPortToSetElements_WithEmptyElements_AddsThePort()
    {
        const string conf = "set profile_hosting_tcp_ports {\n type inet_service;\n elements = { }\n}";

        Assert.Equal("set profile_hosting_tcp_ports {\n type inet_service;\n elements = { 5031 }\n}",
            NftablesConf.AddPortToSetElements(conf, Set, 5031));
    }

    [Theory]
    [InlineData("")]
    [InlineData("table inet filter {\n chain input {\n type filter hook input priority filter;\n }\n}")]
    [InlineData("set profile_hosting_tcp_ports {\n type inet_service;\n}")]
    public void AddPortToSetElements_WithoutTheSetElements_ReturnsNull(string conf)
    {
        Assert.Null(NftablesConf.AddPortToSetElements(conf, Set, 5031));
    }
}

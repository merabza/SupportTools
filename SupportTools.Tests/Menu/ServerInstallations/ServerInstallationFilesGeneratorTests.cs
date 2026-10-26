using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SupportTools.Menu.ServerInstallations;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;
using YamlDotNet.RepresentationModel;

namespace SupportTools.Tests.Menu.ServerInstallations;

public sealed class ServerInstallationFilesGeneratorTests
{
    private const string PasswordHash =
        "$6$saltstring$svn8UoSVapNtMuq1ukKS4tPQd8iKwSMHWjl/O817G3uBnIFNjnQJuesI68u4OTLiBFdcbYEdFCoEOfaS35inz1";

    private const string MariaDbHash = "*2470C0C06DEE42FD1618BB99005ADCA2EC9D1E19";

    private static readonly string SqlServerHash = "0x0200" + new string('A', 136);

    //the parameters of the oslo installation (D:\1Work\oslo)
    private static ServerInstallationModel CreateOslo()
    {
        return new ServerInstallationModel
        {
            Hostname = "oslo",
            DomainName = "cyberia.ge",
            RealName = "Merab O'Test",
            UserName = "merab",
            PasswordHash = PasswordHash,
            SshAuthorizedKeys = { "ssh-ed25519 AAAAC3Nza merab@example.com" },
            NetworkInterfaceName = "enp3s0",
            IpV4Addresses = { "192.168.10.5/24" },
            Gateway = "192.168.10.1",
            NameServers = { "192.168.10.1", "212.58.96.96" },
            InstallDiskSerial = "CT500P3PSSD8_2331E865DB19_1",
            InstallVirtualmin = true,
            EmailToAddress = "admin@example.com",
            MariaDbRootPasswordHash = MariaDbHash,
            InstallPostgreSql = true,
            InstallMsSqlServer = true,
            SqlServerSaPasswordHash = SqlServerHash
        };
    }

    //only the required values, no applications and an address from DHCP
    private static ServerInstallationModel CreateMinimal()
    {
        return new ServerInstallationModel
        {
            Hostname = "test",
            UserName = "user",
            PasswordHash = PasswordHash,
            NetworkInterfaceName = "eth0",
            InstallDiskSerial = "SERIAL"
        };
    }

    private static YamlMappingNode LoadAutoinstall(string yamlText)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(yamlText));
        return (YamlMappingNode)((YamlMappingNode)yaml.Documents[0].RootNode)["autoinstall"];
    }

    private static YamlNode Get(YamlNode node, params string[] path)
    {
        return path.Aggregate(node, (current, key) => ((YamlMappingNode)current)[key]);
    }

    private static string Scalar(YamlNode node, params string[] path)
    {
        return ((YamlScalarNode)Get(node, path)).Value!;
    }

    private static List<string> Scalars(YamlNode node, params string[] path)
    {
        return [.. ((YamlSequenceNode)Get(node, path)).Children.Cast<YamlScalarNode>().Select(x => x.Value!)];
    }

    //the second late command writes ~/install-apps.sh's script
    private static string GetInstallAppsLateCommand(ServerInstallationModel installation)
    {
        return Scalars(LoadAutoinstall(ServerInstallationFilesGenerator.CreateAutoinstallYaml(installation)),
            "late-commands")[1];
    }

    private static string GetInstallAppsComment(ServerInstallationModel installation)
    {
        YamlMappingNode autoinstall =
            LoadAutoinstall(ServerInstallationFilesGenerator.CreateAutoinstallYaml(installation));
        YamlNode writeFile = ((YamlSequenceNode)Get(autoinstall, "user-data", "write_files")).Children[0];
        return Scalar(writeFile, "content").Split('\n')[1];
    }

    [Fact]
    public void GetFqdn_WithDomain_JoinsTheHostNameAndTheDomain()
    {
        // Act
        string result = ServerInstallationFilesGenerator.GetFqdn(CreateOslo());

        // Assert
        Assert.Equal("oslo.cyberia.ge", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void GetFqdn_WithoutDomain_ReturnsTheHostName(string? domainName)
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.DomainName = domainName;

        // Act
        string result = ServerInstallationFilesGenerator.GetFqdn(installation);

        // Assert
        Assert.Equal("oslo", result);
    }

    [Fact]
    public void GetFqdn_WithoutHostNameAndDomain_ReturnsEmpty()
    {
        // Act
        string result = ServerInstallationFilesGenerator.GetFqdn(new ServerInstallationModel());

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Validate_ForOslo_ReturnsNoErrors()
    {
        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(CreateOslo());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Validate_ForMinimalInstallation_ReturnsNoErrors()
    {
        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(CreateMinimal());

        // Assert
        Assert.Empty(result);
    }

    //a new installation has the defaults of the time zone, the locale and the keyboard, but nothing else
    [Fact]
    public void Validate_ForNewInstallation_RequiresTheValuesWithoutDefaults()
    {
        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(new ServerInstallationModel());

        // Assert
        Assert.Equal(
        [
            "Hostname is required", "UserName is required", "PasswordHash is required",
            "NetworkInterfaceName is required", "InstallDiskSerial is required"
        ], result);
    }

    [Fact]
    public void Validate_WithoutTimezoneLocaleAndKeyboard_RequiresThem()
    {
        // Arrange
        ServerInstallationModel installation = CreateMinimal();
        installation.Timezone = string.Empty;
        installation.Locale = " ";
        installation.KeyboardLayout = null;

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Equal(["Timezone is required", "Locale is required", "KeyboardLayout is required"], result);
    }

    [Fact]
    public void Validate_WithoutRequiredAndWithInvalidValues_ReturnsEveryErrorInOrder()
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.Hostname = "oslo;rm";
        installation.DomainName = null;
        installation.PasswordHash = null;
        installation.IpV4Addresses.Add("192.168.10.6");
        installation.InstallDiskSerial = " ";
        installation.SqlServerSaPasswordHash = null;
        installation.SqlServerPort = 70000;

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Equal(
        [
            "Hostname has an invalid value: oslo;rm",
            "DomainName is required for Virtualmin (it needs a full host name)",
            "PasswordHash is required",
            "IPv4 Address 192.168.10.6 must look like 192.168.10.5/24",
            "InstallDiskSerial is required",
            "SqlServerSaPasswordHash is required",
            "SqlServerPort must be 1-65535, or 0 for the default port"
        ], result);
    }

    //the values reach YAML, shell and perl, so only their safe characters pass; a value that ends with a line feed
    //does not pass either
    [Theory]
    [InlineData(nameof(ServerInstallationModel.Hostname), "-oslo")]
    [InlineData(nameof(ServerInstallationModel.Hostname), "oslo\n")]
    [InlineData(nameof(ServerInstallationModel.DomainName), "cyberia.ge.")]
    [InlineData(nameof(ServerInstallationModel.RealName), "Merab\nZ")]
    [InlineData(nameof(ServerInstallationModel.UserName), "Merab")]
    [InlineData(nameof(ServerInstallationModel.PasswordHash), "secret")]
    [InlineData(nameof(ServerInstallationModel.NetworkInterfaceName), "enp 3s0")]
    [InlineData(nameof(ServerInstallationModel.Timezone), "Asia Tbilisi")]
    [InlineData(nameof(ServerInstallationModel.Locale), "en US")]
    [InlineData(nameof(ServerInstallationModel.KeyboardLayout), "u's")]
    [InlineData(nameof(ServerInstallationModel.InstallDiskSerial), "SERIAL\nX")]
    [InlineData(nameof(ServerInstallationModel.DnsPrimaryNameServer), "192.168.10.1, 212.58.96.96")]
    [InlineData(nameof(ServerInstallationModel.EmailToAddress), "admin")]
    [InlineData(nameof(ServerInstallationModel.EmailToAddress), "admin@example.com\n")]
    [InlineData(nameof(ServerInstallationModel.MariaDbRootPasswordHash), "*ABC")]
    [InlineData(nameof(ServerInstallationModel.SqlServerEdition), "Enterprise Developer")]
    [InlineData(nameof(ServerInstallationModel.SqlServerSaPasswordHash), "0x0100AA")]
    public void Validate_WithInvalidValue_NamesTheFieldAndTheValue(string propertyName, string value)
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        typeof(ServerInstallationModel).GetProperty(propertyName)!.SetValue(installation, value);

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Equal([$"{propertyName} has an invalid value: {value}"], result);
    }

    [Theory]
    [InlineData(" ", "SSH Authorized Key is required")]
    [InlineData("ssh-ed25519 A\nB", "SSH Authorized Key has an invalid value: ssh-ed25519 A\nB")]
    public void Validate_WithInvalidSshKey_NamesTheKey(string key, string expected)
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.SshAuthorizedKeys.Add(key);

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Equal([expected], result);
    }

    [Theory]
    [InlineData("192.168.10.6")]
    [InlineData("192.168.10/24")]
    [InlineData("192.168.10.6/0")]
    [InlineData("192.168.10.6/33")]
    [InlineData("192.168.10.6/+24")]
    [InlineData("192.168.10.6/ 24")]
    [InlineData("192.168.10.6/24/1")]
    [InlineData("2001:db8::6/64")]
    [InlineData("host/24")]
    public void Validate_WithInvalidIpV4Address_NamesTheAddress(string address)
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.IpV4Addresses.Add(address);

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Equal([$"IPv4 Address {address} must look like 192.168.10.5/24"], result);
    }

    [Theory]
    [InlineData("10.0.0.1/1")]
    [InlineData("10.0.0.1/32")]
    public void Validate_WithPrefixAtTheLimits_AcceptsTheAddress(string address)
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.IpV4Addresses.Add(address);

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("2001:db8::1")]
    [InlineData("192.168.10")]
    [InlineData("router")]
    public void Validate_WithInvalidGateway_NamesTheGateway(string gateway)
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.Gateway = gateway;

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Equal([$"Gateway {gateway} is not an IPv4 address"], result);
    }

    [Theory]
    [InlineData("dns.example")]
    [InlineData("212.58.96")]
    public void Validate_WithInvalidNameServer_NamesTheNameServer(string nameServer)
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.NameServers.Add(nameServer);

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Equal([$"Name Server {nameServer} is not an IP address"], result);
    }

    [Fact]
    public void Validate_WithIpV6NameServer_AcceptsIt()
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.NameServers.Add("2001:4860:4860::8888");

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    public void Validate_WithGrubTimeout_RejectsOnlyANegativeOne(int timeout, bool expectError)
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.GrubTimeoutSeconds = timeout;
        List<string> expected = expectError ? ["GrubTimeoutSeconds cannot be negative"] : [];

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Equal(expected, result);
    }

    //without the application its answers and hashes are not written, so they are not checked
    [Fact]
    public void Validate_WithoutVirtualmin_IgnoresTheDomainAndTheWizardAnswers()
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.InstallVirtualmin = false;
        installation.DomainName = null;
        installation.DnsPrimaryNameServer = "192.168.10.1, 212.58.96.96";
        installation.EmailToAddress = "admin";

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Validate_WithoutVirtualmin_IgnoresTheMariaDbHash()
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.InstallVirtualmin = false;
        installation.DomainName = null;
        installation.MariaDbRootPasswordHash = "*ABC";

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Validate_WithoutSqlServer_IgnoresItsEditionHashAndPort()
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.InstallMsSqlServer = false;
        installation.SqlServerEdition = string.Empty;
        installation.SqlServerSaPasswordHash = null;
        installation.SqlServerPort = 70000;

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Validate_WithSqlServerWithoutEdition_RequiresIt()
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.SqlServerEdition = null;

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Equal(["SqlServerEdition is required"], result);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(65535, false)]
    [InlineData(65536, true)]
    public void Validate_WithSqlServerPort_AcceptsZeroAndTheTcpPorts(int port, bool expectError)
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.SqlServerPort = port;
        List<string> expected = expectError ? ["SqlServerPort must be 1-65535, or 0 for the default port"] : [];

        // Act
        List<string> result = ServerInstallationFilesGenerator.Validate(installation);

        // Assert
        Assert.Equal(expected, result);
    }

    //the grub.cfg of oslo with the timeout of the installation
    [Fact]
    public void CreateGrubCfg_WithTimeout_StartsTheAutoinstallWithLineFeeds()
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.GrubTimeoutSeconds = 5;

        // Act
        string result = ServerInstallationFilesGenerator.CreateGrubCfg(installation);

        // Assert
        Assert.Equal("""
                     set timeout=5

                     loadfont unicode

                     set menu_color_normal=white/black
                     set menu_color_highlight=black/light-gray

                     menuentry "Try or Install Ubuntu Server" {
                         set gfxpayload=keep
                         linux  /casper/vmlinuz  autoinstall noprompt ---
                         initrd /casper/initrd
                     }
                     grub_platform
                     if [ "$grub_platform" = "efi" ]; then
                     menuentry 'Boot from next volume' {
                         exit 1
                     }
                     menuentry 'UEFI Firmware Settings' {
                         fwsetup
                     }
                     fi

                     """.ReplaceLineEndings("\n"), result);
    }

    [Fact]
    public void CreateAutoinstallYaml_ForOslo_HasTheParametersInYaml()
    {
        // Act
        string result = ServerInstallationFilesGenerator.CreateAutoinstallYaml(CreateOslo());

        // Assert
        Assert.StartsWith("#cloud-config\n", result);
        YamlMappingNode autoinstall = LoadAutoinstall(result);
        Assert.Equal("offline-install", Scalar(autoinstall, "apt", "fallback"));
        Assert.Equal("oslo", Scalar(autoinstall, "identity", "hostname"));
        Assert.Equal(PasswordHash, Scalar(autoinstall, "identity", "password"));
        Assert.Equal("Merab O'Test", Scalar(autoinstall, "identity", "realname"));
        Assert.Equal("merab", Scalar(autoinstall, "identity", "username"));
        Assert.Equal("linux-generic", Scalar(autoinstall, "kernel", "package"));
        Assert.Equal("us", Scalar(autoinstall, "keyboard", "layout"));
        Assert.Equal("en_US.UTF-8", Scalar(autoinstall, "locale"));
        Assert.Equal("auto", Scalar(autoinstall, "oem", "install"));
        Assert.Equal("reboot", Scalar(autoinstall, "shutdown"));
        Assert.Equal("ubuntu-server", Scalar(autoinstall, "source", "id"));
        Assert.Equal("Asia/Tbilisi", Scalar(autoinstall, "timezone"));
        Assert.Equal("all", Scalar(autoinstall, "updates"));
        Assert.Equal("1", Scalar(autoinstall, "version"));
    }

    [Fact]
    public void CreateAutoinstallYaml_ForOslo_SetsTheStaticAddressOfTheInterface()
    {
        // Act
        YamlMappingNode autoinstall =
            LoadAutoinstall(ServerInstallationFilesGenerator.CreateAutoinstallYaml(CreateOslo()));

        // Assert
        YamlNode ethernet = Get(autoinstall, "network", "ethernets", "enp3s0");
        Assert.Equal(["192.168.10.5/24"], Scalars(ethernet, "addresses"));
        Assert.Equal(["192.168.10.1", "212.58.96.96"], Scalars(ethernet, "nameservers", "addresses"));
        Assert.Empty((YamlSequenceNode)Get(ethernet, "nameservers", "search"));
        YamlNode route = ((YamlSequenceNode)Get(ethernet, "routes")).Children[0];
        Assert.Equal("default", Scalar(route, "to"));
        Assert.Equal("192.168.10.1", Scalar(route, "via"));
        Assert.Equal("2", Scalar(autoinstall, "network", "version"));
    }

    [Fact]
    public void CreateAutoinstallYaml_ForOslo_InstallsOnTheDiskWithTheSerial()
    {
        // Act
        YamlMappingNode autoinstall =
            LoadAutoinstall(ServerInstallationFilesGenerator.CreateAutoinstallYaml(CreateOslo()));

        // Assert
        Assert.Equal("direct", Scalar(autoinstall, "storage", "layout", "name"));
        Assert.Equal("CT500P3PSSD8_2331E865DB19_1", Scalar(autoinstall, "storage", "layout", "match", "serial"));
    }

    [Fact]
    public void CreateAutoinstallYaml_ForOslo_LetsInOnlyWithTheKeys()
    {
        // Act
        YamlMappingNode autoinstall =
            LoadAutoinstall(ServerInstallationFilesGenerator.CreateAutoinstallYaml(CreateOslo()));

        // Assert
        Assert.Equal("false", Scalar(autoinstall, "ssh", "allow-pw"));
        Assert.Equal(["ssh-ed25519 AAAAC3Nza merab@example.com"], Scalars(autoinstall, "ssh", "authorized-keys"));
        Assert.Equal("true", Scalar(autoinstall, "ssh", "install-server"));
    }

    [Fact]
    public void CreateAutoinstallYaml_WithPasswordLogin_AllowsIt()
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.SshAllowPassword = true;

        // Act
        YamlMappingNode autoinstall =
            LoadAutoinstall(ServerInstallationFilesGenerator.CreateAutoinstallYaml(installation));

        // Assert
        Assert.Equal("true", Scalar(autoinstall, "ssh", "allow-pw"));
    }

    //Linux files: line feeds only, and no line ends with spaces (the empty lines of the scripts are not indented)
    [Fact]
    public void CreateAutoinstallYaml_ForOslo_HasCleanLineEnds()
    {
        // Act
        string result = ServerInstallationFilesGenerator.CreateAutoinstallYaml(CreateOslo());

        // Assert
        Assert.DoesNotContain("\r", result);
        Assert.DoesNotContain(" \n", result);
        Assert.EndsWith("  version: 1\n", result);
    }

    [Fact]
    public void CreateAutoinstallYaml_ForOslo_StartsTheInstalledUbuntuOnTheNextBoot()
    {
        // Act
        YamlMappingNode autoinstall =
            LoadAutoinstall(ServerInstallationFilesGenerator.CreateAutoinstallYaml(CreateOslo()));

        // Assert
        Assert.Equal("""
                     n=$(efibootmgr | sed -n 's/^Boot\([0-9A-Fa-f]\{4\}\)\* ubuntu.*/\1/p' | head -n1)
                     [ -n "$n" ] && efibootmgr -n "$n" || true

                     """.ReplaceLineEndings("\n"), Scalars(autoinstall, "late-commands")[0]);
    }

    [Fact]
    public void CreateAutoinstallYaml_ForOslo_WritesTheInstallAppsScript()
    {
        // Act
        string script = GetInstallAppsLateCommand(CreateOslo());

        // Assert
        Assert.StartsWith("cat > /target/usr/local/sbin/install-apps <<'EOF'\n#!/bin/sh\nset -e\n" +
                          "# curl downloads the installers and repository files below\n" +
                          "if ! dpkg -s curl >/dev/null 2>&1; then\n  apt-get update\n" +
                          "  DEBIAN_FRONTEND=noninteractive apt-get install -y curl\nfi\n# Skip the installer",
            script);
        Assert.DoesNotContain("dotnet", script);
        Assert.Contains(
            "--hostname oslo.cyberia.ge \\\n    --bundle LEMP --extra postgresql,postgresql-client --include PostgreSQL\n",
            script);
        Assert.Contains("  perl /usr/local/sbin/virtualmin-wizard.pl\n", script);
        Assert.Contains($"mysql_native_password USING '{MariaDbHash}'\"\n", script);
        Assert.Contains("apt-get install -y mssql-tools18 unixodbc-dev\n", script);
        Assert.Contains("MSSQL_PID=EnterpriseDeveloper", script);
        Assert.Contains($"ALTER LOGIN sa WITH PASSWORD = {SqlServerHash} HASHED", script);
        Assert.Contains("echo \"All done.\"\nEOF\nchmod 700 /target/usr/local/sbin/install-apps\n", script);
        Assert.DoesNotContain("network.tcpport", script);
    }

    [Fact]
    public void CreateAutoinstallYaml_ForOslo_AnswersTheVirtualminWizard()
    {
        // Act
        string script = GetInstallAppsLateCommand(CreateOslo());

        // Assert
        Assert.Contains("cat > /target/usr/local/sbin/virtualmin-wizard.pl <<'EOF'\n#!/usr/bin/perl\n", script);
        Assert.Contains("require './virtual-server-lib.pl';\n\nmy %answers = (\n", script);
        Assert.Contains("'db'     => { 'mysql' => 1, 'postgres' => 1 },", script);
        Assert.Contains("'prins' => 'oslo.cyberia.ge', 'prins_skip' => 1,", script);
        Assert.Contains("'email'  => { 'from_addr_def' => 1, 'to_addr_def' => 0,\n" +
                        "                  'to_addr' => 'admin@example.com' },", script);
        Assert.EndsWith("&save_module_config_keys({ 'wizard_run' => 1 });\n" +
                        "EOF\nchmod 700 /target/usr/local/sbin/virtualmin-wizard.pl\n", script);
    }

    [Fact]
    public void CreateAutoinstallYaml_ForOslo_PutsTheScriptIntoTheHomeOfTheUser()
    {
        // Act
        YamlMappingNode autoinstall =
            LoadAutoinstall(ServerInstallationFilesGenerator.CreateAutoinstallYaml(CreateOslo()));

        // Assert
        YamlNode writeFile = ((YamlSequenceNode)Get(autoinstall, "user-data", "write_files")).Children[0];
        Assert.Equal("/home/merab/install-apps.sh", Scalar(writeFile, "path"));
        Assert.Equal("merab:merab", Scalar(writeFile, "owner"));
        Assert.Equal("0755", Scalar(writeFile, "permissions"));
        Assert.Equal("""
                     #!/bin/sh
                     # Installs Virtualmin LEMP (with wizard answers), MariaDB, PostgreSQL, SQL Server, sqlcmd
                     exec sudo /usr/local/sbin/install-apps "$@"

                     """.ReplaceLineEndings("\n"), Scalar(writeFile, "content"));
    }

    //a re-run must not set the port again: mssql-conf refuses a port in use, also by SQL Server itself
    [Fact]
    public void CreateAutoinstallYaml_WithSqlServerPort_MovesSqlServerOnceWithoutOpeningThePort()
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.SqlServerPort = 2433;

        // Act
        string script = GetInstallAppsLateCommand(installation);

        // Assert
        Assert.Contains("if ! grep -Eq '^[[:space:]]*tcpport[[:space:]]*=[[:space:]]*2433[[:space:]]*$' \\\n" +
                        "  /var/opt/mssql/mssql.conf 2>/dev/null; then\n" +
                        "  /opt/mssql/bin/mssql-conf set network.tcpport 2433\n  systemctl restart mssql-server\nfi\n",
            script);
        Assert.DoesNotContain("firewall-cmd", script);
        Assert.DoesNotContain("nftables", script);
    }

    //0 and 1433 are SQL Server's default port: mssql-conf would fail with "Port '1433' is already in use"
    [Theory]
    [InlineData(0)]
    [InlineData(1433)]
    public void CreateAutoinstallYaml_WithDefaultSqlServerPort_KeepsThePort(int sqlServerPort)
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.SqlServerPort = sqlServerPort;

        // Act
        string script = GetInstallAppsLateCommand(installation);

        // Assert
        Assert.DoesNotContain("network.tcpport", script);
    }

    [Theory]
    [InlineData(2433, "2433")]
    [InlineData(1433, "1433")]
    [InlineData(0, "1433")]
    public void CreateAutoinstallYaml_WithOpenSqlServerPort_OpensTheListenedPort(int sqlServerPort, string opened)
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.SqlServerPort = sqlServerPort;
        installation.OpenSqlServerPort = true;

        // Act
        string script = GetInstallAppsLateCommand(installation);

        // Assert
        Assert.Contains($"firewall-cmd --permanent --add-port={opened}/tcp\n", script);
        Assert.Contains(
            $"  nft add element inet webmin_profile_hosting profile_hosting_tcp_ports '{{ {opened} }}'\n", script);
        Assert.Contains($"elements = \\{{[^}}]*?)\\s*\\}}/$1, {opened} }}/' \\\n", script);
        Assert.Contains($"echo 'add rule inet filter input tcp dport {opened} accept' >> /etc/nftables.conf\n",
            script);
    }

    [Fact]
    public void CreateAutoinstallYaml_WithVirtualminOnly_AnswersTheWizardWithMariaDbOnlyAndWithoutEmail()
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.InstallWebmin = true;
        installation.MariaDbRootPasswordHash = null;
        installation.InstallPostgreSql = false;
        installation.InstallMsSqlServer = false;
        installation.EmailToAddress = null;
        installation.DnsPrimaryNameServer = "ns1.cyberia.ge";

        // Act
        string script = GetInstallAppsLateCommand(installation);

        // Assert
        Assert.Contains("--hostname oslo.cyberia.ge \\\n    --bundle LEMP\n", script);
        Assert.Contains("'db'     => { 'mysql' => 1, 'postgres' => 0 },", script);
        Assert.Contains("'prins' => 'ns1.cyberia.ge',", script);
        Assert.Contains("'email'  => { 'from_addr_def' => 1, 'to_addr_def' => 1 },", script);
        Assert.DoesNotContain("webmin-setup-repo", script);
        Assert.DoesNotContain("ALTER USER", script);
        Assert.DoesNotContain("mssql", script);
        Assert.Equal("# Installs Virtualmin LEMP (with wizard answers), MariaDB", GetInstallAppsComment(installation));
    }

    [Fact]
    public void CreateAutoinstallYaml_WithoutVirtualmin_InstallsTheSelectedPackagesItself()
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.InstallVirtualmin = false;
        installation.InstallWebmin = true;
        installation.InstallMsSqlServer = false;
        installation.InstallSqlServerClient = true;

        // Act
        string script = GetInstallAppsLateCommand(installation);

        // Assert
        Assert.Contains("  sh /tmp/webmin-setup-repo.sh --force\n", script);
        Assert.Contains("if ! dpkg -s postgresql >/dev/null 2>&1; then\n  apt-get update\n" +
                        "  DEBIAN_FRONTEND=noninteractive apt-get install -y postgresql postgresql-client\nfi\n",
            script);
        Assert.Contains("apt-get install -y mssql-tools18 unixodbc-dev\n", script);
        Assert.DoesNotContain("virtualmin", script);
        Assert.DoesNotContain("mariadb", script);
        Assert.DoesNotContain("mssql-server", script);
        Assert.Equal("# Installs Webmin, PostgreSQL, sqlcmd", GetInstallAppsComment(installation));
    }

    [Fact]
    public void CreateAutoinstallYaml_WithLampBundle_InstallsVirtualminWithApache()
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.Bundle = EVirtualminBundle.LampApache;
        installation.InstallPostgreSql = false;

        // Act
        string script = GetInstallAppsLateCommand(installation);

        // Assert
        Assert.Contains("--hostname oslo.cyberia.ge \\\n    --bundle LAMP\n", script);
        Assert.DoesNotContain("LEMP", script);
        Assert.Equal("# Installs Virtualmin LAMP (with wizard answers), MariaDB, SQL Server, sqlcmd",
            GetInstallAppsComment(installation));
    }

    //without Virtualmin nothing installs MariaDB
    [Fact]
    public void CreateAutoinstallYaml_WithoutApps_InstallsNoMariaDbAndNoSqlTools()
    {
        // Arrange
        ServerInstallationModel installation = CreateMinimal();
        installation.InstallDotnetRuntime = true;
        installation.MariaDbRootPasswordHash = MariaDbHash;

        // Act
        string script = GetInstallAppsLateCommand(installation);

        // Assert
        Assert.DoesNotContain("mariadb", script);
        Assert.DoesNotContain("webmin", script);
        Assert.DoesNotContain("postgresql", script);
        Assert.DoesNotContain("mssql-tools18", script);
    }

    //the SDK contains the runtime, and the Microsoft repository of sqlcmd must not supply .NET
    [Theory]
    [InlineData(true, false, "dotnet-sdk-10.0", ".NET SDK 10.0")]
    [InlineData(true, true, "dotnet-sdk-10.0", ".NET SDK 10.0")]
    [InlineData(false, true, "aspnetcore-runtime-10.0", "ASP.NET Core Runtime 10.0")]
    public void CreateAutoinstallYaml_WithDotnet_InstallsItFromUbuntuFeed(bool installSdk, bool installRuntime,
        string package, string appName)
    {
        // Arrange
        ServerInstallationModel installation = CreateMinimal();
        installation.InstallDotnetSdk = installSdk;
        installation.InstallDotnetRuntime = installRuntime;

        // Act
        string script = GetInstallAppsLateCommand(installation);

        // Assert
        Assert.Contains("printf 'Package: dotnet* aspnet* netstandard*\\nPin: origin \"packages.microsoft.com\"\\n" +
                        "Pin-Priority: -10\\n' \\\n  > /etc/apt/preferences.d/dotnet-not-from-microsoft\n" +
                        $"if ! dpkg -s {package} >/dev/null 2>&1; then\n  apt-get update\n" +
                        $"  DEBIAN_FRONTEND=noninteractive apt-get install -y {package}\nfi\necho \"All done.\"\n",
            script);
        Assert.Equal(installSdk, script.Contains("dotnet-sdk-10.0", StringComparison.Ordinal));
        Assert.Equal(!installSdk, script.Contains("aspnetcore-runtime-10.0", StringComparison.Ordinal));
        Assert.Equal($"# Installs {appName}", GetInstallAppsComment(installation));
    }

    //the root hash is set only for the MariaDB of Virtualmin and only when there is a hash
    [Theory]
    [InlineData(true, null)]
    [InlineData(false, MariaDbHash)]
    public void CreateAutoinstallYaml_WithoutVirtualminOrHash_SetsNoRootPassword(bool installVirtualmin,
        string? hash)
    {
        // Arrange
        ServerInstallationModel installation = CreateOslo();
        installation.InstallVirtualmin = installVirtualmin;
        installation.MariaDbRootPasswordHash = hash;

        // Act
        string script = GetInstallAppsLateCommand(installation);

        // Assert
        Assert.DoesNotContain("ALTER USER", script);
    }

    [Fact]
    public void CreateAutoinstallYaml_WithAddressWithoutGatewayAndNameServers_WritesOnlyTheAddress()
    {
        // Arrange
        ServerInstallationModel installation = CreateMinimal();
        installation.IpV4Addresses.Add("192.168.10.7/24");

        // Act
        YamlMappingNode autoinstall =
            LoadAutoinstall(ServerInstallationFilesGenerator.CreateAutoinstallYaml(installation));

        // Assert
        var ethernet = (YamlMappingNode)Get(autoinstall, "network", "ethernets", "eth0");
        Assert.Equal(["192.168.10.7/24"], Scalars(ethernet, "addresses"));
        Assert.Equal(["addresses"], ethernet.Children.Keys.Cast<YamlScalarNode>().Select(x => x.Value));
    }

    [Fact]
    public void CreateAutoinstallYaml_WithoutAppsAndAddresses_UsesDhcpAndWritesNoScript()
    {
        // Arrange
        ServerInstallationModel installation = CreateMinimal();

        // Act
        YamlMappingNode autoinstall =
            LoadAutoinstall(ServerInstallationFilesGenerator.CreateAutoinstallYaml(installation));

        // Assert
        Assert.Equal("true", Scalar(autoinstall, "network", "ethernets", "eth0", "dhcp4"));
        Assert.Single(Scalars(autoinstall, "late-commands"));
        Assert.Empty((YamlSequenceNode)Get(autoinstall, "ssh", "authorized-keys"));
        Assert.Equal(string.Empty, Scalar(autoinstall, "identity", "realname"));
        Assert.False(autoinstall.Children.ContainsKey("user-data"));
    }
}

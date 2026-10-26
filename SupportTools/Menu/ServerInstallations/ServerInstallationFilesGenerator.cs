using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using SupportToolsData;
using SupportToolsData.Models;

namespace SupportTools.Menu.ServerInstallations;

//საინსტალაციო ფლეშკის grub.cfg და autoinstall.yaml. ნიმუშია D:\1Work\oslo-ს ფაილები, რომლებითაც oslo დადგა.
//ორივე ფაილი Linux-ისაა, ამიტომ ხაზები LF-ით მთავრდება
public static partial class ServerInstallationFilesGenerator
{
    public const string GrubCfgFileName = "grub.cfg";
    public const string AutoinstallFileName = "autoinstall.yaml";

    //.NET-ის ვერსია Ubuntu 26.04-ის საკუთარ feed-ში (dotnet-sdk-10.0, aspnetcore-runtime-10.0)
    private const string DotnetVersion = "10.0";

    public static string GetFqdn(ServerInstallationModel installation)
    {
        return string.IsNullOrWhiteSpace(installation.DomainName)
            ? installation.Hostname ?? string.Empty
            : $"{installation.Hostname}.{installation.DomainName}";
    }

    //მნიშვნელობები shell-ისა და perl-ის ბრძანებებშიც ჩაიწერება, ამიტომ მათი სიმბოლოები შეზღუდულია
    public static List<string> Validate(ServerInstallationModel installation)
    {
        List<string> errors = [];

        CheckRequired(errors, nameof(installation.Hostname), installation.Hostname, HostnameRegex());
        CheckOptional(errors, nameof(installation.DomainName), installation.DomainName, DomainRegex());
        if (installation.InstallVirtualmin && string.IsNullOrWhiteSpace(installation.DomainName))
        {
            errors.Add($"{nameof(installation.DomainName)} is required for Virtualmin (it needs a full host name)");
        }

        CheckOptional(errors, nameof(installation.RealName), installation.RealName, SingleLineRegex());
        CheckRequired(errors, nameof(installation.UserName), installation.UserName, UserNameRegex());
        CheckRequired(errors, nameof(installation.PasswordHash), installation.PasswordHash, LinuxHashRegex());
        foreach (string key in installation.SshAuthorizedKeys)
        {
            CheckRequired(errors, "SSH Authorized Key", key, SingleLineRegex());
        }

        CheckRequired(errors, nameof(installation.NetworkInterfaceName), installation.NetworkInterfaceName,
            InterfaceNameRegex());
        foreach (string address in installation.IpV4Addresses.Where(x => !IsIpV4WithPrefix(x)))
        {
            errors.Add($"IPv4 Address {address} must look like 192.168.10.5/24");
        }

        if (!string.IsNullOrWhiteSpace(installation.Gateway) && !IsIpV4(installation.Gateway))
        {
            errors.Add($"{nameof(installation.Gateway)} {installation.Gateway} is not an IPv4 address");
        }

        foreach (string nameServer in installation.NameServers.Where(x => !IsNameServer(x)))
        {
            errors.Add($"Name Server {nameServer} is not an IP address");
        }

        CheckRequired(errors, nameof(installation.Timezone), installation.Timezone, SimpleValueRegex());
        CheckRequired(errors, nameof(installation.Locale), installation.Locale, SimpleValueRegex());
        CheckRequired(errors, nameof(installation.KeyboardLayout), installation.KeyboardLayout, SimpleValueRegex());
        if (installation.GrubTimeoutSeconds < 0)
        {
            errors.Add($"{nameof(installation.GrubTimeoutSeconds)} cannot be negative");
        }

        CheckRequired(errors, nameof(installation.InstallDiskSerial), installation.InstallDiskSerial,
            SingleLineRegex());

        if (installation.InstallVirtualmin)
        {
            CheckOptional(errors, nameof(installation.DnsPrimaryNameServer), installation.DnsPrimaryNameServer,
                DomainRegex());
            CheckOptional(errors, nameof(installation.EmailToAddress), installation.EmailToAddress, EmailRegex());
            CheckOptional(errors, nameof(installation.MariaDbRootPasswordHash),
                installation.MariaDbRootPasswordHash, MariaDbHashRegex());
        }

        if (installation.InstallMsSqlServer)
        {
            CheckRequired(errors, nameof(installation.SqlServerEdition), installation.SqlServerEdition,
                SqlServerEditionRegex());
            //setup-ს ერთჯერადი პაროლი ეძლევა, ამიტომ ჰეშის გარეშე sa-თი შესვლა შეუძლებელი იქნებოდა
            CheckRequired(errors, nameof(installation.SqlServerSaPasswordHash), installation.SqlServerSaPasswordHash,
                SqlServerHashRegex());
            if (installation.SqlServerPort is < 0 or > 65535)
            {
                errors.Add($"{nameof(installation.SqlServerPort)} must be 1-65535, or 0 for the default port");
            }
        }

        return errors;
    }

    public static string CreateGrubCfg(ServerInstallationModel installation)
    {
        string timeout = installation.GrubTimeoutSeconds.ToString(CultureInfo.InvariantCulture);
        return $$"""
                 set timeout={{timeout}}

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

                 """.ReplaceLineEndings("\n");
    }

    public static string CreateAutoinstallYaml(ServerInstallationModel installation)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $$"""
                    #cloud-config
                    # See the autoinstall documentation at:
                    # https://canonical-subiquity.readthedocs-hosted.com/en/latest/reference/autoinstall-reference.html
                    autoinstall:
                      apt:
                        disable_components: []
                        fallback: offline-install
                        geoip: true
                        mirror-selection:
                          primary:
                          - country-mirror
                          - arches:
                            - amd64
                            - arm64
                            - i386
                            uri: http://archive.ubuntu.com/ubuntu/
                          - arches:
                            - armhf
                            - powerpc
                            - ppc64el
                            - riscv64
                            - s390x
                            uri: http://ports.ubuntu.com/ubuntu-ports
                        preserve_sources_list: false
                        security:
                        - arches:
                          - amd64
                          - arm64
                          - i386
                          uri: http://security.ubuntu.com/ubuntu/
                        - arches:
                          - armhf
                          - powerpc
                          - ppc64el
                          - riscv64
                          - s390x
                          uri: http://ports.ubuntu.com/ubuntu-ports
                      codecs:
                        install: false
                      drivers:
                        install: false
                      identity:
                        hostname: {{Quote(installation.Hostname)}}
                        password: {{Quote(installation.PasswordHash)}}
                        realname: {{Quote(installation.RealName)}}
                        username: {{Quote(installation.UserName)}}
                      kernel:
                        package: linux-generic
                      keyboard:
                        layout: {{Quote(installation.KeyboardLayout)}}
                        toggle: null
                        variant: ''
                      late-commands:
                      # Next boot only: start the freshly installed "ubuntu" UEFI entry, not the USB
                      - |
                        n=$(efibootmgr | sed -n 's/^Boot\([0-9A-Fa-f]\{4\}\)\* ubuntu.*/\1/p' | head -n1)
                        [ -n "$n" ] && efibootmgr -n "$n" || true

                    """);

        List<string> apps = GetAppNames(installation);
        if (apps.Count > 0)
        {
            sb.Append("""
                        # Script that installs the applications, run by hand after the first
                        # boot via ~/install-apps.sh (safe to re-run: finished parts are skipped)
                        - |

                      """);
            sb.Append(Indent(CreateLateCommandScript(installation), 4));
        }

        sb.Append(CultureInfo.InvariantCulture, $$"""
                      locale: {{Quote(installation.Locale)}}
                      network:
                        ethernets:
                          {{installation.NetworkInterfaceName}}:

                    """);
        sb.Append(Indent(CreateInterfaceYaml(installation), 8));
        sb.Append(CultureInfo.InvariantCulture, $$"""
                        version: 2
                      oem:
                        install: auto
                      shutdown: reboot
                      source:
                        id: ubuntu-server
                        search_drivers: false
                      ssh:
                        allow-pw: {{(installation.SshAllowPassword ? "true" : "false")}}

                    """);
        if (installation.SshAuthorizedKeys.Count == 0)
        {
            sb.Append("    authorized-keys: []\n");
        }
        else
        {
            sb.Append("    authorized-keys:\n");
            foreach (string key in installation.SshAuthorizedKeys)
            {
                sb.Append("    - ").Append(Quote(key)).Append('\n');
            }
        }

        //დისკი serial-ით: სხვა კომპიუტერზე ის არ მოიძებნება და ინსტალაცია რამის წაშლამდე გაჩერდება
        sb.Append(CultureInfo.InvariantCulture, $$"""
                        install-server: true
                      storage:
                        layout:
                          name: direct
                          match:
                            serial: {{Quote(installation.InstallDiskSerial)}}
                      timezone: {{Quote(installation.Timezone)}}
                      updates: all

                    """);
        if (apps.Count > 0)
        {
            sb.Append(CultureInfo.InvariantCulture, $$"""
                          user-data:
                            write_files:
                            - path: /home/{{installation.UserName}}/install-apps.sh
                              owner: {{installation.UserName}}:{{installation.UserName}}
                              permissions: '0755'
                              defer: true
                              content: |
                                #!/bin/sh
                                # Installs {{string.Join(", ", apps)}}
                                exec sudo /usr/local/sbin/install-apps "$@"

                        """);
        }

        sb.Append("  version: 1\n");
        return sb.ToString().ReplaceLineEndings("\n");
    }

    private static List<string> GetAppNames(ServerInstallationModel installation)
    {
        List<string> apps = [];
        if (installation.InstallVirtualmin)
        {
            apps.Add($"Virtualmin {GetBundleName(installation.Bundle)} (with wizard answers)");
            apps.Add("MariaDB");
        }
        else if (installation.InstallWebmin)
        {
            apps.Add("Webmin");
        }

        if (installation.InstallPostgreSql)
        {
            apps.Add("PostgreSQL");
        }

        if (installation.InstallMsSqlServer)
        {
            apps.Add("SQL Server");
        }

        if (installation.InstallMsSqlServer || installation.InstallSqlServerClient)
        {
            apps.Add("sqlcmd");
        }

        if (installation.InstallDotnetSdk)
        {
            apps.Add($".NET SDK {DotnetVersion}");
        }
        else if (installation.InstallDotnetRuntime)
        {
            apps.Add($"ASP.NET Core Runtime {DotnetVersion}");
        }

        return apps;
    }

    private static string CreateInterfaceYaml(ServerInstallationModel installation)
    {
        var sb = new StringBuilder();
        if (installation.IpV4Addresses.Count == 0)
        {
            sb.Append("dhcp4: true\n");
        }
        else
        {
            sb.Append("addresses:\n");
            foreach (string address in installation.IpV4Addresses)
            {
                sb.Append("- ").Append(address).Append('\n');
            }
        }

        if (installation.NameServers.Count > 0)
        {
            sb.Append("nameservers:\n  addresses:\n");
            foreach (string nameServer in installation.NameServers)
            {
                sb.Append("  - ").Append(nameServer).Append('\n');
            }

            sb.Append("  search: []\n");
        }

        if (!string.IsNullOrWhiteSpace(installation.Gateway))
        {
            sb.Append("routes:\n- to: default\n  via: ").Append(installation.Gateway).Append('\n');
        }

        return sb.ToString();
    }

    //late-command, რომელიც /target-ში install-apps-ს (და Virtualmin-ისთვის ვიზარდის პასუხებს) წერს
    private static string CreateLateCommandScript(ServerInstallationModel installation)
    {
        var sb = new StringBuilder();
        sb.Append("cat > /target/usr/local/sbin/install-apps <<'EOF'\n");
        sb.Append(CreateInstallAppsScript(installation));
        sb.Append("EOF\nchmod 700 /target/usr/local/sbin/install-apps\n");
        if (installation.InstallVirtualmin)
        {
            sb.Append("cat > /target/usr/local/sbin/virtualmin-wizard.pl <<'EOF'\n");
            sb.Append(CreateVirtualminWizardScript(installation));
            sb.Append("EOF\nchmod 700 /target/usr/local/sbin/virtualmin-wizard.pl\n");
        }

        return sb.ToString();
    }

    private static string CreateInstallAppsScript(ServerInstallationModel installation)
    {
        var sb = new StringBuilder();
        sb.Append("""
                  #!/bin/sh
                  set -e
                  # curl downloads the installers and repository files below

                  """);
        sb.Append(AptInstall("curl", "curl"));

        if (installation.InstallVirtualmin)
        {
            string postgreSql = installation.InstallPostgreSql
                ? " --extra postgresql,postgresql-client --include PostgreSQL"
                : string.Empty;
            sb.Append(CultureInfo.InvariantCulture, $$"""
                        # Skip the installer on a retry if it already finished
                        if [ ! -e /etc/webmin/virtual-server/installed-auto ]; then
                          script=$(curl -fsSL --retry 10 --retry-connrefused https://download.virtualmin.com/virtualmin-install)
                          sh -c "$script" -- --force --hostname {{GetFqdn(installation)}} \
                            --bundle {{GetBundleName(installation.Bundle)}}{{postgreSql}}
                        fi
                        if [ -e /usr/local/sbin/virtualmin-wizard.pl ]; then
                          perl /usr/local/sbin/virtualmin-wizard.pl
                          rm -f /usr/local/sbin/virtualmin-wizard.pl
                        fi

                        """);
            if (!string.IsNullOrWhiteSpace(installation.MariaDbRootPasswordHash))
            {
                sb.Append(CultureInfo.InvariantCulture, $$"""
                            # root keeps socket login (used by Webmin) and also gets a password,
                            # given here only as its mysql_native_password hash
                            mariadb -e "ALTER USER 'root'@'localhost' IDENTIFIED VIA unix_socket OR mysql_native_password USING '{{installation.MariaDbRootPasswordHash}}'"

                            """);
            }
        }
        else
        {
            if (installation.InstallWebmin)
            {
                sb.Append("""
                          if ! dpkg -s webmin >/dev/null 2>&1; then
                            curl -fsSL --retry 10 -o /tmp/webmin-setup-repo.sh \
                              https://raw.githubusercontent.com/webmin/webmin/master/webmin-setup-repo.sh
                            sh /tmp/webmin-setup-repo.sh --force
                            DEBIAN_FRONTEND=noninteractive apt-get install -y --install-recommends webmin
                          fi

                          """);
            }

            if (installation.InstallPostgreSql)
            {
                sb.Append(AptInstall("postgresql", "postgresql postgresql-client"));
            }
        }

        if (installation.InstallMsSqlServer || installation.InstallSqlServerClient)
        {
            sb.Append("""
                      if [ ! -e /usr/share/keyrings/microsoft-prod.gpg ]; then
                        curl -fsSL --retry 10 -o /tmp/microsoft.asc https://packages.microsoft.com/keys/microsoft.asc
                        gpg --batch --yes --dearmor -o /usr/share/keyrings/microsoft-prod.gpg /tmp/microsoft.asc
                      fi
                      # sqlcmd and bcp (the 26.04 repo is not signed with Microsoft's public key yet)
                      if [ ! -x /opt/mssql-tools18/bin/sqlcmd ]; then
                        curl -fsSL --retry 10 -o /etc/apt/sources.list.d/mssql-release.list \
                          https://packages.microsoft.com/config/ubuntu/24.04/prod.list
                        apt-get update
                        ACCEPT_EULA=Y DEBIAN_FRONTEND=noninteractive apt-get install -y mssql-tools18 unixodbc-dev
                        echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' > /etc/profile.d/mssql-tools18.sh
                      fi

                      """);
        }

        if (installation.InstallMsSqlServer)
        {
            sb.Append(CultureInfo.InvariantCulture, $$"""
                        # SQL Server 2025 (Microsoft only publishes its repo for Ubuntu 24.04).
                        # Setup gets a throwaway password, then sa is switched to the given hash.
                        if [ ! -e /var/lib/mssql-sa-hashed ]; then
                          curl -fsSL --retry 10 -o /etc/apt/sources.list.d/mssql-server-2025.list \
                            https://packages.microsoft.com/config/ubuntu/24.04/mssql-server-2025.list
                          apt-get update
                          DEBIAN_FRONTEND=noninteractive apt-get install -y mssql-server
                          # setup refuses to run while SQL Server is up (matters on a re-run)
                          systemctl stop mssql-server 2>/dev/null || true
                          tmp="$(openssl rand -hex 16)Aa1!"
                          ACCEPT_EULA=Y MSSQL_PID={{installation.SqlServerEdition}} MSSQL_SA_PASSWORD="$tmp" \
                            /opt/mssql/bin/mssql-conf -n setup
                          ok=
                          for i in $(seq 30); do
                            if /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$tmp" -C -b -Q \
                              "ALTER LOGIN sa WITH CHECK_POLICY = OFF; ALTER LOGIN sa WITH PASSWORD = {{installation.SqlServerSaPasswordHash}} HASHED"; then
                              ok=1; break
                            fi
                            sleep 2
                          done
                          [ -n "$ok" ]
                          touch /var/lib/mssql-sa-hashed
                        fi

                        """);

            //0 და ნაგულისხმევი პორტი არაფერს ცვლის: mssql-conf გამოყენებულ პორტს არ იღებს
            int sqlServerPort = installation.SqlServerPort == 0
                ? ServerInstallationModel.DefaultSqlServerPort
                : installation.SqlServerPort;
            string port = sqlServerPort.ToString(CultureInfo.InvariantCulture);
            if (sqlServerPort != ServerInstallationModel.DefaultSqlServerPort)
            {
                sb.Append(CultureInfo.InvariantCulture, $$"""
                            # SQL Server listens on port {{port}} (mssql-conf refuses a port in use, so not again)
                            if ! grep -Eq '^[[:space:]]*tcpport[[:space:]]*=[[:space:]]*{{port}}[[:space:]]*$' \
                              /var/opt/mssql/mssql.conf 2>/dev/null; then
                              /opt/mssql/bin/mssql-conf set network.tcpport {{port}}
                              systemctl restart mssql-server
                            fi

                            """);
            }

            if (installation.OpenSqlServerPort)
            {
                sb.Append(CultureInfo.InvariantCulture, $$"""
                            # Open SQL Server's port {{port}}: in firewalld if it runs (Virtualmin sets it up), in nftables otherwise
                            if command -v firewall-cmd >/dev/null 2>&1 && firewall-cmd --state >/dev/null 2>&1; then
                              firewall-cmd --permanent --add-port={{port}}/tcp
                              firewall-cmd --reload
                            else
                              if ! dpkg -s nftables >/dev/null 2>&1; then
                                apt-get update
                                DEBIAN_FRONTEND=noninteractive apt-get install -y nftables
                              fi
                              grep -q 'tcp dport {{port}} accept' /etc/nftables.conf ||
                                echo 'add rule inet filter input tcp dport {{port}} accept' >> /etc/nftables.conf
                              systemctl enable nftables
                              systemctl restart nftables
                            fi

                            """);
            }
        }

        //Ubuntu 26.04-ზე .NET Ubuntu-ს საკუთარ feed-შია, Microsoft-ის რეპოზიტორიაში (sqlcmd, SQL Server) კი
        //.NET-ის პაკეტები არ უნდა აირჩეს: ორი წყაროს პაკეტების შერევა .NET-ს აფუჭებს
        if (installation.InstallDotnetSdk || installation.InstallDotnetRuntime)
        {
            sb.Append("""
                      # .NET only from Ubuntu's feed, never from the Microsoft repository
                      printf 'Package: dotnet* aspnet* netstandard*\nPin: origin "packages.microsoft.com"\nPin-Priority: -10\n' \
                        > /etc/apt/preferences.d/dotnet-not-from-microsoft

                      """);
            string dotnetPackage = installation.InstallDotnetSdk
                ? $"dotnet-sdk-{DotnetVersion}"
                : $"aspnetcore-runtime-{DotnetVersion}";
            sb.Append(AptInstall(dotnetPackage, dotnetPackage));
        }

        sb.Append("echo \"All done.\"\n");
        return sb.ToString();
    }

    private static string AptInstall(string checkedPackage, string packages)
    {
        return $$"""
                 if ! dpkg -s {{checkedPackage}} >/dev/null 2>&1; then
                   apt-get update
                   DEBIAN_FRONTEND=noninteractive apt-get install -y {{packages}}
                 fi

                 """;
    }

    private static string CreateVirtualminWizardScript(ServerInstallationModel installation)
    {
        string prins = string.IsNullOrWhiteSpace(installation.DnsPrimaryNameServer)
            ? GetFqdn(installation)
            : installation.DnsPrimaryNameServer;
        string postgres = installation.InstallPostgreSql ? "1" : "0";
        string email = string.IsNullOrWhiteSpace(installation.EmailToAddress)
            ? "'to_addr_def' => 1"
            : $"'to_addr_def' => 0,\n                  'to_addr' => '{installation.EmailToAddress}'";
        return $$"""
                 #!/usr/bin/perl
                 # Answer Virtualmin's Post-Installation Wizard non-interactively
                 package virtual_server;
                 $main::no_acl_check++;
                 $ENV{'WEBMIN_CONFIG'} ||= "/etc/webmin";
                 $ENV{'WEBMIN_VAR'} ||= "/var/webmin";
                 chdir("/usr/share/webmin/virtual-server") || die "Virtualmin not found\n";
                 $0 = "/usr/share/webmin/virtual-server/wizard-auto.pl";
                 require './virtual-server-lib.pl';

                 my %answers = (
                     'memory' => { 'lookup' => 1 },
                     'spam'   => { 'spamd' => 0 },
                     'virus'  => { 'clamd' => 0 },
                     'db'     => { 'mysql' => 1, 'postgres' => {{postgres}} },
                     'mysql'  => { 'socket' => 1, 'mypass' => '' },
                     'dns'    => { 'prins' => '{{prins}}', 'prins_skip' => 1,
                                   'secns' => &get_template(0)->{'dns_ns'} },
                     'email'  => { 'from_addr_def' => 1, {{email}} },
                     );

                 # Walk the steps like wizard.cgi does, since answers can change the list
                 my @steps = &get_wizard_steps();
                 my $i = 0;
                 while (defined($i)) {
                     my $step = $steps[$i];
                     my $pfunc = "wizard_parse_".$step;
                     if (defined(&$pfunc)) {
                         my $err = &$pfunc($answers{$step} || { });
                         die "Wizard step $step failed: $err\n" if ($err);
                         }
                     $i = &get_wizard_next_step($step, \@steps);
                     @steps = &get_wizard_steps();
                     }
                 &save_module_config_keys({ 'wizard_run' => 1 });

                 """;
    }

    //virtualmin-install-ის --bundle-ის მნიშვნელობა
    private static string GetBundleName(EVirtualminBundle bundle)
    {
        return bundle == EVirtualminBundle.LampApache ? "LAMP" : "LEMP";
    }

    private static string Indent(string text, int spaces)
    {
        string prefix = new(' ', spaces);
        return string.Concat(text.ReplaceLineEndings("\n").Split('\n').SkipLast(1)
            .Select(x => x.Length == 0 ? "\n" : prefix + x + "\n"));
    }

    //YAML-ის ერთბრჭყალიანი სტრიქონი: შიგნით მხოლოდ ბრჭყალი ორმაგდება
    private static string Quote(string? value)
    {
        return "'" + (value ?? string.Empty).Replace("'", "''") + "'";
    }

    //IPNetwork აქ არ გამოდგება: ის ქსელის მისამართს ითხოვს, ჩვენ კი ჰოსტის მისამართი გვაქვს პრეფიქსით
    private static bool IsIpV4WithPrefix(string value)
    {
        string[] parts = value.Split('/');
        return parts.Length == 2 && IsIpV4(parts[0]) &&
               int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int prefix) &&
               prefix is >= 1 and <= 32;
    }

    //IPAddress.TryParse "192.168.10"-საც იღებს (192.168.0.10), ამიტომ მისამართი ზუსტად ისე უნდა იყოს ჩაწერილი,
    //როგორც მას ToString აბრუნებს
    private static bool IsIpV4(string value)
    {
        return IPAddress.TryParse(value, out IPAddress? address) &&
               address.AddressFamily == AddressFamily.InterNetwork && address.ToString() == value;
    }

    private static bool IsNameServer(string value)
    {
        return IsIpV4(value) || IPAddress.TryParse(value, out IPAddress? address) &&
            address.AddressFamily == AddressFamily.InterNetworkV6;
    }

    private static void CheckRequired(List<string> errors, string fieldName, string? value, Regex regex)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{fieldName} is required");
        }
        else
        {
            CheckOptional(errors, fieldName, value, regex);
        }
    }

    private static void CheckOptional(List<string> errors, string fieldName, string? value, Regex regex)
    {
        if (!string.IsNullOrWhiteSpace(value) && !regex.IsMatch(value))
        {
            errors.Add($"{fieldName} has an invalid value: {value}");
        }
    }

    //ბოლოში \z და არა $: $ ბოლო ხაზის გადატანის წინაც ემთხვევა და "oslo\n" გაივლიდა
    [GeneratedRegex(@"^[A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?\z")]
    private static partial Regex HostnameRegex();

    [GeneratedRegex(@"^[A-Za-z0-9]([A-Za-z0-9.-]*[A-Za-z0-9])?\z")]
    private static partial Regex DomainRegex();

    [GeneratedRegex(@"^[a-z_][a-z0-9_-]{0,31}\z")]
    private static partial Regex UserNameRegex();

    [GeneratedRegex(@"^\$6\$[^\s$']+\$[./0-9A-Za-z]+\z")]
    private static partial Regex LinuxHashRegex();

    [GeneratedRegex(@"^\*[0-9A-F]{40}\z")]
    private static partial Regex MariaDbHashRegex();

    [GeneratedRegex(@"^0x0200[0-9A-F]{136}\z")]
    private static partial Regex SqlServerHashRegex();

    [GeneratedRegex(@"^[A-Za-z0-9-]+\z")]
    private static partial Regex SqlServerEditionRegex();

    [GeneratedRegex(@"^[A-Za-z0-9._-]+\z")]
    private static partial Regex InterfaceNameRegex();

    [GeneratedRegex(@"^[A-Za-z0-9._/+-]+\z")]
    private static partial Regex SimpleValueRegex();

    [GeneratedRegex(@"^[^\s'""\\@]+@[^\s'""\\@]+\z")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[^\r\n]+\z")]
    private static partial Regex SingleLineRegex();
}

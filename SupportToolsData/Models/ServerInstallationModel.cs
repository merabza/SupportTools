using System.Collections.Generic;
using SystemTools.SystemToolsShared;

namespace SupportToolsData.Models;

//Ubuntu Server-ის ავტომატური ინსტალაციის პარამეტრები. მათგან გენერირდება საინსტალაციო ფლეშკის grub.cfg და
//autoinstall.yaml. პაროლები ღიად არ ინახება, მხოლოდ მათი ჰეშები
public sealed class ServerInstallationModel : ItemData
{
    public const int DefaultGrubTimeoutSeconds = 3;
    public const string DefaultTimezone = "Asia/Tbilisi";
    public const string DefaultLocale = "en_US.UTF-8";
    public const string DefaultKeyboardLayout = "us";
    public const string DefaultSqlServerEdition = "EnterpriseDeveloper";
    public const int DefaultSqlServerPort = 1433;

    //იდენტიფიკაცია
    public string? Hostname { get; set; }

    //დომენი. სრული სახელი (FQDN) არის Hostname.DomainName, ის Virtualmin-ს სჭირდება
    public string? DomainName { get; set; }

    public string? RealName { get; set; }
    public string? UserName { get; set; }

    //SHA-512 crypt ($6$...)
    public string? PasswordHash { get; set; }

    public List<string> SshAuthorizedKeys { get; init; } = [];
    public bool SshAllowPassword { get; set; }

    //ქსელი. თუ მისამართები არ არის მითითებული, ინტერფეისი მისამართს DHCP-ით იღებს
    public string? NetworkInterfaceName { get; set; }

    //მისამართები პრეფიქსით, მაგალითად 192.168.10.5/24
    public List<string> IpV4Addresses { get; init; } = [];

    public string? Gateway { get; set; }
    public List<string> NameServers { get; init; } = [];

    //სისტემა
    public string? Timezone { get; set; } = DefaultTimezone;
    public string? Locale { get; set; } = DefaultLocale;
    public string? KeyboardLayout { get; set; } = DefaultKeyboardLayout;
    public int GrubTimeoutSeconds { get; set; } = DefaultGrubTimeoutSeconds;

    //დისკი, რომელიც მთლიანად წაიშლება და რომელზეც სისტემა დადგება (lsblk -o NAME,SERIAL)
    public string? InstallDiskSerial { get; set; }

    //წინა ინსტალაციის /var/log/installer/autoinstall-user-data ფაილის ასლი. მისგან დისკის serial და ქსელის
    //ინტერფეისის სახელი იკითხება
    public string? AutoinstallUserDataFilePath { get; set; }

    //ფოლდერი, სადაც გენერირებული ფაილები იწერება. თუ ცარიელია, {WorkFolder}\ServerInstallations\{სახელი}
    public string? FilesFolder { get; set; }

    //პირველი ჩატვირთვის შემდეგ ხელით გასაშვები პროგრამები (~/install-apps.sh)
    public bool InstallWebmin { get; set; }
    public bool InstallVirtualmin { get; set; }

    //Virtualmin-ის ვებსერვერი: LAMP (Apache) ან LEMP (Nginx). MariaDB ორივე შემთხვევაში დგება
    public EVirtualminBundle Bundle { get; set; } = EVirtualminBundle.LempNginx;

    //Virtualmin-ის Post-Installation Wizard-ის პასუხები. თუ DnsPrimaryNameServer ცარიელია, FQDN გამოიყენება
    public string? DnsPrimaryNameServer { get; set; }
    public string? EmailToAddress { get; set; }

    //Virtualmin-ის მიერ დადგმული MariaDB-ის root-ის mysql_native_password ჰეში (*SHA1(SHA1))
    public string? MariaDbRootPasswordHash { get; set; }

    public bool InstallPostgreSql { get; set; }
    public bool InstallMsSqlServer { get; set; }
    public string? SqlServerEdition { get; set; } = DefaultSqlServerEdition;

    //SQL Server-ის sa-ს ჰეში (0x0200 + salt + SHA-512)
    public string? SqlServerSaPasswordHash { get; set; }

    public bool InstallSqlServerClient { get; set; }

    //SQL Server ამ პორტზე უსმენს. 0 ან 1433 (ნაგულისხმევი) ნიშნავს, რომ პორტი არ იცვლება
    public int SqlServerPort { get; set; }

    //SQL Server-ის პორტი firewall-ში იხსნება
    public bool OpenSqlServerPort { get; set; }

    //.NET SDK (Runtime-ს შეიცავს). თუ SDK არ დგება, შეიძლება მხოლოდ ASP.NET Core Runtime დადგეს
    public bool InstallDotnetSdk { get; set; }
    public bool InstallDotnetRuntime { get; set; }
}

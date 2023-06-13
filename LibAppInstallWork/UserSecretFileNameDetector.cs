using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace LibAppInstallWork;

public static class UserSecretFileNameDetector
{
    public static string? GetFileName(string csprojFileFullName)
    {
        //ამოვიღოთ წაკითხული XLM-დან შემდეგი მნიშვნელობა:
        var xmlCsproj = XElement.Load(csprojFileFullName);

        //  Project => PropertyGroup => UserSecretsId
        //.Descendants("Project")
        var xmlUserSecretsId = xmlCsproj.Descendants("PropertyGroup").Descendants("UserSecretsId")
            .SingleOrDefault();

        string? userSecretsId = null;
        if (xmlUserSecretsId != null)
            userSecretsId = xmlUserSecretsId.Value;

        //თუ ასეთი მნიშვნელობის ამოღება მოხერხდა, მაშინ
        if (userSecretsId != null)
            // ჩავტვირთოთ ობიექტად შედეგი json ფაილი
            //  %APPDATA%\microsoft\UserSecrets\<userSecretsId>\secrets.json
            //C:\Users\{UserName}\AppData\Roaming\Microsoft\UserSecrets

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "UserSecrets", userSecretsId, "secrets.json");

        return null;
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SupportTools.Menu;

//სოლუშენის ფაილში ჩაწერილი პროექტების გზები, ისე, როგორც ფაილშია (სოლუშენის ფოლდერის მიმართ).
//.slnx: ყველა Project ელემენტის Path (Folder-ების შიგნითაც); File ჩანაწერები (Solution Items) პროექტები არ არის.
//.sln: Project(...) სტრიქონები, solution folder-ების გარდა. ფაილის წაკითხვის შეცდომებს გამომძახებელი იჭერს
public static partial class SolutionFileReader
{
    //.sln-ში solution folder-იც Project(...) სტრიქონით იწერება, ამ ტიპით
    private const string SolutionFolderTypeGuid = "2150E333-8FDC-42A3-9474-1A3956D46DE8";

    public static bool IsSolutionFile(string fileName)
    {
        string extension = Path.GetExtension(fileName);
        return extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
    }

    public static List<string> ReadProjectPaths(string solutionFileName)
    {
        if (Path.GetExtension(solutionFileName).Equals(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                .. XDocument.Load(solutionFileName).Descendants().Where(x => x.Name.LocalName == "Project")
                    .Select(x => (string?)x.Attribute("Path")).OfType<string>()
            ];
        }

        return
        [
            .. File.ReadLines(solutionFileName).Select(line => SlnProjectLineRegex().Match(line))
                .Where(match => match.Success && !match.Groups["type"].Value.Equals(SolutionFolderTypeGuid,
                    StringComparison.OrdinalIgnoreCase)).Select(match => match.Groups["path"].Value)
        ];
    }

    //Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Name", "Name\Name.csproj", "{...}"
    [GeneratedRegex(@"^Project\(""\{(?<type>[^}]+)\}""\)\s*=\s*""[^""]*""\s*,\s*""(?<path>[^""]*)""")]
    private static partial Regex SlnProjectLineRegex();
}

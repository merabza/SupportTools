using System;
using System.Linq;

namespace LibDotnetWork;

public static class NugetServerUrls
{
    //nuget-ის service index-ის მისამართი, გამოიყენება პაკეტების ასატვირთად
    //სტრიქონი ბრუნდება იმიტომ, რომ მისამართი dotnet-ის ბრძანების არგუმენტად გამოიყენება
#pragma warning disable CA1055
    public static string GetServiceIndexUrl(string server)
#pragma warning restore CA1055
    {
        return NormalizeBaseUrl(server) + "/v3/index.json";
    }

    //package manager-ის მისამართი შეიძლება ბოლოვდებოდეს /api/v1-ის მსგავსი სუფიქსით,
    //nuget-ის მისამართების ასაგებად კი საჭიროა საბაზისო მისამართი
    private static string NormalizeBaseUrl(string server)
    {
        string baseUrl = server.TrimEnd('/');
        int lastSlashIndex = baseUrl.LastIndexOf('/');
        if (lastSlashIndex <= 0)
        {
            return baseUrl;
        }

        string lastSegment = baseUrl[(lastSlashIndex + 1)..];
        string beforeLastSegment = baseUrl[..lastSlashIndex];
        if (lastSegment.Length > 1 && (lastSegment[0] == 'v' || lastSegment[0] == 'V') &&
            lastSegment[1..].All(char.IsAsciiDigit) &&
            beforeLastSegment.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = beforeLastSegment[..^4];
        }

        return baseUrl;
    }
}

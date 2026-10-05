using System.Collections.Generic;
using LibSupportToolsServerWork.Registry.Paths;
using LibSupportToolsServerWork.Registry.Sync;
using Newtonsoft.Json;
using SupportToolsData.Models;

namespace SupportTools.Tests.Registry.Mappers;

internal static class MapperTestHelpers
{
    public const string CanonicalWorkFolder = @"D:\1WorkDotnet";
    public const string LinuxWorkFolder = "/home/u/1WorkDotnet";

    //all the fields of a local model, to compare a model with its round-tripped copy
    public static string JsonOf(object value)
    {
        return JsonConvert.SerializeObject(value);
    }

    public static string HashOf(object contract)
    {
        return RegistryContractHasher.ComputeHash(contract);
    }

    //a Windows computer with the layout of the main computer: no rules, nothing changes
    public static PathMapper WindowsPathMapper()
    {
        return new PathMapper([], '\\');
    }

    //a Linux computer: D:\1WorkDotnet is /home/u/1WorkDotnet
    public static PathMapper LinuxPathMapper()
    {
        List<PathMappingModel> pathMappings =
        [
            new() { CanonicalPrefix = CanonicalWorkFolder, LocalPrefix = LinuxWorkFolder }
        ];
        return new PathMapper(pathMappings, '/');
    }
}

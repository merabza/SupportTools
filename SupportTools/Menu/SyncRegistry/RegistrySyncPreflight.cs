using System;
using System.Collections.Generic;
using System.Linq;
using LibSupportToolsServerWork.Registry.Adapters;
using SupportToolsData.Models;

namespace SupportTools.Menu.SyncRegistry;

//სინქრონიზაციის წინასწარი შემოწმება (C5): ლოკალური გასაღებები, რომლებიც მხოლოდ რეგისტრით განსხვავდება (G8). ასეთი
//ჩანაწერებისთვის ვერ დგინდება, რომელს შეესაბამება სერვერის ჩანაწერი, ამიტომ ბრძანება სინქრონიზაციაზე უარს ამბობს,
//სანამ მომხმარებელი მათ არ გაასწორებს. შემოწმება ყველა კოლექციას ეხება, Projects-საც (მომხმარებლის გადაწყვეტილება);
//Projects-ის ადაპტერი ასეთ პროექტებს გაფრთხილებით გამორიცხავს (C4), რაც ავტომატური სინქრონიზაციისთვის (D2) რჩება.
//singleton-ებს (GlobalSettings, ProjectCreatorSettings) გასაღები არ აქვს. საიდუმლო ფაილების (StoredFiles, C6) გასაღებები
//გზებია: მხოლოდ რეგისტრით განსხვავებული გზები Windows-ზე ერთი ფაილია და ადაპტერი მათ აერთიანებს
internal static class RegistrySyncPreflight
{
    //თითო ჯგუფზე ერთი სტრიქონი: "კოლექცია: გასაღები1 / გასაღები2"
    public static List<string> FindKeysDifferingOnlyByCase(SupportToolsParameters parameters)
    {
        return
        [
            .. GetLocalKeys(parameters).SelectMany(collection => collection.Keys
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1)
                .Select(x => $"{collection.CollectionName}: {string.Join(" / ", x)}"))
        ];
    }

    //ლოკალური კოლექციების გასაღებები, ადაპტერების რიგით (RegistryCollections). ახალი კოლექციის ადაპტერი აქაც უნდა
    //დაემატოს; ამას ტესტი ამოწმებს
    internal static List<(string CollectionName, IEnumerable<string> Keys)> GetLocalKeys(
        SupportToolsParameters parameters)
    {
        return
        [
            (RegistryCollections.Environments, parameters.Environments.Keys),
            (RegistryCollections.RunTimes, parameters.RunTimes.Keys),
            (RegistryCollections.NpmPackages, parameters.NpmPackages.Keys),
            (RegistryCollections.ReactAppTemplates, parameters.ReactAppTemplates.Keys),
            (RegistryCollections.DotnetTools, parameters.DotnetTools.Keys),
            (RegistryCollections.SmartSchemas, parameters.SmartSchemas.Keys),
            (RegistryCollections.FileStorages, parameters.FileStorages.Keys),
            (RegistryCollections.ApiClients, parameters.ApiClients.Keys),
            (RegistryCollections.DatabaseServerConnections, parameters.DatabaseServerConnections.Keys),
            (RegistryCollections.Servers, parameters.Servers.Keys),
            (RegistryCollections.GitIgnorePatterns, parameters.GitIgnorePatterns),
            (RegistryCollections.Gits, parameters.Gits.Keys),
            (RegistryCollections.EditorConfigPatterns, parameters.EditorConfigPatterns),
            (RegistryCollections.ProjectTemplates, GetProjectTemplateNames(parameters)),
            (RegistryCollections.Projects, parameters.Projects.Keys)
        ];
    }

    private static List<string> GetProjectTemplateNames(SupportToolsParameters parameters)
    {
        return parameters.AppProjectCreatorAllParameters is null
            ? []
            : [.. parameters.AppProjectCreatorAllParameters.Templates.Keys];
    }
}

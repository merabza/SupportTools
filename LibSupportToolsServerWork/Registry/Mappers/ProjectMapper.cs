using System;
using System.Collections.Generic;
using System.Linq;
using LibSupportToolsServerWork.Registry.Paths;
using SupportToolsData;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Mappers;

//პროექტი (B6), ServerInfo-ებით (B7) ერთ აგრეგატად (G7): ProjectModel ↔ StsProjectDataModel. dictionary-ის key
//კონტრაქტის Name-ია. 15 გზის ველი PathMapper-ით გარდაიქმნება (README §4.4). KeyGuidPart საიდუმლოა (G2): გადაიცემა,
//მაგრამ არსად იბეჭდება. ProjectType, AllowToolsList და endpoint-ების HttpMethod/EndpointType enum-ების სახელებია.
//RouteClassModel.Version კონტრაქტის ApiVersion-ია. ScaffoldSeederGitProjectNames ისე გადადის, როგორც არის.
//ProjectModel-ის ყველა ველი კონტრაქტშია; მომავალში დამატებული, ჯერ ლოკალური ველი (README §4.6) ApplyToLocal-მა
//უცვლელად უნდა დატოვოს: ადგილზე განახლებისას ის ისედაც რჩება, ახალი ობიექტისას CreateWithInitOnlyValues-მა უნდა
//გადაიტანოს
public static class ProjectMapper
{
    public static StsProjectDataModel ToContract(string name, ProjectModel project, PathMapper pathMapper)
    {
        return new StsProjectDataModel
        {
            Name = name,
            ProjectType = project.ProjectType.ToString(),
            ProjectGroupName = project.ProjectGroupName,
            ProjectDescription = project.ProjectDescription,
            MajorVersion = project.MajorVersion,
            MinorVersion = project.MinorVersion,
            UseAlternativeWebAgent = project.UseAlternativeWebAgent,
            EditorConfigPatternName = project.EditorConfigPatternName,
            MainProjectName = project.MainProjectName,
            ApiContractsProjectName = project.ApiContractsProjectName,
            SpaProjectName = project.SpaProjectName,
            DbContextName = project.DbContextName,
            ProjectShortPrefix = project.ProjectShortPrefix,
            ScaffoldSeederProjectName = project.ScaffoldSeederProjectName,
            DbContextProjectName = project.DbContextProjectName,
            NewDataSeedingClassLibProjectName = project.NewDataSeedingClassLibProjectName,
            ProgramArchiveDateMask = project.ProgramArchiveDateMask,
            ProgramArchiveExtension = project.ProgramArchiveExtension,
            ParametersFileDateMask = project.ParametersFileDateMask,
            ParametersFileExtension = project.ParametersFileExtension,
            ProjectFolderName = pathMapper.ToCanonical(project.ProjectFolderName),
            SolutionFileName = pathMapper.ToCanonical(project.SolutionFileName),
            ProjectSecurityFolderPath = pathMapper.ToCanonical(project.ProjectSecurityFolderPath),
            MigrationStartupProjectFilePath = pathMapper.ToCanonical(project.MigrationStartupProjectFilePath),
            MigrationProjectFilePath = pathMapper.ToCanonical(project.MigrationProjectFilePath),
            DataSeederRulesByTableStartupProjectFilePath =
                pathMapper.ToCanonical(project.DataSeederRulesByTableStartupProjectFilePath),
            OldDataConvertorForDataSeeder = pathMapper.ToCanonical(project.OldDataConvertorForDataSeeder),
            SeedProjectFilePath = pathMapper.ToCanonical(project.SeedProjectFilePath),
            SeedProjectParametersFilePath = pathMapper.ToCanonical(project.SeedProjectParametersFilePath),
            ExcludesRulesParametersFilePath = pathMapper.ToCanonical(project.ExcludesRulesParametersFilePath),
            AppSetEnKeysJsonFileName = pathMapper.ToCanonical(project.AppSetEnKeysJsonFileName),
            MigrationSqlFilesFolder = pathMapper.ToCanonical(project.MigrationSqlFilesFolder),
            PrepareProdCopyDatabaseProjectFilePath =
                pathMapper.ToCanonical(project.PrepareProdCopyDatabaseProjectFilePath),
            PrepareProdCopyDatabaseProjectParametersFilePath =
                pathMapper.ToCanonical(project.PrepareProdCopyDatabaseProjectParametersFilePath),
            PairedDbObjectsResultFileName = pathMapper.ToCanonical(project.PairedDbObjectsResultFileName),
            KeyGuidPart = project.KeyGuidPart,
            DevDatabaseParameters = DatabaseParametersMapper.ToContract(project.DevDatabaseParameters),
            ProdCopyDatabaseParameters = DatabaseParametersMapper.ToContract(project.ProdCopyDatabaseParameters),
            GitProjectNames = [.. project.GitProjectNames],
            ScaffoldSeederGitProjectNames = [.. project.ScaffoldSeederGitProjectNames],
            FrontNpmPackageNames = [.. project.FrontNpmPackageNames],
            RedundantFileNames = [.. project.RedundantFileNames],
            AllowToolsList = [.. project.AllowToolsList.Select(x => x.ToString())],
            Endpoints =
            [
                .. project.Endpoints.Select(x => new StsProjectEndpointDataModel
                {
                    Name = x.Key,
                    EndpointName = x.Value.EndpointName,
                    EndpointRoute = x.Value.EndpointRoute,
                    RequireAuthorization = x.Value.RequireAuthorization,
                    HttpMethod = x.Value.HttpMethod.ToString(),
                    EndpointType = x.Value.EndpointType.ToString(),
                    ReturnType = x.Value.ReturnType,
                    SendMessageToCurrentUser = x.Value.SendMessageToCurrentUser
                })
            ],
            RouteClasses =
            [
                .. project.RouteClasses.Select(x => new StsProjectRouteClassDataModel
                {
                    Name = x.Key, Root = x.Value.Root, ApiVersion = x.Value.Version, Base = x.Value.Base
                })
            ],
            ServerInfos = [.. project.ServerInfos.Values.Select(x => ServerInfoMapper.ToContract(x, pathMapper))]
        };
    }

    //არსებული პროექტი ადგილზე ახლდება, თუ კონტრაქტი მის init-only ველებს არ ცვლის. თუ ცვლის, იქმნება ახალი ობიექტი,
    //რომელიც არსებულის სიებისა და dictionary-ების ეგზემპლარებს იღებს, და ის dictionary-ში ძველს ანაცვლებს. ამ დროს
    //ღია რედაქტორი ძველ ობიექტს ხედავს, მაგრამ pull მენიუს ხელახალ აგებამდე ხდება (C5). enum-ების სახელები ამ
    //კლიენტისთვის ცნობილი უნდა იყოს (ადაპტერი უცნობს წინასწარ გამორიცხავს)
    public static ProjectModel ToLocal(StsProjectDataModel contract, ProjectModel? existing, PathMapper pathMapper)
    {
        ProjectModel candidate = CreateWithInitOnlyValues(contract, existing, pathMapper);
        ProjectModel project = existing is not null && HaveSameInitOnlyValues(existing, candidate)
            ? existing
            : candidate;

        project.ProjectDescription = contract.ProjectDescription;
        project.MajorVersion = contract.MajorVersion;
        project.MinorVersion = contract.MinorVersion;
        project.EditorConfigPatternName = contract.EditorConfigPatternName;
        project.SpaProjectName = contract.SpaProjectName;
        project.ScaffoldSeederProjectName = contract.ScaffoldSeederProjectName;
        project.ProgramArchiveDateMask = contract.ProgramArchiveDateMask;
        project.ProgramArchiveExtension = contract.ProgramArchiveExtension;
        project.ParametersFileDateMask = contract.ParametersFileDateMask;
        project.ParametersFileExtension = contract.ParametersFileExtension;
        project.MigrationStartupProjectFilePath = pathMapper.ToLocal(contract.MigrationStartupProjectFilePath);
        project.MigrationProjectFilePath = pathMapper.ToLocal(contract.MigrationProjectFilePath);
        project.DataSeederRulesByTableStartupProjectFilePath =
            pathMapper.ToLocal(contract.DataSeederRulesByTableStartupProjectFilePath);
        project.OldDataConvertorForDataSeeder = pathMapper.ToLocal(contract.OldDataConvertorForDataSeeder);
        project.SeedProjectFilePath = pathMapper.ToLocal(contract.SeedProjectFilePath);
        project.SeedProjectParametersFilePath = pathMapper.ToLocal(contract.SeedProjectParametersFilePath);
        project.MigrationSqlFilesFolder = pathMapper.ToLocal(contract.MigrationSqlFilesFolder);
        project.PrepareProdCopyDatabaseProjectFilePath =
            pathMapper.ToLocal(contract.PrepareProdCopyDatabaseProjectFilePath);
        project.PrepareProdCopyDatabaseProjectParametersFilePath =
            pathMapper.ToLocal(contract.PrepareProdCopyDatabaseProjectParametersFilePath);
        project.PairedDbObjectsResultFileName = pathMapper.ToLocal(contract.PairedDbObjectsResultFileName);

        Refill(project.GitProjectNames, contract.GitProjectNames);
        Refill(project.ScaffoldSeederGitProjectNames, contract.ScaffoldSeederGitProjectNames);
        Refill(project.FrontNpmPackageNames, contract.FrontNpmPackageNames);
        Refill(project.RedundantFileNames, contract.RedundantFileNames);
        Refill(project.AllowToolsList, contract.AllowToolsList.Select(x => Enum.Parse<EProjectTools>(x, true)));
        ApplyEndpoints(contract.Endpoints, project.Endpoints);
        ApplyRouteClasses(contract.RouteClasses, project.RouteClasses);
        ServerInfoMapper.ApplyToLocal(contract.ServerInfos, project.ServerInfos, pathMapper);
        return project;
    }

    public static StsProjectDataModel Normalize(StsProjectDataModel contract)
    {
        contract.ProjectType = ContractNormalization.EnumName<EProjectType>(contract.ProjectType);
        contract.ProjectGroupName = ContractNormalization.EmptyToNull(contract.ProjectGroupName);
        contract.ProjectDescription = ContractNormalization.EmptyToNull(contract.ProjectDescription);
        contract.EditorConfigPatternName = ContractNormalization.EmptyToNull(contract.EditorConfigPatternName);
        contract.MainProjectName = ContractNormalization.EmptyToNull(contract.MainProjectName);
        contract.ApiContractsProjectName = ContractNormalization.EmptyToNull(contract.ApiContractsProjectName);
        contract.SpaProjectName = ContractNormalization.EmptyToNull(contract.SpaProjectName);
        contract.DbContextName = ContractNormalization.EmptyToNull(contract.DbContextName);
        contract.ProjectShortPrefix = ContractNormalization.EmptyToNull(contract.ProjectShortPrefix);
        contract.ScaffoldSeederProjectName = ContractNormalization.EmptyToNull(contract.ScaffoldSeederProjectName);
        contract.DbContextProjectName = ContractNormalization.EmptyToNull(contract.DbContextProjectName);
        contract.NewDataSeedingClassLibProjectName =
            ContractNormalization.EmptyToNull(contract.NewDataSeedingClassLibProjectName);
        contract.ProgramArchiveDateMask = ContractNormalization.EmptyToNull(contract.ProgramArchiveDateMask);
        contract.ProgramArchiveExtension = ContractNormalization.EmptyToNull(contract.ProgramArchiveExtension);
        contract.ParametersFileDateMask = ContractNormalization.EmptyToNull(contract.ParametersFileDateMask);
        contract.ParametersFileExtension = ContractNormalization.EmptyToNull(contract.ParametersFileExtension);
        contract.ProjectFolderName = NormalizePath(contract.ProjectFolderName);
        contract.SolutionFileName = NormalizePath(contract.SolutionFileName);
        contract.ProjectSecurityFolderPath = NormalizePath(contract.ProjectSecurityFolderPath);
        contract.MigrationStartupProjectFilePath = NormalizePath(contract.MigrationStartupProjectFilePath);
        contract.MigrationProjectFilePath = NormalizePath(contract.MigrationProjectFilePath);
        contract.DataSeederRulesByTableStartupProjectFilePath =
            NormalizePath(contract.DataSeederRulesByTableStartupProjectFilePath);
        contract.OldDataConvertorForDataSeeder = NormalizePath(contract.OldDataConvertorForDataSeeder);
        contract.SeedProjectFilePath = NormalizePath(contract.SeedProjectFilePath);
        contract.SeedProjectParametersFilePath = NormalizePath(contract.SeedProjectParametersFilePath);
        contract.ExcludesRulesParametersFilePath = NormalizePath(contract.ExcludesRulesParametersFilePath);
        contract.AppSetEnKeysJsonFileName = NormalizePath(contract.AppSetEnKeysJsonFileName);
        contract.MigrationSqlFilesFolder = NormalizePath(contract.MigrationSqlFilesFolder);
        contract.PrepareProdCopyDatabaseProjectFilePath =
            NormalizePath(contract.PrepareProdCopyDatabaseProjectFilePath);
        contract.PrepareProdCopyDatabaseProjectParametersFilePath =
            NormalizePath(contract.PrepareProdCopyDatabaseProjectParametersFilePath);
        contract.PairedDbObjectsResultFileName = NormalizePath(contract.PairedDbObjectsResultFileName);
        contract.KeyGuidPart = ContractNormalization.EmptyToNull(contract.KeyGuidPart);
        contract.DevDatabaseParameters = DatabaseParametersMapper.Normalize(contract.DevDatabaseParameters);
        contract.ProdCopyDatabaseParameters = DatabaseParametersMapper.Normalize(contract.ProdCopyDatabaseParameters);
        contract.GitProjectNames = ContractNormalization.OrderByName(contract.GitProjectNames, x => x);
        contract.ScaffoldSeederGitProjectNames =
            ContractNormalization.OrderByName(contract.ScaffoldSeederGitProjectNames, x => x);
        contract.FrontNpmPackageNames = ContractNormalization.OrderByName(contract.FrontNpmPackageNames, x => x);
        contract.RedundantFileNames = ContractNormalization.OrderByName(contract.RedundantFileNames, x => x);
        contract.AllowToolsList = ContractNormalization.OrderByName(
            contract.AllowToolsList.Select(ContractNormalization.EnumName<EProjectTools>), x => x);

        foreach (StsProjectEndpointDataModel endpoint in contract.Endpoints)
        {
            endpoint.EndpointName = ContractNormalization.EmptyToNull(endpoint.EndpointName);
            endpoint.EndpointRoute = ContractNormalization.EmptyToNull(endpoint.EndpointRoute);
            endpoint.HttpMethod = ContractNormalization.EnumName<EHttpMethod>(endpoint.HttpMethod);
            endpoint.EndpointType = ContractNormalization.EnumName<EEndpointType>(endpoint.EndpointType);
            endpoint.ReturnType = ContractNormalization.EmptyToNull(endpoint.ReturnType);
        }

        contract.Endpoints = ContractNormalization.OrderByName(contract.Endpoints, x => x.Name);

        foreach (StsProjectRouteClassDataModel routeClass in contract.RouteClasses)
        {
            routeClass.Root = ContractNormalization.EmptyToNull(routeClass.Root);
            routeClass.ApiVersion = ContractNormalization.EmptyToNull(routeClass.ApiVersion);
            routeClass.Base = ContractNormalization.EmptyToNull(routeClass.Base);
        }

        contract.RouteClasses = ContractNormalization.OrderByName(contract.RouteClasses, x => x.Name);

        //სერვერის რიგი: ServerName, შემდეგ EnvironmentName (რეგისტრის გარეშე, ტოლობისას Ordinal)
        contract.ServerInfos =
        [
            .. contract.ServerInfos.Select(ServerInfoMapper.Normalize)
                .OrderBy(x => x.ServerName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.EnvironmentName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.ServerName, StringComparer.Ordinal)
                .ThenBy(x => x.EnvironmentName, StringComparer.Ordinal)
        ];
        return contract;
    }

    //ამ კლიენტისთვის უცნობი enum-ის ველის სახელი; null — ასეთი ველი არ არის
    public static string? FindUnknownEnumField(StsProjectDataModel contract)
    {
        if (!ContractNormalization.IsEnumName<EProjectType>(contract.ProjectType))
        {
            return nameof(StsProjectDataModel.ProjectType);
        }

        if (!contract.AllowToolsList.All(ContractNormalization.IsEnumName<EProjectTools>))
        {
            return nameof(StsProjectDataModel.AllowToolsList);
        }

        StsProjectEndpointDataModel? endpoint = contract.Endpoints.Find(x =>
            !ContractNormalization.IsEnumName<EHttpMethod>(x.HttpMethod) ||
            !ContractNormalization.IsEnumName<EEndpointType>(x.EndpointType));
        if (endpoint is not null)
        {
            return $"{nameof(StsProjectDataModel.Endpoints)}.{endpoint.Name}";
        }

        string? field = DatabaseParametersMapper.FindUnknownEnumField(contract.DevDatabaseParameters);
        if (field is not null)
        {
            return $"{nameof(StsProjectDataModel.DevDatabaseParameters)}.{field}";
        }

        field = DatabaseParametersMapper.FindUnknownEnumField(contract.ProdCopyDatabaseParameters);
        if (field is not null)
        {
            return $"{nameof(StsProjectDataModel.ProdCopyDatabaseParameters)}.{field}";
        }

        return contract.ServerInfos.Select(ServerInfoMapper.FindUnknownEnumField).OfType<string>()
            .Select(x => $"{nameof(StsProjectDataModel.ServerInfos)}.{x}").FirstOrDefault();
    }

    //სიმრავლეების გამეორებული ელემენტები და გასაღებები, რეგისტრის გარეშე (მათ შორის ის, რაც მხოლოდ რეგისტრით
    //განსხვავდება): სერვერი ასეთ პროექტს უარყოფს, ლოკალური ServerInfo-ს ნატურალური გასაღების გამეორება კი ვერ
    //გაირკვევა, რომელი ჩანაწერია სწორი. აბრუნებს აღწერებს "<ველი>: <მნიშვნელობები>"; ცარიელი სია — პრობლემა არ არის
    public static List<string> FindRepeatedValues(StsProjectDataModel contract)
    {
        List<string> problems = [];
        AddRepeated(problems, nameof(StsProjectDataModel.GitProjectNames), contract.GitProjectNames);
        AddRepeated(problems, nameof(StsProjectDataModel.ScaffoldSeederGitProjectNames),
            contract.ScaffoldSeederGitProjectNames);
        AddRepeated(problems, nameof(StsProjectDataModel.FrontNpmPackageNames), contract.FrontNpmPackageNames);
        AddRepeated(problems, nameof(StsProjectDataModel.RedundantFileNames), contract.RedundantFileNames);
        AddRepeated(problems, nameof(StsProjectDataModel.AllowToolsList), contract.AllowToolsList);
        AddRepeated(problems, nameof(StsProjectDataModel.Endpoints), contract.Endpoints.Select(x => x.Name));
        AddRepeated(problems, nameof(StsProjectDataModel.RouteClasses), contract.RouteClasses.Select(x => x.Name));
        AddRepeated(problems, nameof(StsProjectDataModel.ServerInfos),
            contract.ServerInfos.Select(x => ServerInfoMapper.NaturalKey(x.ServerName, x.EnvironmentName)));
        foreach (StsServerInfoDataModel serverInfo in contract.ServerInfos)
        {
            string naturalKey = ServerInfoMapper.NaturalKey(serverInfo.ServerName, serverInfo.EnvironmentName);
            AddRepeated(problems,
                $"{nameof(StsProjectDataModel.ServerInfos)}.{naturalKey}.{nameof(StsServerInfoDataModel.AllowToolsList)}",
                serverInfo.AllowToolsList);
        }

        return problems;
    }

    private static void AddRepeated(List<string> problems, string field, IEnumerable<string> values)
    {
        List<string> repeated =
        [
            .. values.GroupBy(x => x, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1)
                .Select(x => string.Join('/', x))
        ];
        if (repeated.Count > 0)
        {
            problems.Add($"{field}: {string.Join(", ", repeated)}");
        }
    }

    private static string? NormalizePath(string? path)
    {
        return ContractNormalization.UpperDriveLetter(ContractNormalization.EmptyToNull(path));
    }

    //init-only ველები კონტრაქტიდან; სიები, dictionary-ები და ბაზის პარამეტრების ეგზემპლარები არსებულისაა, თუ ის არის.
    //DatabaseParametersMapper.ToLocal არსებულ ბაზის პარამეტრებს აქვე, ადგილზე ანახლებს, ამიტომ ისინი განახლებულია
    //როგორც არსებულ პროექტში, ისე ახალ ობიექტში
    private static ProjectModel CreateWithInitOnlyValues(StsProjectDataModel contract, ProjectModel? existing,
        PathMapper pathMapper)
    {
        return new ProjectModel
        {
            ProjectType = Enum.Parse<EProjectType>(contract.ProjectType, true),
            ProjectGroupName = contract.ProjectGroupName,
            UseAlternativeWebAgent = contract.UseAlternativeWebAgent,
            ProjectFolderName = pathMapper.ToLocal(contract.ProjectFolderName),
            SolutionFileName = pathMapper.ToLocal(contract.SolutionFileName),
            ProjectSecurityFolderPath = pathMapper.ToLocal(contract.ProjectSecurityFolderPath),
            MainProjectName = contract.MainProjectName,
            ApiContractsProjectName = contract.ApiContractsProjectName,
            DbContextName = contract.DbContextName,
            ProjectShortPrefix = contract.ProjectShortPrefix,
            DbContextProjectName = contract.DbContextProjectName,
            NewDataSeedingClassLibProjectName = contract.NewDataSeedingClassLibProjectName,
            ExcludesRulesParametersFilePath = pathMapper.ToLocal(contract.ExcludesRulesParametersFilePath),
            AppSetEnKeysJsonFileName = pathMapper.ToLocal(contract.AppSetEnKeysJsonFileName),
            KeyGuidPart = contract.KeyGuidPart,
            DevDatabaseParameters =
                DatabaseParametersMapper.ToLocal(contract.DevDatabaseParameters, existing?.DevDatabaseParameters),
            ProdCopyDatabaseParameters =
                DatabaseParametersMapper.ToLocal(contract.ProdCopyDatabaseParameters,
                    existing?.ProdCopyDatabaseParameters),
            RedundantFileNames = existing?.RedundantFileNames ?? [],
            FrontNpmPackageNames = existing?.FrontNpmPackageNames ?? [],
            Endpoints = existing?.Endpoints ?? [],
            RouteClasses = existing?.RouteClasses ?? [],
            GitProjectNames = existing?.GitProjectNames ?? [],
            ScaffoldSeederGitProjectNames = existing?.ScaffoldSeederGitProjectNames ?? [],
            ServerInfos = existing?.ServerInfos ?? [],
            AllowToolsList = existing?.AllowToolsList ?? []
        };
    }

    //სიები და dictionary-ები candidate-ს არსებულისგან აქვს, ამიტომ მხოლოდ სკალარები და ბაზის პარამეტრების არსებობა
    //შედარდება
    private static bool HaveSameInitOnlyValues(ProjectModel existing, ProjectModel candidate)
    {
        return existing.ProjectType == candidate.ProjectType &&
               existing.UseAlternativeWebAgent == candidate.UseAlternativeWebAgent &&
               existing.DevDatabaseParameters is null == candidate.DevDatabaseParameters is null &&
               existing.ProdCopyDatabaseParameters is null == candidate.ProdCopyDatabaseParameters is null && new[]
               {
                   (existing.ProjectGroupName, candidate.ProjectGroupName),
                   (existing.ProjectFolderName, candidate.ProjectFolderName),
                   (existing.SolutionFileName, candidate.SolutionFileName),
                   (existing.ProjectSecurityFolderPath, candidate.ProjectSecurityFolderPath),
                   (existing.MainProjectName, candidate.MainProjectName),
                   (existing.ApiContractsProjectName, candidate.ApiContractsProjectName),
                   (existing.DbContextName, candidate.DbContextName),
                   (existing.ProjectShortPrefix, candidate.ProjectShortPrefix),
                   (existing.DbContextProjectName, candidate.DbContextProjectName),
                   (existing.NewDataSeedingClassLibProjectName, candidate.NewDataSeedingClassLibProjectName),
                   (existing.ExcludesRulesParametersFilePath, candidate.ExcludesRulesParametersFilePath),
                   (existing.AppSetEnKeysJsonFileName, candidate.AppSetEnKeysJsonFileName),
                   (existing.KeyGuidPart, candidate.KeyGuidPart)
               }.All(x => string.Equals(x.Item1, x.Item2, StringComparison.Ordinal));
    }

    private static void Refill<T>(List<T> list, IEnumerable<T> values)
    {
        list.Clear();
        list.AddRange(values);
    }

    //endpoint-ები მთლიანად კონტრაქტისაა (G7): dictionary-ის ეგზემპლარი და არსებული ჩანაწერის ობიექტი რჩება
    private static void ApplyEndpoints(List<StsProjectEndpointDataModel> contracts,
        Dictionary<string, EndpointModel> endpoints)
    {
        Dictionary<string, EndpointModel> existing = new(endpoints, endpoints.Comparer);
        endpoints.Clear();
        foreach (StsProjectEndpointDataModel contract in contracts)
        {
            EndpointModel endpoint = existing.GetValueOrDefault(contract.Name) ?? new EndpointModel();
            endpoint.EndpointName = contract.EndpointName ?? string.Empty;
            endpoint.EndpointRoute = contract.EndpointRoute ?? string.Empty;
            endpoint.RequireAuthorization = contract.RequireAuthorization;
            endpoint.HttpMethod = Enum.Parse<EHttpMethod>(contract.HttpMethod, true);
            endpoint.EndpointType = Enum.Parse<EEndpointType>(contract.EndpointType, true);
            endpoint.ReturnType = contract.ReturnType ?? string.Empty;
            endpoint.SendMessageToCurrentUser = contract.SendMessageToCurrentUser;
            endpoints[contract.Name] = endpoint;
        }
    }

    private static void ApplyRouteClasses(List<StsProjectRouteClassDataModel> contracts,
        Dictionary<string, RouteClassModel> routeClasses)
    {
        Dictionary<string, RouteClassModel> existing = new(routeClasses, routeClasses.Comparer);
        routeClasses.Clear();
        foreach (StsProjectRouteClassDataModel contract in contracts)
        {
            RouteClassModel routeClass = existing.GetValueOrDefault(contract.Name) ?? new RouteClassModel();
            routeClass.Root = contract.Root ?? string.Empty;
            routeClass.Version = contract.ApiVersion ?? string.Empty;
            routeClass.Base = contract.Base ?? string.Empty;
            routeClasses[contract.Name] = routeClass;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using LibSupportToolsServerWork.Registry.Paths;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Mappers;

//პროექტის ServerInfo (B7): ServerInfoModel ↔ StsServerInfoDataModel, პროექტის აგრეგატის ნაწილი (G7). ჩანაწერის
//ნატურალური გასაღებია (ServerName, EnvironmentName), რეგისტრის გარეშე. ლოკალური dictionary-ის key (GUID ან
//"Server|Env") სერვერზე არ მიდის: ApplyToLocal მას ნატურალური გასაღებით პოულობს და ინარჩუნებს. AppSettings-ის ორი
//ფაილი PathMapper-ით გარდაიქმნება (README §4.4). AllowToolsList EProjectServerTools-ის სახელებია
public static class ServerInfoMapper
{
    public static StsServerInfoDataModel ToContract(ServerInfoModel serverInfo, PathMapper pathMapper)
    {
        return new StsServerInfoDataModel
        {
            ServerName = serverInfo.ServerName ?? string.Empty,
            EnvironmentName = serverInfo.EnvironmentName ?? string.Empty,
            WebAgentNameForCheck = serverInfo.WebAgentNameForCheck,
            ServerSidePort = serverInfo.ServerSidePort,
            ApiVersionId = serverInfo.ApiVersionId,
            AppSettingsJsonSourceFileName = pathMapper.ToCanonical(serverInfo.AppSettingsJsonSourceFileName),
            AppSettingsEncodedJsonFileName = pathMapper.ToCanonical(serverInfo.AppSettingsEncodedJsonFileName),
            ServiceUserName = serverInfo.ServiceUserName,
            AllowToolsList = [.. (serverInfo.AllowToolsList ?? []).Select(x => x.ToString())],
            CurrentDatabaseParameters = DatabaseParametersMapper.ToContract(serverInfo.CurrentDatabaseParameters),
            NewDatabaseParameters = DatabaseParametersMapper.ToContract(serverInfo.NewDatabaseParameters)
        };
    }

    //ნატურალური გასაღები ServerInfoModel.GetItemKey-ის ფორმით; შედარება რეგისტრის გარეშეა
    public static string NaturalKey(string? serverName, string? environmentName)
    {
        return $"{serverName}|{environmentName}";
    }

    //სერვერის ServerInfo-ები მთლიანად ანაცვლებს ლოკალურებს (G7), dictionary-ის ეგზემპლარი კი რჩება. არსებული ჩანაწერი
    //ნატურალური გასაღებით მოიძებნება და თავის key-ს ინარჩუნებს (თუ ლოკალურად ერთი გასაღები ორჯერაა, პირველს). ახალი
    //ჩანაწერი "{ServerName}|{EnvironmentName}" key-ს იღებს; თუ ეს key სხვა ჩანაწერს უკავია, ახალ GUID-ს
    public static void ApplyToLocal(IEnumerable<StsServerInfoDataModel> contracts,
        Dictionary<string, ServerInfoModel> serverInfos, PathMapper pathMapper)
    {
        Dictionary<string, KeyValuePair<string, ServerInfoModel>> existingByNaturalKey =
            new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, ServerInfoModel> serverInfo in serverInfos)
        {
            existingByNaturalKey.TryAdd(NaturalKey(serverInfo.Value.ServerName, serverInfo.Value.EnvironmentName),
                serverInfo);
        }

        HashSet<string> usedKeys = new(serverInfos.Comparer);
        List<(string? Key, StsServerInfoDataModel Contract, ServerInfoModel? Existing)> matches = [];
        foreach (StsServerInfoDataModel contract in contracts)
        {
            if (existingByNaturalKey.Remove(NaturalKey(contract.ServerName, contract.EnvironmentName),
                    out KeyValuePair<string, ServerInfoModel> existing))
            {
                usedKeys.Add(existing.Key);
                matches.Add((existing.Key, contract, existing.Value));
            }
            else
            {
                matches.Add((null, contract, null));
            }
        }

        serverInfos.Clear();
        foreach ((string? key, StsServerInfoDataModel contract, ServerInfoModel? existing) in matches)
        {
            string itemKey = key ?? NewKey(contract, usedKeys);
            serverInfos[itemKey] = ToLocal(contract, existing, pathMapper);
        }
    }

    public static StsServerInfoDataModel Normalize(StsServerInfoDataModel contract)
    {
        contract.WebAgentNameForCheck = ContractNormalization.EmptyToNull(contract.WebAgentNameForCheck);
        contract.ApiVersionId = ContractNormalization.EmptyToNull(contract.ApiVersionId);
        contract.AppSettingsJsonSourceFileName =
            ContractNormalization.UpperDriveLetter(
                ContractNormalization.EmptyToNull(contract.AppSettingsJsonSourceFileName));
        contract.AppSettingsEncodedJsonFileName =
            ContractNormalization.UpperDriveLetter(
                ContractNormalization.EmptyToNull(contract.AppSettingsEncodedJsonFileName));
        contract.ServiceUserName = ContractNormalization.EmptyToNull(contract.ServiceUserName);
        contract.AllowToolsList = ContractNormalization.OrderByName(
            contract.AllowToolsList.Select(ContractNormalization.EnumName<EProjectServerTools>), x => x);
        contract.CurrentDatabaseParameters = DatabaseParametersMapper.Normalize(contract.CurrentDatabaseParameters);
        contract.NewDatabaseParameters = DatabaseParametersMapper.Normalize(contract.NewDatabaseParameters);
        return contract;
    }

    //ამ კლიენტისთვის უცნობი enum-ის ველის სახელი ("<Server>|<Env>.<Field>"); null — ასეთი ველი არ არის
    public static string? FindUnknownEnumField(StsServerInfoDataModel contract)
    {
        string prefix = NaturalKey(contract.ServerName, contract.EnvironmentName);
        if (!contract.AllowToolsList.All(ContractNormalization.IsEnumName<EProjectServerTools>))
        {
            return $"{prefix}.{nameof(StsServerInfoDataModel.AllowToolsList)}";
        }

        string? field = DatabaseParametersMapper.FindUnknownEnumField(contract.CurrentDatabaseParameters);
        if (field is not null)
        {
            return $"{prefix}.{nameof(StsServerInfoDataModel.CurrentDatabaseParameters)}.{field}";
        }

        field = DatabaseParametersMapper.FindUnknownEnumField(contract.NewDatabaseParameters);
        return field is null ? null : $"{prefix}.{nameof(StsServerInfoDataModel.NewDatabaseParameters)}.{field}";
    }

    //GUID-ში '|' არ არის, ამიტომ ის ნატურალური გასაღების ფორმის key-ს ვერ დაემთხვევა და usedKeys-ში არ ემატება
    private static string NewKey(StsServerInfoDataModel contract, HashSet<string> usedKeys)
    {
        string key = NaturalKey(contract.ServerName, contract.EnvironmentName);
        return usedKeys.Add(key) ? key : Guid.NewGuid().ToString();
    }

    //CurrentDatabaseParameters და NewDatabaseParameters init-only-ა. DatabaseParametersMapper.ToLocal არსებულ
    //ეგზემპლარს ადგილზე ანახლებს, ამიტომ თუ ორივე იგივე ეგზემპლარია (ან ორივე აკლია), ჩანაწერი ადგილზე ახლდება; თუ
    //რომელიმე ჩნდება ან ქრება, ჩანაწერი ახალი ობიექტით იცვლება. ServerInfoModel-ის ყველა ველი კონტრაქტშია
    private static ServerInfoModel ToLocal(StsServerInfoDataModel contract, ServerInfoModel? existing,
        PathMapper pathMapper)
    {
        DatabaseParameters? currentDatabaseParameters =
            DatabaseParametersMapper.ToLocal(contract.CurrentDatabaseParameters, existing?.CurrentDatabaseParameters);
        DatabaseParameters? newDatabaseParameters =
            DatabaseParametersMapper.ToLocal(contract.NewDatabaseParameters, existing?.NewDatabaseParameters);
        ServerInfoModel serverInfo = existing is not null &&
                                     ReferenceEquals(existing.CurrentDatabaseParameters, currentDatabaseParameters) &&
                                     ReferenceEquals(existing.NewDatabaseParameters, newDatabaseParameters)
            ? existing
            : new ServerInfoModel
            {
                CurrentDatabaseParameters = currentDatabaseParameters,
                NewDatabaseParameters = newDatabaseParameters,
                AllowToolsList = existing?.AllowToolsList
            };

        serverInfo.ServerName = contract.ServerName;
        serverInfo.EnvironmentName = contract.EnvironmentName;
        serverInfo.WebAgentNameForCheck = contract.WebAgentNameForCheck;
        serverInfo.ServerSidePort = contract.ServerSidePort;
        serverInfo.ApiVersionId = contract.ApiVersionId;
        serverInfo.AppSettingsJsonSourceFileName = pathMapper.ToLocal(contract.AppSettingsJsonSourceFileName);
        serverInfo.AppSettingsEncodedJsonFileName = pathMapper.ToLocal(contract.AppSettingsEncodedJsonFileName);
        serverInfo.ServiceUserName = contract.ServiceUserName;

        //ცარიელი სია და სიის უქონლობა ერთია: ლოკალური null ცარიელი კონტრაქტით არ იცვლება
        if (serverInfo.AllowToolsList is null && contract.AllowToolsList.Count == 0)
        {
            return serverInfo;
        }

        serverInfo.AllowToolsList ??= [];
        serverInfo.AllowToolsList.Clear();
        serverInfo.AllowToolsList.AddRange(
            contract.AllowToolsList.Select(x => Enum.Parse<EProjectServerTools>(x, true)));
        return serverInfo;
    }
}

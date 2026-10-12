using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Cruders;

//სერვერის რედაქტორების (Support Tools Server Editor) ვერსიის შემოწმება (D4). ძველი git-ის endpoint-ები ვერსიას არ
//ამოწმებს, ამიტომ რედაქტორი ჩაწერამდე სერვერის სიას თავიდან კითხულობს და ჩანაწერს სიის ჩატვირთვისას ნანახს ადარებს
//(B1-ის წესი, როგორც რეესტრის ადაპტერების UpsertCheckingVersionOnClient-ში): შუალედში სხვა კომპიუტერის ან
//Sync Registry-ის ცვლილება ჩუმად აღარ გადაიწერება. შემოწმებასა და ჩაწერას შორის ფანჯარა მცირეა
internal static class StsRecordVersionCheck
{
    //ჩანაწერის ვერსია ჩატვირთულ სიაში; null — სიაში არ არის (ახალი ჩანაწერი). სერვერი სახელებს რეგისტრის გარეშე ადარებს
    public static int? FindVersion<T>(List<T> records, string key, Func<T, string> getKey, Func<T, int> getVersion)
        where T : class
    {
        T? record = records.Find(x => string.Equals(getKey(x), key, StringComparison.OrdinalIgnoreCase));
        return record is null ? null : getVersion(record);
    }

    //loadedVersion: ჩანაწერის ვერსია სიის ჩატვირთვისას; null — ახალი ჩანაწერი, რომელიც სერვერზე ჯერ არ უნდა იყოს.
    //null — ჩაწერა შეიძლება; სხვა შემთხვევაში ConcurrencyConflict, RecordWithNameNotFound ან სიის წაკითხვის შეცდომა
    public static async Task<Error?> Check<T>(string entityName, string key, int? loadedVersion,
        Func<CancellationToken, Task<Result<List<T>>>> readServerRecords, Func<T, string> getKey,
        Func<T, int> getVersion, CancellationToken cancellationToken) where T : class
    {
        Result<List<T>> serverRecords = await readServerRecords(cancellationToken);
        if (serverRecords.IsFailure)
        {
            return serverRecords.Error;
        }

        int? storedVersion = FindVersion(serverRecords.Value, key, getKey, getVersion);
        if (storedVersion == loadedVersion)
        {
            return null;
        }

        return storedVersion is null
            ? SupportToolsServerApiClientErrors.RecordWithNameNotFound(entityName, key)
            : SupportToolsServerApiClientErrors.ConcurrencyConflict(entityName, key, loadedVersion ?? 0,
                storedVersion.Value);
    }
}

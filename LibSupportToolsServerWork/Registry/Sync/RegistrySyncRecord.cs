namespace LibSupportToolsServerWork.Registry.Sync;

//ჩანაწერი დამგეგმავისთვის: ნორმალიზებული კონტრაქტი და მისი ჰეში (RegistryContractHasher). Version სერვერის ჩანაწერის
//ვერსიაა; ლოკალურ ჩანაწერს ვერსია არ აქვს და 0-ია
public sealed record RegistrySyncRecord(object Contract, string Hash, int Version);

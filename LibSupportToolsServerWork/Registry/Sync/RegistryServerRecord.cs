namespace LibSupportToolsServerWork.Registry.Sync;

//სერვერის ჩანაწერი, როგორც ადაპტერი აბრუნებს: კონტრაქტი და მისი Version (optimistic concurrency, B1)
public sealed record RegistryServerRecord(object Contract, int Version);

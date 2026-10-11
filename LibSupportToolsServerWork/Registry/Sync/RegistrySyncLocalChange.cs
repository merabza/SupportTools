namespace LibSupportToolsServerWork.Registry.Sync;

//ლოკალური ცვლილება ბოლო სინქრონიზაციის შემდეგ (RegistrySyncEngine.GetLocalChanges). Hash null — ჩანაწერი ლოკალურად
//აღარ არის
public sealed record RegistrySyncLocalChange(string CollectionName, string Key, string? Hash);

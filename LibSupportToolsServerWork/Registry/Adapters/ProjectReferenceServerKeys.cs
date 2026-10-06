namespace LibSupportToolsServerWork.Registry.Adapters;

//კოლექციები, რომლებსაც პროექტი მიმართავს და რომელთა სერვერის გასაღებებსაც Projects-ის ადაპტერი Push-მდე ამოწმებს:
//მითითებული ჩანაწერი ან ლოკალურად უნდა იყოს, ან სერვერზე
public sealed record ProjectReferenceServerKeys(
    IRegistryServerKeys Gits,
    IRegistryServerKeys NpmPackages,
    IRegistryServerKeys EditorConfigPatterns,
    IRegistryServerKeys DatabaseServerConnections);

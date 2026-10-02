namespace LibSupportToolsServerWork.Registry.Paths;

//გზა, რომელსაც mapping-ის არცერთი წესი არ დაემთხვა და უცვლელად დარჩა
public sealed record PathMappingIssue(EPathMappingDirection Direction, string Path);

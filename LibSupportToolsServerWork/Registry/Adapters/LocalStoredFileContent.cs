namespace LibSupportToolsServerWork.Registry.Adapters;

//ლოკალური საიდუმლო ფაილის შიგთავსი BOM-ის გარეშე და მისი Sha256 და Length (StoredFileContract-ის წესით)
internal sealed record LocalStoredFileContent(string Content, string Sha256, int Length);

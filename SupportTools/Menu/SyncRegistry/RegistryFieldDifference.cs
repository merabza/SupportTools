namespace SupportTools.Menu.SyncRegistry;

//ჩანაწერის ველი (გზით, მაგ. ServerInfos[0].AppSettingsJsonSourceFileName), რომელშიც ლოკალური და სერვერის მხარე
//განსხვავდება. მნიშვნელობები გამოსატანადაა: საიდუმლო დაფარულია (RegistryRecordDiff.HiddenValue), null ნიშნავს, რომ ველი
//ამ მხარეს არ არის
internal sealed record RegistryFieldDifference(string Path, string? Local, string? Server);

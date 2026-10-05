namespace LibSupportToolsServerWork.Registry.Adapters;

//ადაპტერის გაფრთხილება: ჩანაწერი (Key) ან მთელი კოლექცია (Key == null), რომელიც სინქრონიზაციაში ვერ მონაწილეობს.
//შეტყობინებაში საიდუმლო მნიშვნელობა არ იწერება
public sealed record RegistrySyncWarning(string CollectionName, string? Key, string Message);

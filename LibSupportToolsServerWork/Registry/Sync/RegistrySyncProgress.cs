namespace LibSupportToolsServerWork.Registry.Sync;

//სერვერის ერთი შესრულებული ოპერაცია (upsert ან წაშლა) და მისი შედეგი: Number-ე (1-დან) Count-იდან. Count არჩეული
//სერვერის ოპერაციების რაოდენობაა, მათ შორის იმათიც, რომლებიც გაჩერების გამო აღარ შესრულდება (NotExecuted; მათზე
//პროგრესი არ იძახება). C5 ამით ასობით მოთხოვნის პროგრესს აჩვენებს
public sealed record RegistrySyncProgress(int Number, int Count, RegistrySyncReportItem Item);

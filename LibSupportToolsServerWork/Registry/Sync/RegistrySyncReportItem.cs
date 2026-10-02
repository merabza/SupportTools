using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Sync;

//გეგმის ერთი ჩანაწერის შედეგი. Error ივსება Conflict-ისა და Failed-ის დროს
public sealed record RegistrySyncReportItem(RegistrySyncPlanItem PlanItem, ERegistrySyncOutcome Outcome, Error? Error);

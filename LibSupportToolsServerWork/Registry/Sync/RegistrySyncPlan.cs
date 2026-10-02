using System.Collections.Generic;

namespace LibSupportToolsServerWork.Registry.Sync;

//სინქრონიზაციის გეგმა: ჩანაწერები კოლექციების რიგით (Order), კოლექციის შიგნით გასაღებით. გეგმა ერთხელ სრულდება;
//შემდეგი შესრულებისთვის ახალი გეგმა უნდა აიგოს, რადგან მდგომარეობა და სერვერის ვერსიები შეიცვალა
public sealed record RegistrySyncPlan(IReadOnlyList<RegistrySyncPlanItem> Items);

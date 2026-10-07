using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Sync;

//კოლექცია, რომლის ლოკალური ჩანაწერები ამ კომპიუტერის ფაილებია (საიდუმლო ფაილები, C6). ძრავა მას ფრთხილად ეპყრობა:
//- ჩანაწერი, რომელიც ამ კომპიუტერზე უნდა იყოს, მაგრამ მისი ფაილი აკლია (GetMissingLocalKeys), ლოკალურად წაშლილად არ
//  ითვლება: დამგეგმავი მას სერვერიდან ჩამოიტანს და სერვერიდან არ წაშლის;
//- წაშლა ორივე მიმართულებით კონფლიქტია (ERegistrySyncConflict.DeleteNeedsConfirmation): ფაილი მხოლოდ მომხმარებლის
//  გადაწყვეტით იშლება, არჩევანის პრესეტები (AllNonConflicting, PullOnly, PushOnly) მას არ შლის;
//- ApplyLocal-ის წინ შემსრულებელი PrepareApplyLocal-ს იძახებს: ადაპტერი სერვერიდან ითხოვს იმას, რაც სერვერის სიაში არ
//  მოდის (ფაილის შიგთავსი). წარუმატებლობისას ჩანაწერი Failed-ია და ApplyLocal არ იძახება
public interface IRegistryFileSyncAdapter : IRegistrySyncAdapter
{
    //ჩანაწერები, რომლებიც ამ კომპიუტერზე უნდა იყოს (მაგ. რეესტრი მათ ფაილზე მიუთითებს), მაგრამ ფაილი არ არსებობს.
    //ძრავა მას GetLocalRecords-ის შემდეგ იძახებს
    IReadOnlyCollection<string> GetMissingLocalKeys();

    //ApplyLocal-ის მომზადება: რაც სერვერის კონტრაქტს აკლია, ის სერვერიდან მოაქვს. key და contract იგივეა, რაც ApplyLocal-ში
    Task<Result> PrepareApplyLocal(string key, object contract, CancellationToken cancellationToken);
}

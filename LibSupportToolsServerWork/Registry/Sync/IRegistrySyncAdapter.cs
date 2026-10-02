using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Sync;

//რეესტრის ერთი კოლექციის ადაპტერი სინქრონიზაციის ძრავისთვის (RegistrySyncEngine). კონკრეტულ ადაპტერებს C3 და C4 წერს.
//კონტრაქტი სერვერის Sts…DataModel-ია, კანონიკური გზებით (README §4.4). ძრავა კონტრაქტს მხოლოდ ჰეშავს და ადაპტერს
//უბრუნებს, ამიტომ მისი ტიპი ადაპტერის საქმეა. ჩანაწერის გასაღები მისი სახელია და რეგისტრის გარეშე შედარდება (G8):
//გასაღებები, რომლებიც მხოლოდ რეგისტრით განსხვავდება, ძრავა შეცდომად აბრუნებს
public interface IRegistrySyncAdapter
{
    //კოლექციის სახელი: მდგომარეობის გასაღები (RegistrySyncState.Collections) და ანგარიშის ნაწილი. უნიკალურია და არ
    //უნდა შეიცვალოს, თორემ კოლექციის მდგომარეობა დაიკარგება
    string CollectionName { get; }

    //დამოკიდებულების რიგი. სერვერის upsert-ები და ApplyLocal ზრდადობით სრულდება, წაშლები კლებადობით. მითითებულ
    //კოლექციას (მაგ. ApiClients) ნაკლები Order აქვს, ვიდრე მიმთითებელს (მაგ. Servers); Projects ბოლოა
    int Order { get; }

    //ჰეშისთვის ნორმალიზაცია: "" → null, სიმრავლეების დალაგება OrdinalIgnoreCase-ით და სხვ. ძრავა ნორმალიზაციას ორივე
    //მხარის კონტრაქტზე იძახებს და ამოწმებს, რომ ნორმალიზებულის ხელახალი ნორმალიზაცია ჰეშს არ ცვლის. შეიძლება
    //დააბრუნოს ახალი ეგზემპლარი ან იგივე, ადგილზე შეცვლილი
    object Normalize(object contract);

    //ლოკალური ჩანაწერები: გასაღები → კონტრაქტი კანონიკური გზებით. კომპიუტერის ველები (MachineLocalFields) არ შედის.
    //ძრავა მას ApplyLocal-ისა და RemoveLocal-ის შემდეგაც იძახებს, რომ მდგომარეობაში ახალი ლოკალური ჰეში ჩაწეროს
    IReadOnlyDictionary<string, object> GetLocalRecords();

    //სერვერის ჩანაწერები: გასაღები → კონტრაქტი და მისი Version. შეცდომისას (სერვერი მიუწვდომელია და სხვ.) გეგმა არ
    //იგება და ძრავა ამ შეცდომას აბრუნებს
    Task<Result<IReadOnlyDictionary<string, RegistryServerRecord>>> GetServerRecords(
        CancellationToken cancellationToken);

    //სერვერზე ჩაწერა (B1-ის upsert): expectedVersion 0 — შექმნა, N — განახლება მხოლოდ მაშინ, თუ სერვერზე ვერსია N-ია.
    //წარმატებისას აბრუნებს ჩანაწერის ახალ ვერსიას. შეცდომა სერვერის კოდით უნდა დაბრუნდეს (RegistrySyncServerErrorCodes)
    Task<Result<int>> Upsert(string key, object contract, int expectedVersion, CancellationToken cancellationToken);

    //სერვერიდან წაშლა, თუ სერვერზე ვერსია expectedVersion-ია
    Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken);

    //სერვერის კონტრაქტის გაერთიანება ლოკალურ მოდელში, ადგილზე: კომპიუტერის ველები და ის ველები, რომლებიც კონტრაქტში არ
    //არის, უცვლელი რჩება; dictionary-ების ეგზემპლარებს არ ცვლის, რადგან მენიუს რედაქტორები მათზე მიმართვებს ინახავს.
    //თუ ჩანაწერი ლოკალურად არსებობს, key მისი ლოკალური წერილობაა, თორემ სერვერისა. შენახვას ძრავა აკეთებს
    void ApplyLocal(string key, object contract);

    //ლოკალური ჩანაწერის წაშლა. key ლოკალური წერილობაა. შენახვას ძრავა აკეთებს
    void RemoveLocal(string key);
}

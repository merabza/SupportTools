using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Sync;

//სინქრონიზაციის ძრავის შეცდომები
public static class RegistrySyncErrors
{
    //გასაღებები, რომლებიც მხოლოდ რეგისტრით განსხვავდება (G8): ვერ დგინდება, რომელი ჩანაწერი რომელს შეესაბამება
    public static Error DuplicateKeys(string collectionName, string side, string keys)
    {
        return Error.Problem(nameof(DuplicateKeys), $"{collectionName}: {side} keys differ only by case: {keys}");
    }

    //ადაპტერის ნორმალიზაცია სტაბილური არ არის: ნორმალიზებული კონტრაქტის ხელახალი ნორმალიზაცია ჰეშს ცვლის
    public static Error NormalizationIsNotStable(string collectionName, string key)
    {
        return Error.Problem(nameof(NormalizationIsNotStable),
            $"{collectionName}/{key}: normalizing the normalized contract again changes its hash");
    }

    //ApplyLocal-ის შემდეგ ადაპტერის ლოკალურ ჩანაწერებში ეს ჩანაწერი (ზუსტად ერთხელ) არ ჩანს
    public static Error LocalRecordNotApplied(string collectionName, string key)
    {
        return Error.Problem(nameof(LocalRecordNotApplied),
            $"{collectionName}/{key}: the record is not in the local records after it was applied");
    }

    //RemoveLocal-ის შემდეგ ჩანაწერი ისევ ჩანს ადაპტერის ლოკალურ ჩანაწერებში
    public static Error LocalRecordNotRemoved(string collectionName, string key)
    {
        return Error.Problem(nameof(LocalRecordNotRemoved),
            $"{collectionName}/{key}: the record is still in the local records after it was removed");
    }
}

using System.Collections.Generic;
using System.Linq;

namespace LibSupportToolsServerWork.Registry.Adapters;

//კოლექცია, რომელიც ლოკალურად Dictionary<string, TLocal>-ია: გასაღები ჩანაწერის სახელია (G8). dictionary-ის
//ეგზემპლარი არ იცვლება, რადგან მენიუს რედაქტორები მასზე მიმართვას ინახავს
public abstract class DictionaryRegistrySyncAdapter<TLocal, TContract> : RegistrySyncAdapter<TContract>
    where TContract : class
{
    // ReSharper disable once ConvertToPrimaryConstructor
    protected DictionaryRegistrySyncAdapter(RegistrySyncWarnings warnings) : base(warnings)
    {
    }

    public override void RemoveLocal(string key)
    {
        GetLocalDictionary().Remove(key);
    }

    protected override IEnumerable<(string Key, TContract Contract)> GetLocalContracts()
    {
        return GetLocalDictionary().Select(x => (x.Key, ToContract(x.Key, x.Value)));
    }

    //არსებული ჩანაწერი ToLocal-ს გადაეცემა, რომ ის ადგილზე განახლდეს: კომპიუტერის ველები, კონტრაქტში ჯერ არარსებული
    //ველები (README §4.6) და ღია რედაქტორების მიმართვები ასე შენარჩუნდება
    protected override void ApplyContract(string key, TContract contract)
    {
        Dictionary<string, TLocal> dictionary = GetLocalDictionary();
        dictionary[key] = ToLocal(contract, dictionary.GetValueOrDefault(key));
    }

    protected abstract Dictionary<string, TLocal> GetLocalDictionary();

    protected abstract TContract ToContract(string key, TLocal local);

    //existing: ლოკალური ჩანაწერი, თუ ის უკვე არსებობს
    protected abstract TLocal ToLocal(TContract contract, TLocal? existing);
}

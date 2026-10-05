using System.Collections.Generic;

namespace LibSupportToolsServerWork.Registry.Adapters;

//ერთი სინქრონიზაციის ადაპტერების გაფრთხილებები. ძრავა ადაპტერებს რამდენჯერმე კითხულობს, ამიტომ ერთნაირი
//გაფრთხილება ერთხელ ინახება. სინქრონიზაციის ბრძანება (C5) მათ PathMapper-ის Issues-თან ერთად აჩვენებს
public sealed class RegistrySyncWarnings
{
    private readonly List<RegistrySyncWarning> _items = [];

    public IReadOnlyList<RegistrySyncWarning> Items => _items;

    public void Add(string collectionName, string? key, string message)
    {
        var warning = new RegistrySyncWarning(collectionName, key, message);
        if (!_items.Contains(warning))
        {
            _items.Add(warning);
        }
    }
}

using System.Collections.Generic;

namespace LibSupportToolsServerWork.Registry.Adapters;

//კოლექციის სერვერის ჩანაწერების გასაღებები, ბოლო წაკითხვიდან (GetServerRecords). ძრავა ადაპტერებს Order-ის რიგით
//კითხულობს, ამიტომ მიმთითებელი კოლექციის ადაპტერი (Projects) მითითებული კოლექციების გასაღებებს უკვე წაკითხულს ხედავს.
//წაკითხვამდე სია ცარიელია
public interface IRegistryServerKeys
{
    IReadOnlyCollection<string> ServerKeys { get; }
}

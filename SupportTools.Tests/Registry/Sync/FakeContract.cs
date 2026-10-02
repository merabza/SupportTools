namespace SupportTools.Tests.Registry.Sync;

//the contract of the fake collection: like the server contracts it carries its name and the Version used for
//optimistic concurrency
internal sealed record FakeContract(string Name, string? Value, int Version);

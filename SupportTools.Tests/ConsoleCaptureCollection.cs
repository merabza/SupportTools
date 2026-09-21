using Xunit;

namespace SupportTools.Tests;

//Console.SetOut is process-wide: test classes that capture console output must not run in parallel with other tests
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleCaptureCollection
{
    public const string Name = "ConsoleCapture";
}

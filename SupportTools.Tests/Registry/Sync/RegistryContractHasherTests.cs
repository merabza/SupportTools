using System;
using System.Collections.Generic;
using LibSupportToolsServerWork.Registry.Sync;
using Xunit;

namespace SupportTools.Tests.Registry.Sync;

public sealed class RegistryContractHasherTests
{
    //the hash of {"Description":"Development","Name":"Dev"}. Stored sync states contain such hashes: if this value
    //changes, every synced record looks locally changed on every computer
    private const string DevEnvironmentHash = "D6A2073E3F71E84D6C43F70C31ED7ACCE507B2570E9A4ADACE141CFB5E43364F";

    [Fact]
    public void ComputeHash_WhenContractIsGiven_ReturnsSha256OfCanonicalJsonAsUpperCaseHex()
    {
        // Arrange
        var contract = new EnvironmentContract("Dev", "Development", 5);

        // Act
        string result = RegistryContractHasher.ComputeHash(contract);

        // Assert
        Assert.Equal(DevEnvironmentHash, result);
    }

    [Fact]
    public void ToCanonicalJson_WhenContractHasNestedValues_SortsPropertiesAndKeysButKeepsListOrder()
    {
        // Arrange
        var contract = new ProjectContract("P", ["b", "a"], new Dictionary<string, string> { ["b"] = "2", ["A"] = "1" },
            new ChildContract("c", 2), 7);

        // Act
        string result = RegistryContractHasher.ToCanonicalJson(contract);

        // Assert
        Assert.Equal(
            """{"Child":{"Name":"c","Version":2},"GitNames":["b","a"],"Name":"P","Settings":{"A":"1","b":"2"}}""",
            result);
    }

    [Fact]
    public void ComputeHash_WhenCalledTwiceForEqualContracts_ReturnsSameHash()
    {
        // Arrange
        var first = new EnvironmentContract("Dev", "Development", 0);
        var second = new EnvironmentContract("Dev", "Development", 0);

        // Act
        string firstHash = RegistryContractHasher.ComputeHash(first);
        string secondHash = RegistryContractHasher.ComputeHash(second);

        // Assert
        Assert.Equal(firstHash, secondHash);
    }

    //the version belongs to the optimistic concurrency, not to the content
    [Fact]
    public void ComputeHash_WhenOnlyVersionDiffers_ReturnsSameHash()
    {
        // Arrange
        var local = new EnvironmentContract("Dev", "Development", 0);
        var server = new EnvironmentContract("Dev", "Development", 12);

        // Act
        string localHash = RegistryContractHasher.ComputeHash(local);
        string serverHash = RegistryContractHasher.ComputeHash(server);

        // Assert
        Assert.Equal(localHash, serverHash);
    }

    [Fact]
    public void ComputeHash_WhenNestedVersionDiffers_ReturnsDifferentHash()
    {
        // Arrange
        var first = new ProjectContract("P", [], [], new ChildContract("c", 1), 0);
        var second = new ProjectContract("P", [], [], new ChildContract("c", 2), 0);

        // Act
        string firstHash = RegistryContractHasher.ComputeHash(first);
        string secondHash = RegistryContractHasher.ComputeHash(second);

        // Assert
        Assert.NotEqual(firstHash, secondHash);
    }

    [Theory]
    [InlineData("Dev", "Development changed")]
    [InlineData("dev", "Development")]
    [InlineData("Dev", null)]
    public void ComputeHash_WhenContentDiffers_ReturnsDifferentHash(string name, string? description)
    {
        // Arrange
        var original = new EnvironmentContract("Dev", "Development", 0);
        var changed = new EnvironmentContract(name, description, 0);

        // Act
        string originalHash = RegistryContractHasher.ComputeHash(original);
        string changedHash = RegistryContractHasher.ComputeHash(changed);

        // Assert
        Assert.NotEqual(originalHash, changedHash);
    }

    [Fact]
    public void ComputeHash_WhenPropertiesAreDeclaredInOtherOrder_ReturnsSameHash()
    {
        // Arrange
        var contract = new EnvironmentContract("Dev", "Development", 0);
        var reordered = new ReorderedEnvironmentContract(0, "Development", "Dev");

        // Act
        string contractHash = RegistryContractHasher.ComputeHash(contract);
        string reorderedHash = RegistryContractHasher.ComputeHash(reordered);

        // Assert
        Assert.Equal(contractHash, reorderedHash);
    }

    [Fact]
    public void ComputeHash_WhenDictionaryKeysWereAddedInOtherOrder_ReturnsSameHash()
    {
        // Arrange
        var first = new ProjectContract("P", [], new Dictionary<string, string> { ["A"] = "1", ["B"] = "2" }, null, 0);
        var second = new ProjectContract("P", [], new Dictionary<string, string> { ["B"] = "2", ["A"] = "1" }, null, 0);

        // Act
        string firstHash = RegistryContractHasher.ComputeHash(first);
        string secondHash = RegistryContractHasher.ComputeHash(second);

        // Assert
        Assert.Equal(firstHash, secondHash);
    }

    //sorting sets is the job of the adapter's normalization: a list may be ordered on purpose
    [Fact]
    public void ComputeHash_WhenListOrderDiffers_ReturnsDifferentHash()
    {
        // Arrange
        var first = new ProjectContract("P", ["a", "b"], [], null, 0);
        var second = new ProjectContract("P", ["b", "a"], [], null, 0);

        // Act
        string firstHash = RegistryContractHasher.ComputeHash(first);
        string secondHash = RegistryContractHasher.ComputeHash(second);

        // Assert
        Assert.NotEqual(firstHash, secondHash);
    }

    //a field added to a contract later (README §4.6) does not change the hashes stored before, while it is empty
    [Fact]
    public void ComputeHash_WhenNewFieldsAreNullOrDefault_ReturnsSameHashAsWithoutThem()
    {
        // Arrange
        var contract = new EnvironmentContract("Dev", "Development", 0);
        var extended = new ExtendedEnvironmentContract("Dev", "Development", null, 0, false, 0);

        // Act
        string contractHash = RegistryContractHasher.ComputeHash(contract);
        string extendedHash = RegistryContractHasher.ComputeHash(extended);

        // Assert
        Assert.Equal(contractHash, extendedHash);
    }

    //computers in different time zones must get the same hash for the same instant
    [Fact]
    public void ComputeHash_WhenSameInstantIsLocalOrUtcTime_ReturnsSameHash()
    {
        // Arrange
        var utcTime = new DateTime(2026, 10, 2, 8, 30, 0, DateTimeKind.Utc);
        var utcContract = new TimedContract("Backup", utcTime, 0);
        var localContract = new TimedContract("Backup", utcTime.ToLocalTime(), 0);

        // Act
        string utcHash = RegistryContractHasher.ComputeHash(utcContract);
        string localHash = RegistryContractHasher.ComputeHash(localContract);

        // Assert
        Assert.Equal(utcHash, localHash);
        Assert.Equal("""{"Name":"Backup","When":"2026-10-02T08:30:00Z"}""",
            RegistryContractHasher.ToCanonicalJson(localContract));
    }

    //"" → null is the job of the adapter's normalization
    [Fact]
    public void ComputeHash_WhenEmptyStringReplacesNull_ReturnsDifferentHash()
    {
        // Arrange
        var withNull = new EnvironmentContract("Dev", null, 0);
        var withEmptyString = new EnvironmentContract("Dev", "", 0);

        // Act
        string withNullHash = RegistryContractHasher.ComputeHash(withNull);
        string withEmptyStringHash = RegistryContractHasher.ComputeHash(withEmptyString);

        // Assert
        Assert.NotEqual(withNullHash, withEmptyStringHash);
    }

    private sealed record EnvironmentContract(string Name, string? Description, int Version);

    private sealed record ReorderedEnvironmentContract(int Version, string? Description, string Name);

    private sealed record ExtendedEnvironmentContract(
        string Name,
        string? Description,
        string? NewText,
        int NewNumber,
        bool NewFlag,
        int Version);

    private sealed record ChildContract(string Name, int Version);

    private sealed record TimedContract(string Name, DateTime When, int Version);

    private sealed record ProjectContract(
        string Name,
        List<string> GitNames,
        Dictionary<string, string> Settings,
        ChildContract? Child,
        int Version);
}

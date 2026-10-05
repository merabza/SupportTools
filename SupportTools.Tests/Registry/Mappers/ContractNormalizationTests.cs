using System.Collections.Generic;
using System.Linq;
using LibSupportToolsServerWork.Registry.Mappers;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class ContractNormalizationTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(" ", " ")]
    [InlineData("x", "x")]
    public void EmptyToNull_WhenCalled_TurnsOnlyEmptyStringIntoNull(string? value, string? expected)
    {
        // Act
        string? result = ContractNormalization.EmptyToNull(value);

        // Assert
        Assert.Equal(expected, result);
    }

    //only the drive letter: the rest of a path keeps its case, which matters on Linux
    [Theory]
    [InlineData(@"d:\1WorkDotnet\x", @"D:\1WorkDotnet\x")]
    [InlineData("d:", "D:")]
    [InlineData(@"D:\1workdotnet\x", @"D:\1workdotnet\x")]
    [InlineData("ftp://files.example.test/d", "ftp://files.example.test/d")]
    [InlineData("/home/u/x", "/home/u/x")]
    [InlineData(@"\\server\share", @"\\server\share")]
    [InlineData("d", "d")]
    [InlineData(null, null)]
    public void UpperDriveLetter_WhenCalled_UppersOnlyTheDriveLetter(string? path, string? expected)
    {
        // Act
        string? result = ContractNormalization.UpperDriveLetter(path);

        // Assert
        Assert.Equal(expected, result);
    }

    //the server orders child lists by name ignoring case; names that differ only by case keep a stable order
    [Fact]
    public void OrderByName_WhenCalled_OrdersIgnoringCaseThenOrdinal()
    {
        // Arrange
        List<string> names = ["b", "A", "a", "C"];

        // Act
        List<string> result = ContractNormalization.OrderByName(names, x => x);

        // Assert
        Assert.Equal(["A", "a", "b", "C"], result);
    }

    [Fact]
    public void OrderByName_WhenCalledForEveryPermutation_GivesTheSameOrder()
    {
        // Arrange
        List<string> names = ["Default", "default", "Backup"];

        // Act
        List<List<string>> results =
        [
            ContractNormalization.OrderByName(names, x => x),
            ContractNormalization.OrderByName(names.AsEnumerable().Reverse(), x => x),
            ContractNormalization.OrderByName(["default", "Backup", "Default"], x => x)
        ];

        // Assert
        Assert.All(results, x => Assert.Equal(results[0], x));
    }

    [Theory]
    [InlineData("SqlServer", "SqlServer")]
    [InlineData("sqlserver", "SqlServer")]
    [InlineData("Unknown", "Unknown")]
    public void EnumName_WhenCalled_GivesTheCanonicalNameOrKeepsAnUnknownOne(string name, string expected)
    {
        // Act
        string result = ContractNormalization.EnumName<EDatabaseProvider>(name);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Day", true)]
    [InlineData("day", true)]
    [InlineData("Fortnight", false)]
    public void IsEnumName_WhenCalled_TellsWhetherThisClientKnowsTheName(string name, bool expected)
    {
        // Act
        bool result = ContractNormalization.IsEnumName<EPeriodType>(name);

        // Assert
        Assert.Equal(expected, result);
    }
}

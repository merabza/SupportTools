using System.Collections.Generic;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;
using LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;
using Xunit;

namespace SupportTools.Tests.DatabaseWork;

public sealed class UnpairedRequiredColumnsFinderTests
{
    [Fact]
    public void Find_WhenRequiredColumnIsPaired_ReturnsNothing()
    {
        // Arrange
        DevTableMeta meta = CreateMeta(("ServerId", "int"));

        // Act
        (List<string> zeroFilled, List<string> unfillable) =
            UnpairedRequiredColumnsFinder.Find(CreatePairedTable("ServerId"), meta);

        // Assert
        Assert.Empty(zeroFilled);
        Assert.Empty(unfillable);
    }

    [Fact]
    public void Find_WhenNumericColumnIsNotPaired_FillsItWithZero()
    {
        // Arrange
        DevTableMeta meta = CreateMeta(("ServerId", "int"), ("OpenPort", "bit"), ("Amount", "decimal"));

        // Act
        (List<string> zeroFilled, List<string> unfillable) =
            UnpairedRequiredColumnsFinder.Find(CreatePairedTable("ServerId"), meta);

        // Assert
        Assert.Equal(["Amount", "OpenPort"], zeroFilled);
        Assert.Empty(unfillable);
    }

    [Fact]
    public void Find_WhenOtherTypeColumnIsNotPaired_ReportsItWithItsType()
    {
        // Arrange
        DevTableMeta meta = CreateMeta(("Name", "nvarchar"), ("Created", "datetime2"));

        // Act
        (List<string> zeroFilled, List<string> unfillable) =
            UnpairedRequiredColumnsFinder.Find(CreatePairedTable(), meta);

        // Assert
        Assert.Empty(zeroFilled);
        Assert.Equal(["Created (datetime2)", "Name (nvarchar)"], unfillable);
    }

    //the pairs file and the Dev schema are compared case-sensitively elsewhere too
    [Fact]
    public void Find_ComparesDevFieldNamesCaseSensitively()
    {
        // Arrange
        DevTableMeta meta = CreateMeta(("OpenPort", "bit"));

        // Act
        (List<string> zeroFilled, _) = UnpairedRequiredColumnsFinder.Find(CreatePairedTable("openPort"), meta);

        // Assert
        Assert.Equal(["OpenPort"], zeroFilled);
    }

    private static DevTableMeta CreateMeta(params (string Column, string TypeName)[] columns)
    {
        var meta = new DevTableMeta();
        foreach ((string column, string typeName) in columns)
        {
            meta.NotNullWithoutDefaultColumns[column] = typeName;
        }

        return meta;
    }

    private static PairedTable CreatePairedTable(params string[] devFieldNames)
    {
        var pairedFields = new Dictionary<string, PairedField>();
        foreach (string name in devFieldNames)
        {
            pairedFields[name] = new PairedField(name, name);
        }

        return new PairedTable("dbo", "ServerInfos", "dbo", "ServerInfos", pairedFields);
    }
}

using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.DatabaseWork;

public sealed class ProdCopyTableReaderTests
{
    private static readonly PairedField[] Fields = [new("id", "Id"), new("IncorrectPhrase", "IncorrectPhrase")];

    [Fact]
    public void BuildSelectSql_ForMariaDb_QuotesNamesWithBackticksWithoutSchema()
    {
        // Act
        string sql = ProdCopyTableReader.BuildSelectSql(EDatabaseProvider.MariaDb,
            CreatePairedTable(string.Empty, "errdb_erratamain"), Fields);

        // Assert
        Assert.Equal("SELECT `id`, `IncorrectPhrase` FROM `errdb_erratamain`", sql);
    }

    [Fact]
    public void BuildSelectSql_ForSqlServer_QuotesNamesWithBracketsAndKeepsTheSchema()
    {
        // Act
        string sql =
            ProdCopyTableReader.BuildSelectSql(EDatabaseProvider.SqlServer, CreatePairedTable("dbo", "Errata"), Fields);

        // Assert
        Assert.Equal("SELECT [id], [IncorrectPhrase] FROM [dbo].[Errata]", sql);
    }

    [Fact]
    public void BuildSelectSql_ForOleDb_QuotesNamesWithBracketsWithoutSchema()
    {
        // Act
        string sql = ProdCopyTableReader.BuildSelectSql(EDatabaseProvider.OleDb,
            CreatePairedTable(string.Empty, "Errata"), Fields);

        // Assert
        Assert.Equal("SELECT [id], [IncorrectPhrase] FROM [Errata]", sql);
    }

    //rows that the new schema no longer has (e.g. the gits of a removed role) are left in ProdCopy
    [Fact]
    public void BuildSelectSql_WhenRowFilterIsSet_AddsItAsWhere()
    {
        // Arrange
        PairedTable pairedTable = CreatePairedTable("dbo", "Errata", "[Kind] <> N'ScaffoldSeed'");

        // Act
        string sql = ProdCopyTableReader.BuildSelectSql(EDatabaseProvider.SqlServer, pairedTable, Fields);

        // Assert
        Assert.Equal("SELECT [id], [IncorrectPhrase] FROM [dbo].[Errata] WHERE [Kind] <> N'ScaffoldSeed'", sql);
    }

    //the field editor stores an empty text when the filter is cleared
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildSelectSql_WhenRowFilterIsBlank_ReadsEveryRow(string rowFilter)
    {
        // Act
        string sql = ProdCopyTableReader.BuildSelectSql(EDatabaseProvider.SqlServer,
            CreatePairedTable("dbo", "Errata", rowFilter), Fields);

        // Assert
        Assert.Equal("SELECT [id], [IncorrectPhrase] FROM [dbo].[Errata]", sql);
    }

    //a NOT NULL column of Dev that ProdCopy does not have yet (e.g. ServerInfos.OpenPort)
    [Fact]
    public void BuildSelectSql_WithZeroFilledColumns_SelectsZeroUnderTheDevName()
    {
        // Act
        string sql = ProdCopyTableReader.BuildSelectSql(EDatabaseProvider.SqlServer, CreatePairedTable("dbo", "Errata"),
            Fields, ["OpenPort"]);

        // Assert
        Assert.Equal("SELECT [id], [IncorrectPhrase], 0 AS [OpenPort] FROM [dbo].[Errata]", sql);
    }

    private static PairedTable CreatePairedTable(string prodCopySchemaName, string prodCopyTableName,
        string? prodCopyRowFilter = null)
    {
        return new PairedTable(prodCopySchemaName, prodCopyTableName, "dbo", "Errata", [])
        {
            ProdCopyRowFilter = prodCopyRowFilter
        };
    }
}

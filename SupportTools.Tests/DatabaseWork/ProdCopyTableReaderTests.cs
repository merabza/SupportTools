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

    private static PairedTable CreatePairedTable(string prodCopySchemaName, string prodCopyTableName)
    {
        return new PairedTable(prodCopySchemaName, prodCopyTableName, "dbo", "Errata", []);
    }
}

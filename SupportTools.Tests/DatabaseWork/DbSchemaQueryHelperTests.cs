using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.DatabaseWork;

public sealed class DbSchemaQueryHelperTests
{
    //Access-ისა და MariaDB-ის ცხრილები ცარიელი სქემით იკითხება და Dev ცხრილებს მხოლოდ სახელით უწყვილდება
    [Theory]
    [InlineData(EDatabaseProvider.SqlServer, true)]
    [InlineData(EDatabaseProvider.OleDb, false)]
    [InlineData(EDatabaseProvider.MariaDb, false)]
    public void UsesSchemas_ForProvider_TellsWhetherTablesHaveSchemas(EDatabaseProvider dataProvider, bool expected)
    {
        // Act
        bool usesSchemas = DbSchemaQueryHelper.UsesSchemas(dataProvider);

        // Assert
        Assert.Equal(expected, usesSchemas);
    }
}

using System.Collections.Generic;
using System.Linq;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using SystemTools.DatabaseToolsShared;
using Xunit;

namespace SupportTools.Tests.DatabaseWork;

//the pairer writes its warnings to the console
[Collection(ConsoleCaptureCollection.Name)]
public sealed class PairedDbObjectsPairerTests
{
    private readonly Dictionary<(string, string), TableInfo> _devTables = [];
    private readonly Dictionary<string, PairedTable> _existing = [];
    private readonly Dictionary<(string, string), TableInfo> _prodCopyTables = [];

    [Fact]
    public void Pair_WithoutExistingPairs_PairsTablesAndFieldsByNameIgnoringCase()
    {
        // Arrange
        AddTable(_prodCopyTables, "dbo", "Customers", "Id", "Name", "OldColumn");
        AddTable(_devTables, "dbo", "CUSTOMERS", "ID", "name", "NewColumn");
        AddTable(_prodCopyTables, "dbo", "OnlyInProdCopy", "Id");

        // Act
        Dictionary<string, PairedTable> result = Pair();

        // Assert
        PairedTable customers = Assert.Single(result).Value;
        Assert.Equal("dbo.Customers - dbo.CUSTOMERS", result.Keys.Single());
        Assert.Equal(["Id - ID", "Name - name"], customers.PairedFields.Keys);
        Assert.False(customers.Locked);
    }

    //an unlocked table is paired again: its settings come back to the defaults, only its locked fields stay
    [Fact]
    public void Pair_WhenTableIsUnlocked_RecreatesItAndKeepsOnlyItsLockedFields()
    {
        // Arrange
        AddTable(_prodCopyTables, "dbo", "Customers", "Id", "Name", "Code");
        AddTable(_devTables, "dbo", "Customers", "Id", "Name", "Code", "ClientName");
        var old = new PairedTable("dbo", "Customers", "dbo", "Customers",
            new Dictionary<string, PairedField>
            {
                ["Name - ClientName"] = new("Name", "ClientName") { Locked = true },
                ["Code - Name"] = new("Code", "Name")
            })
        {
            SeedDataType = ESeedDataType.OnlySeederRules, KeyFieldNames = ["Code"]
        };
        _existing[old.GetItemKey()] = old;

        // Act
        PairedTable customers = Assert.Single(Pair()).Value;

        // Assert
        Assert.NotSame(old, customers);
        Assert.Equal(ESeedDataType.OnlyDatabase, customers.SeedDataType);
        Assert.Empty(customers.KeyFieldNames);
        //Name is taken by the locked field, so Dev Name stays free and is not paired with anything else
        Assert.Equal(["Id - Id", "Name - ClientName", "Code - Code"], customers.PairedFields.Keys);
        Assert.Same(old.PairedFields["Name - ClientName"], customers.PairedFields["Name - ClientName"]);
    }

    //a locked table keeps its manual pairing and settings; its unlocked fields are paired again
    [Fact]
    public void Pair_WhenTableIsLocked_KeepsItAndPairsItsUnlockedFieldsAgain()
    {
        // Arrange
        AddTable(_prodCopyTables, "dbo", "Customers", "Id", "Name", "Code");
        AddTable(_devTables, "dbo", "Customers", "Id");
        AddTable(_devTables, "dbo", "Clients", "Id", "Name", "Code", "Title");
        AddTable(_prodCopyTables, "dbo", "Clients", "Id");
        var locked = new PairedTable("dbo", "Customers", "dbo", "Clients",
            new Dictionary<string, PairedField>
            {
                ["Name - Title"] = new("Name", "Title") { Locked = true },
                ["Id - Code"] = new("Id", "Code")
            })
        {
            Locked = true, SeedDataType = ESeedDataType.OnlySeederRules, ProdCopyRowFilter = "[Id] > 0"
        };
        _existing["OldKey"] = locked;

        // Act
        Dictionary<string, PairedTable> result = Pair();

        // Assert
        //neither dbo.Customers - dbo.Customers nor dbo.Clients - dbo.Clients is created: both tables are taken
        Assert.Equal(["OldKey"], result.Keys);
        Assert.Same(locked, result["OldKey"]);
        Assert.Equal(ESeedDataType.OnlySeederRules, locked.SeedDataType);
        Assert.Equal("[Id] > 0", locked.ProdCopyRowFilter);
        Assert.Equal(["Id - Id", "Name - Title", "Code - Code"], locked.PairedFields.Keys);
    }

    [Fact]
    public void Pair_WhenLockedTableIsMissing_KeepsItWithOnlyItsLockedFields()
    {
        // Arrange
        AddTable(_prodCopyTables, "dbo", "Customers", "Id");
        AddTable(_devTables, "dbo", "Customers", "Id");
        var locked = new PairedTable("dbo", "Removed", "dbo", "Removed",
            new Dictionary<string, PairedField>
            {
                ["A - A"] = new("A", "A") { Locked = true }, ["B - B"] = new("B", "B")
            }) { Locked = true };
        _existing[locked.GetItemKey()] = locked;

        // Act
        Dictionary<string, PairedTable> result = Pair();

        // Assert
        Assert.Equal(["dbo.Customers - dbo.Customers", "dbo.Removed - dbo.Removed"], result.Keys);
        Assert.Equal(["A - A"], locked.PairedFields.Keys);
    }

    //a locked field whose ProdCopy column is gone still stays
    [Fact]
    public void Pair_WhenLockedFieldColumnIsGone_KeepsTheField()
    {
        // Arrange
        AddTable(_prodCopyTables, "dbo", "Customers", "Id");
        AddTable(_devTables, "dbo", "Customers", "Id");
        var old = new PairedTable("dbo", "Customers", "dbo", "Customers",
            new Dictionary<string, PairedField> { ["Gone - Gone"] = new("Gone", "Gone") { Locked = true } });
        _existing[old.GetItemKey()] = old;

        // Act
        PairedTable customers = Assert.Single(Pair()).Value;

        // Assert
        Assert.Equal(["Id - Id", "Gone - Gone"], customers.PairedFields.Keys);
    }

    //an unlocked table that is not paired again disappears with its locked fields (a warning names it)
    [Fact]
    public void Pair_WhenUnlockedTableIsNotPairedAgain_DropsIt()
    {
        // Arrange
        AddTable(_prodCopyTables, "dbo", "Customers", "Id");
        AddTable(_devTables, "dbo", "Customers", "Id");
        var manual = new PairedTable("dbo", "Customers", "dbo", "Clients",
            new Dictionary<string, PairedField> { ["Id - Id"] = new("Id", "Id") { Locked = true } });
        _existing[manual.GetItemKey()] = manual;

        // Act
        Dictionary<string, PairedTable> result = Pair();

        // Assert
        Assert.Equal(["dbo.Customers - dbo.Customers"], result.Keys);
    }

    //Access and MariaDB have no schemas: the Dev tables are keyed by the table name only
    [Fact]
    public void Pair_WhenProdCopyHasNoSchemas_FindsTheLockedDevTableByNameOnly()
    {
        // Arrange
        _prodCopyTables[(string.Empty, "CUSTOMERS")] = CreateTable(string.Empty, "Customers", "Id", "Name");
        _devTables[(string.Empty, "CLIENTS")] = CreateTable("dbo", "Clients", "Id", "Name");
        _devTables[(string.Empty, "CUSTOMERS")] = CreateTable("dbo", "Customers", "Id");
        var locked = new PairedTable(string.Empty, "Customers", "dbo", "Clients", []) { Locked = true };
        _existing[locked.GetItemKey()] = locked;

        // Act
        Dictionary<string, PairedTable> result = Pair(false);

        // Assert
        Assert.Same(locked, Assert.Single(result).Value);
        Assert.Equal(["Id - Id", "Name - Name"], locked.PairedFields.Keys);
    }

    private Dictionary<string, PairedTable> Pair(bool prodCopyUsesSchemas = true)
    {
        return PairedDbObjectsPairer.Pair(_prodCopyTables, _devTables, prodCopyUsesSchemas, _existing, null);
    }

    private static void AddTable(Dictionary<(string, string), TableInfo> tables, string schemaName,
        string tableName, params string[] columns)
    {
        tables[(schemaName.ToUpperInvariant(), tableName.ToUpperInvariant())] =
            CreateTable(schemaName, tableName, columns);
    }

    private static TableInfo CreateTable(string schemaName, string tableName, params string[] columns)
    {
        var table = new TableInfo(schemaName, tableName);
        table.Columns.AddRange(columns);
        return table;
    }
}

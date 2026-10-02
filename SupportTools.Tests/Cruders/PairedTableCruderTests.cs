using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.CliParameters.FieldEditors;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportTools.FieldEditors;
using SupportToolsData.Models;
using SystemTools.DatabaseToolsShared;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Cruders;

public sealed class PairedTableCruderTests
{
    private const string CustomersKey = "dbo.Customers - dbo.Clients";
    private const string DevConnectionString = "Fake dev connection";
    private const string ProdCopyConnectionString = "Fake prod copy connection";

    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly SupportToolsParameters _parameters = new();
    private readonly Dictionary<string, PairedTable> _tables = [];

    public PairedTableCruderTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public void Constructor_WhenCalled_NamesThePairedTableRecords()
    {
        // Act
        PairedTableCruder sut = CreateSut();

        // Assert
        Assert.Equal("Paired Table", sut.CrudName);
        Assert.Equal("Paired Tables", sut.CrudNamePlural);
    }

    [Fact]
    public void Constructor_WhenCalled_EditsEveryFieldOfAPairedTable()
    {
        // Act
        List<FieldEditor> fieldEditors = CliMenuTestAccess.GetFieldEditors(CreateSut());

        // Assert
        Assert.Equal(
        [
            nameof(PairedTable.ProdCopySchemaName), nameof(PairedTable.ProdCopyTableName),
            nameof(PairedTable.DevSchemaName), nameof(PairedTable.DevTableName), nameof(PairedTable.SeedDataType),
            nameof(PairedTable.UseOldDataConvertor), nameof(PairedTable.KeyFieldNames),
            nameof(PairedTable.PairedFields)
        ], fieldEditors.Select(x => x.PropertyName));
        Assert.IsType<SchemaNameFieldEditor>(fieldEditors[0]);
        Assert.IsType<TableNameFieldEditor>(fieldEditors[1]);
        Assert.IsType<SchemaNameFieldEditor>(fieldEditors[2]);
        Assert.IsType<TableNameFieldEditor>(fieldEditors[3]);
        Assert.IsType<EnumFieldEditor<ESeedDataType>>(fieldEditors[4]);
        Assert.IsType<BoolFieldEditor>(fieldEditors[5]);
        Assert.IsType<SimpleNamesListFieldEditor<KeyFieldNamesLisCruder>>(fieldEditors[6]);
        Assert.IsType<PairedFieldsListFieldEditor>(fieldEditors[7]);
    }

    //the schema and table names of each side are read from the database of that side
    [Fact]
    public void Constructor_WhenCalled_ReadsTheNamesOfEachSideFromItsOwnDatabase()
    {
        // Act
        List<FieldEditor> fieldEditors = CliMenuTestAccess.GetFieldEditors(CreateSut());

        // Assert
        foreach (FieldEditor prodCopyEditor in fieldEditors.Take(2))
        {
            Assert.Equal("ProdCopy", GetPrivateField(prodCopyEditor, "_sideName"));
            Assert.Equal(EDatabaseProvider.OleDb, GetPrivateField(prodCopyEditor, "_dataProvider"));
            Assert.Equal(ProdCopyConnectionString, GetPrivateField(prodCopyEditor, "_connectionString"));
        }

        foreach (FieldEditor devEditor in fieldEditors.Skip(2).Take(2))
        {
            Assert.Equal("Dev", GetPrivateField(devEditor, "_sideName"));
            Assert.Equal(EDatabaseProvider.SqlServer, GetPrivateField(devEditor, "_dataProvider"));
            Assert.Equal(DevConnectionString, GetPrivateField(devEditor, "_connectionString"));
        }
    }

    //the key field names are a list inside the paired table: editing it saves the whole parameters
    [Fact]
    public async Task KeyFieldNamesEditor_WhenListChanges_SavesTheWholeParameters()
    {
        // Arrange
        var pairedTable = new PairedTable { KeyFieldNames = ["Code"] };
        FieldEditor keyFieldNamesEditor = CliMenuTestAccess.GetFieldEditors(CreateSut())[6];

        // Act
        CliMenuSet? listMenu = keyFieldNamesEditor.GetSubMenu(pairedTable);
        Cruder keyFieldNamesCruder = CliMenuTestAccess.GetListMenuCruder(listMenu!);
        bool saved = await keyFieldNamesCruder.Save("Key field names saved");

        // Assert
        Assert.IsType<KeyFieldNamesLisCruder>(keyFieldNamesCruder);
        Assert.True(keyFieldNamesCruder.ContainsRecordWithKey("Code"));
        Assert.True(saved);
        _parametersManager.Verify(
            x => x.Save(_parameters, "Key field names saved", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    //the record key is built from the fields of the record, so the item menu has no separate record name editor
    [Fact]
    public void GetItemMenu_WhenCalled_HasNoRecordNameEditor()
    {
        // Arrange
        _tables[CustomersKey] = CreateCustomersTable();

        // Act
        CliMenuSet itemMenu = CreateSut().GetItemMenu(CustomersKey);

        // Assert
        List<CliMenuCommand> commands = [.. CliMenuTestAccess.GetMenuItems(itemMenu).Select(x => x.CliMenuCommand)];
        Assert.Empty(commands.OfType<RecordKeyEditorCliMenuCommand>());
        Assert.Single(commands.OfType<EditItemAllFieldsInSequenceCliMenuCommand>());
    }

    [Fact]
    public void Create_WhenCalled_EditsTheGivenTables()
    {
        // Arrange
        _tables[CustomersKey] = CreateCustomersTable();

        // Act
        PairedTableCruder sut = PairedTableCruder.Create(_parametersManager.Object, new Mock<ILogger>().Object,
            _tables, EDatabaseProvider.OleDb, ProdCopyConnectionString, EDatabaseProvider.SqlServer,
            DevConnectionString);

        // Assert
        Assert.Equal("Paired Table", sut.CrudName);
        Assert.True(sut.ContainsRecordWithKey(CustomersKey));
        Assert.Equal(EDatabaseProvider.OleDb,
            GetPrivateField(CliMenuTestAccess.GetFieldEditors(sut)[0], "_dataProvider"));
    }

    //old parameters files keyed the tables by a GUID (or by the ProdCopy side only)
    [Fact]
    public void GetListMenu_WhenKeyIsOutdated_KeysTheRecordByItsFields()
    {
        // Arrange
        PairedTable customers = CreateCustomersTable();
        _tables["2f1d7c2e-0f7a-4a59-9a43-3c1d6f0f5b11"] = customers;

        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.Equal([CustomersKey], _tables.Keys);
        Assert.Same(customers, _tables[CustomersKey]);
        Assert.Equal([CustomersKey], GetListedNames(listMenu));
    }

    [Fact]
    public void GetListMenu_WhenKeyMatchesTheFields_KeepsTheRecord()
    {
        // Arrange
        PairedTable customers = CreateCustomersTable();
        _tables[CustomersKey] = customers;

        // Act
        CreateSut().GetListMenu();

        // Assert
        Assert.Equal([CustomersKey], _tables.Keys);
        Assert.Same(customers, _tables[CustomersKey]);
    }

    [Fact]
    public void GetListMenu_WhenOutdatedKeysCollide_NumbersTheLaterRecords()
    {
        // Arrange
        PairedTable first = CreateCustomersTable();
        PairedTable second = CreateCustomersTable();
        PairedTable third = CreateCustomersTable();
        _tables[CustomersKey] = first;
        _tables["OldKey1"] = second;
        _tables["OldKey2"] = third;

        // Act
        CreateSut().GetListMenu();

        // Assert
        Assert.Equal(3, _tables.Count);
        Assert.Same(first, _tables[CustomersKey]);
        Assert.Same(second, _tables[$"{CustomersKey} (2)"]);
        Assert.Same(third, _tables[$"{CustomersKey} (3)"]);
    }

    //a numbered key already belongs to its record: it is not renumbered again
    [Fact]
    public void GetListMenu_WhenNumberedKeyIsItsOwn_KeepsIt()
    {
        // Arrange
        PairedTable first = CreateCustomersTable();
        PairedTable second = CreateCustomersTable();
        _tables[CustomersKey] = first;
        _tables[$"{CustomersKey} (2)"] = second;

        // Act
        CreateSut().GetListMenu();

        // Assert
        Assert.Equal([CustomersKey, $"{CustomersKey} (2)"], _tables.Keys);
        Assert.Same(second, _tables[$"{CustomersKey} (2)"]);
    }

    [Fact]
    public async Task AddRecordWithKey_WhenCalled_KeysTheRecordByItsFields()
    {
        // Arrange
        PairedTable customers = CreateCustomersTable();

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(CreateSut(), "EnteredKey", customers);

        // Assert
        Assert.Equal([CustomersKey], _tables.Keys);
        Assert.Same(customers, _tables[CustomersKey]);
        _parametersManager.Verify(
            x => x.Save(_parameters, $"record {CustomersKey} Added", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddRecordWithKey_WhenKeyIsTaken_NumbersTheNewRecord()
    {
        // Arrange
        PairedTableCruder sut = CreateSut();
        _tables[CustomersKey] = CreateCustomersTable();
        PairedTable second = CreateCustomersTable();
        PairedTable third = CreateCustomersTable();

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(sut, CustomersKey, second);
        await CliMenuTestAccess.InvokeAddRecordWithKey(sut, CustomersKey, third);

        // Assert
        Assert.Same(second, _tables[$"{CustomersKey} (2)"]);
        Assert.Same(third, _tables[$"{CustomersKey} (3)"]);
    }

    private PairedTableCruder CreateSut()
    {
        return new PairedTableCruder(_parametersManager.Object, _tables, new Mock<ILogger>().Object,
            EDatabaseProvider.OleDb, ProdCopyConnectionString, EDatabaseProvider.SqlServer, DevConnectionString);
    }

    private static PairedTable CreateCustomersTable()
    {
        return new PairedTable("dbo", "Customers", "dbo", "Clients", []);
    }

    private static List<string> GetListedNames(CliMenuSet listMenu)
    {
        return
        [
            .. CliMenuTestAccess.GetMenuItems(listMenu).Select(x => x.CliMenuCommand)
                .OfType<ItemSubMenuCliMenuCommand>().Select(x => x.Name)
        ];
    }

    private static object? GetPrivateField(object instance, string fieldName)
    {
        return instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(instance);
    }
}

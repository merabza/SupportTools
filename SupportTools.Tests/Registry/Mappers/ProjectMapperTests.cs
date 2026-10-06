using System;
using System.Collections.Generic;
using System.Linq;
using LibSupportToolsServerWork.Registry.Mappers;
using LibSupportToolsServerWork.Registry.Paths;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class ProjectMapperTests
{
    private const string LinuxFolder = "/home/u/1WorkDotnet/AppFake";

    [Fact]
    public void ToLocal_WhenContractComesFromRealisticProject_RestoresEveryField()
    {
        // Arrange
        ProjectModel original = ProjectTestData.NewProject();
        PathMapper pathMapper = MapperTestHelpers.WindowsPathMapper();
        StsProjectDataModel contract = ProjectMapper.ToContract(ProjectTestData.Name, original, pathMapper);

        // Act
        ProjectModel restored = ProjectMapper.ToLocal(contract, null, pathMapper);

        // Assert
        Assert.Equal(MapperTestHelpers.JsonOf(original), MapperTestHelpers.JsonOf(restored));
    }

    [Fact]
    public void ToContract_WhenCalled_MapsNamesKeysAndRenamedFields()
    {
        // Act
        StsProjectDataModel result = ProjectMapper.ToContract(ProjectTestData.Name, ProjectTestData.NewProject(),
            MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Equal(ProjectTestData.Name, result.Name);
        Assert.Equal("IsService", result.ProjectType);
        Assert.Equal(["SeedData", "GenerateApiRoutes"], result.AllowToolsList);
        Assert.Equal("v2", Assert.Single(result.RouteClasses).ApiVersion);
        Assert.Equal(["GetItems", "SaveItem"], result.Endpoints.Select(x => x.Name));
        Assert.Equal(("Get", "Query"), (result.Endpoints[0].HttpMethod, result.Endpoints[0].EndpointType));
        Assert.Equal("Diff", result.DevDatabaseParameters?.BackupType);
        Assert.Equal("Simple", result.DevDatabaseParameters?.DatabaseRecoveryModel);
        StsServerInfoDataModel prod = result.ServerInfos[0];
        Assert.Equal(("PAZISI", "Production"), (prod.ServerName, prod.EnvironmentName));
        Assert.Equal(["ProgramUpdater", "AppSettingsUpdater"], prod.AllowToolsList);
        Assert.Null(result.ServerInfos[1].CurrentDatabaseParameters);
    }

    //canonical (Windows) → Linux local → canonical gives the original contract
    [Fact]
    public void ToContract_WhenLinuxComputerPushesWhatItPulled_GivesTheOriginalCanonicalContract()
    {
        // Arrange
        PathMapper linux = MapperTestHelpers.LinuxPathMapper();
        StsProjectDataModel canonical = ProjectMapper.ToContract(ProjectTestData.Name, ProjectTestData.NewProject(),
            MapperTestHelpers.WindowsPathMapper());
        string canonicalJson = MapperTestHelpers.JsonOf(canonical);
        ProjectModel linuxProject = ProjectMapper.ToLocal(canonical, null, linux);

        // Act
        StsProjectDataModel result = ProjectMapper.ToContract(ProjectTestData.Name, linuxProject, linux);

        // Assert
        Assert.Equal(LinuxFolder, linuxProject.ProjectFolderName);
        Assert.Equal($"{LinuxFolder}/AppFake/AppFake.slnx", linuxProject.SolutionFileName);
        Assert.Equal("/home/u/1WorkDotnet/Security/AppFake/appsettings.Production.json",
            linuxProject.ServerInfos[ProjectTestData.ProdKey].AppSettingsJsonSourceFileName);
        Assert.Equal($"{LinuxFolder}/Encoded/appsettings.Test.json",
            linuxProject.ServerInfos[ProjectTestData.TestKey].AppSettingsEncodedJsonFileName);
        Assert.Equal(canonicalJson, MapperTestHelpers.JsonOf(result));
        Assert.Empty(linux.Issues);
    }

    //the ServerInfo keys are local: an existing one keeps its key (found by server and environment, ignoring case),
    //a new one gets "Server|Env", a missing one is removed
    [Fact]
    public void ToLocal_WhenProjectExists_KeepsTheServerInfoKeysAndGivesNewOnesTheServerEnvForm()
    {
        // Arrange
        const string guidKey = "0b5f6a54-0000-4000-8000-fake00000001";
        ProjectModel existing = ProjectTestData.NewProject();
        ServerInfoModel prod = existing.ServerInfos[ProjectTestData.ProdKey];
        existing.ServerInfos.Clear();
        existing.ServerInfos[guidKey] = prod;
        existing.ServerInfos["Old|Gone"] = new ServerInfoModel { ServerName = "Old", EnvironmentName = "Gone" };
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        contract.ServerInfos[0].ServerName = "pazisi";
        contract.ServerInfos[0].EnvironmentName = "PRODUCTION";
        contract.ServerInfos[0].ServerSidePort = 6060;

        // Act
        ProjectModel result = ProjectMapper.ToLocal(contract, existing, MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Equal([guidKey, ProjectTestData.TestKey], result.ServerInfos.Keys);
        Assert.Same(prod, result.ServerInfos[guidKey]);
        Assert.Equal(("pazisi", 6060), (prod.ServerName, prod.ServerSidePort));
    }

    //an existing record with another server and environment can hold the "Server|Env" key: the new one gets a GUID
    [Fact]
    public void ToLocal_WhenTheServerEnvKeyIsTaken_GivesTheNewServerInfoAGuidKey()
    {
        // Arrange
        ProjectModel existing = ProjectTestData.NewProject();
        existing.ServerInfos.Clear();
        existing.ServerInfos[ProjectTestData.TestKey] =
            new ServerInfoModel { ServerName = "PAZISI", EnvironmentName = "Production" };
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());

        // Act
        ProjectModel result = ProjectMapper.ToLocal(contract, existing, MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Equal("PAZISI", result.ServerInfos[ProjectTestData.TestKey].ServerName);
        string newKey = Assert.Single(result.ServerInfos.Keys, x => x != ProjectTestData.TestKey);
        Assert.True(Guid.TryParse(newKey, out _));
        Assert.Equal("Merinson", result.ServerInfos[newKey].ServerName);
    }

    //the settable fields change in place: the object, its lists and dictionaries stay, so open editors keep them
    [Fact]
    public void ToLocal_WhenOnlySettableFieldsChange_UpdatesTheSameObject()
    {
        // Arrange
        ProjectModel existing = ProjectTestData.NewProject();
        List<string> gitProjectNames = existing.GitProjectNames;
        Dictionary<string, EndpointModel> endpoints = existing.Endpoints;
        DatabaseParameters? devDatabaseParameters = existing.DevDatabaseParameters;
        DatabaseParameters? prodCopyDatabaseParameters = existing.ProdCopyDatabaseParameters;
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        contract.ProjectDescription = "Changed";
        contract.GitProjectNames = ["Other"];
        contract.DevDatabaseParameters!.DatabaseName = "ChangedDb";
        contract.ProdCopyDatabaseParameters!.DatabaseName = "ChangedProdCopyDb";

        // Act
        ProjectModel result = ProjectMapper.ToLocal(contract, existing, MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Same(existing, result);
        Assert.Same(gitProjectNames, result.GitProjectNames);
        Assert.Same(endpoints, result.Endpoints);
        Assert.Same(devDatabaseParameters, result.DevDatabaseParameters);
        Assert.Same(prodCopyDatabaseParameters, result.ProdCopyDatabaseParameters);
        Assert.Equal(("Changed", "ChangedDb", "ChangedProdCopyDb"),
            (result.ProjectDescription, devDatabaseParameters?.DatabaseName, prodCopyDatabaseParameters?.DatabaseName));
        Assert.Equal(["Other"], gitProjectNames);
    }

    //each init-only value alone decides: when only one of them differs, the project is replaced
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void ToLocal_WhenOneInitOnlyValueChanges_CreatesANewObject(int caseNumber)
    {
        // Arrange
        ProjectModel existing = ProjectTestData.NewProject();
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        switch (caseNumber)
        {
            case 0:
                contract.ProjectType = nameof(EProjectType.Standard);
                break;
            case 1:
                contract.UseAlternativeWebAgent = false;
                break;
            case 2:
                contract.DevDatabaseParameters = null;
                break;
            case 3:
                contract.ProdCopyDatabaseParameters = null;
                break;
            case 4:
                contract.ProjectGroupName = "Other Group";
                break;
            default:
                contract.KeyGuidPart = "11111111-fake-key-guid-part";
                break;
        }

        // Act
        ProjectModel result = ProjectMapper.ToLocal(contract, existing, MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.NotSame(existing, result);
        Assert.Equal(MapperTestHelpers.JsonOf(contract),
            MapperTestHelpers.JsonOf(ProjectMapper.ToContract(ProjectTestData.Name, result,
                MapperTestHelpers.WindowsPathMapper())));
    }

    //the database parameters that appear get a new object
    [Fact]
    public void ToLocal_WhenDatabaseParametersAppear_CreatesThem()
    {
        // Arrange
        ProjectModel existing = ProjectTestData.NewProject();
        var withoutParameters = new ProjectModel { ProjectType = existing.ProjectType };
        StsProjectDataModel contract = ContractOf(existing);

        // Act
        ProjectModel result = ProjectMapper.ToLocal(contract, withoutParameters, MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Equal(("AppFakeDev", "AppFakeProdCopy"),
            (result.DevDatabaseParameters?.DatabaseName, result.ProdCopyDatabaseParameters?.DatabaseName));
    }

    //the endpoints and route classes are replaced as a whole inside the same dictionaries: a kept record keeps its
    //object, a missing one is removed and missing texts become empty texts of the local model
    [Fact]
    public void ToLocal_WhenEndpointsAndRouteClassesChange_ReplacesThemInTheSameDictionaries()
    {
        // Arrange
        ProjectModel existing = ProjectTestData.NewProject();
        EndpointModel getItems = existing.Endpoints["GetItems"];
        RouteClassModel items = existing.RouteClasses["Items"];
        existing.RouteClasses["Users"] = new RouteClassModel { Base = "users" };
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        contract.Endpoints.RemoveAt(1);
        contract.Endpoints[0].EndpointRoute = "items/all";
        contract.Endpoints.Add(new StsProjectEndpointDataModel
        {
            Name = "DeleteItem", HttpMethod = "delete", EndpointType = "command"
        });
        contract.RouteClasses[0].Base = "goods";
        contract.RouteClasses.Add(new StsProjectRouteClassDataModel { Name = "Empty" });

        // Act
        ProjectModel result = ProjectMapper.ToLocal(contract, existing, MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Equal(["GetItems", "DeleteItem"], result.Endpoints.Keys);
        Assert.Same(getItems, result.Endpoints["GetItems"]);
        Assert.Equal("items/all", getItems.EndpointRoute);
        EndpointModel deleteItem = result.Endpoints["DeleteItem"];
        Assert.Equal((EHttpMethod.Delete, EEndpointType.Command), (deleteItem.HttpMethod, deleteItem.EndpointType));
        Assert.Equal((string.Empty, string.Empty, string.Empty),
            (deleteItem.EndpointName, deleteItem.EndpointRoute, deleteItem.ReturnType));
        Assert.Equal(["Items", "Empty"], result.RouteClasses.Keys);
        Assert.Same(items, result.RouteClasses["Items"]);
        Assert.Equal("goods", items.Base);
        RouteClassModel empty = result.RouteClasses["Empty"];
        Assert.Equal((string.Empty, string.Empty, string.Empty), (empty.Root, empty.Version, empty.Base));
    }

    //the server keeps the names as they came, so another case still maps to the enums
    [Fact]
    public void ToLocal_WhenEnumNamesHaveAnotherCase_ParsesThem()
    {
        // Arrange
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        contract.ProjectType = "ispackage";
        contract.AllowToolsList = ["seeddata"];
        contract.Endpoints[0].HttpMethod = "patch";
        contract.Endpoints[0].EndpointType = "command";

        // Act
        ProjectModel result = ProjectMapper.ToLocal(contract, null, MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Equal(EProjectType.IsPackage, result.ProjectType);
        Assert.Equal([EProjectTools.SeedData], result.AllowToolsList);
        Assert.Equal((EHttpMethod.Patch, EEndpointType.Command),
            (result.Endpoints["GetItems"].HttpMethod, result.Endpoints["GetItems"].EndpointType));
    }

    //an init-only field can change only with a new object; it takes over the lists and dictionaries of the old one
    [Fact]
    public void ToLocal_WhenAnInitOnlyFieldChanges_CreatesANewObjectWithTheSameCollections()
    {
        // Arrange
        ProjectModel existing = ProjectTestData.NewProject();
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        contract.ProjectFolderName = @"D:\1WorkDotnet\Moved";
        contract.ProdCopyDatabaseParameters = null;

        // Act
        ProjectModel result = ProjectMapper.ToLocal(contract, existing, MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.NotSame(existing, result);
        Assert.Equal(@"D:\1WorkDotnet\Moved", result.ProjectFolderName);
        Assert.Null(result.ProdCopyDatabaseParameters);
        Assert.Same(existing.RedundantFileNames, result.RedundantFileNames);
        Assert.Same(existing.FrontNpmPackageNames, result.FrontNpmPackageNames);
        Assert.Same(existing.Endpoints, result.Endpoints);
        Assert.Same(existing.RouteClasses, result.RouteClasses);
        Assert.Same(existing.GitProjectNames, result.GitProjectNames);
        Assert.Same(existing.ScaffoldSeederGitProjectNames, result.ScaffoldSeederGitProjectNames);
        Assert.Same(existing.ServerInfos, result.ServerInfos);
        Assert.Same(existing.AllowToolsList, result.AllowToolsList);
        Assert.Same(existing.DevDatabaseParameters, result.DevDatabaseParameters);
    }

    //a ServerInfo whose database parameters appear or disappear is replaced under its key
    [Fact]
    public void ToLocal_WhenServerInfoDatabaseParametersAppear_ReplacesTheServerInfoUnderTheSameKey()
    {
        // Arrange
        ProjectModel existing = ProjectTestData.NewProject();
        ServerInfoModel test = existing.ServerInfos[ProjectTestData.TestKey];
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        contract.ServerInfos[1].NewDatabaseParameters = new StsDatabaseParametersDataModel { DatabaseName = "New" };

        // Act
        ProjectModel result = ProjectMapper.ToLocal(contract, existing, MapperTestHelpers.WindowsPathMapper());

        // Assert
        ServerInfoModel replaced = result.ServerInfos[ProjectTestData.TestKey];
        Assert.NotSame(test, replaced);
        Assert.Equal("New", replaced.NewDatabaseParameters?.DatabaseName);
        Assert.Same(test.AllowToolsList, replaced.AllowToolsList);
    }

    //README §4.6: a field that is still local stays unchanged on a pull. Today every field of the aggregate is in the
    //contract (RouteClassModel.Version is the contract's ApiVersion), so the list holds only that one. A new local
    //field must be added here and kept by ProjectMapper.ToLocal: in place it stays by itself, a new object of
    //CreateWithInitOnlyValues has to take it over
    [Fact]
    public void LocalModels_WhenComparedWithTheContracts_HaveNoFieldThatTheMapperDoesNotKnow()
    {
        // Act
        List<string> result =
        [
            .. FieldsNotInContract(typeof(ProjectModel), typeof(StsProjectDataModel)),
            .. FieldsNotInContract(typeof(ServerInfoModel), typeof(StsServerInfoDataModel)),
            .. FieldsNotInContract(typeof(DatabaseParameters), typeof(StsDatabaseParametersDataModel)),
            .. FieldsNotInContract(typeof(EndpointModel), typeof(StsProjectEndpointDataModel)),
            .. FieldsNotInContract(typeof(RouteClassModel), typeof(StsProjectRouteClassDataModel))
        ];

        // Assert
        Assert.Equal(["RouteClassModel.Version"], result);
    }

    //the same content always gives the same hash: what was pulled hashes like the server record, the order of the sets
    //and of the ServerInfos does not matter, "" is null and the drive letter is upper case
    [Fact]
    public void Normalize_WhenContentIsTheSame_GivesTheSameHash()
    {
        // Arrange
        PathMapper pathMapper = MapperTestHelpers.WindowsPathMapper();
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        string hash = HashOf(contract);
        StsProjectDataModel pulled =
            ProjectMapper.ToContract(ProjectTestData.Name, ProjectMapper.ToLocal(contract, null, pathMapper),
                pathMapper);
        StsProjectDataModel reordered = ContractOf(ProjectTestData.NewProject());
        reordered.ServerInfos[0].ServiceUserName = "";
        reordered.GitProjectNames.Reverse();
        reordered.AllowToolsList.Reverse();
        reordered.Endpoints.Reverse();
        reordered.ServerInfos.Reverse();
        reordered.ServerInfos[1].AllowToolsList.Reverse();
        reordered.SolutionFileName = "d" + reordered.SolutionFileName![1..];
        StsProjectDataModel withoutUserName = ContractOf(ProjectTestData.NewProject());
        withoutUserName.ServerInfos[0].ServiceUserName = null;

        // Act
        string pulledHash = HashOf(pulled);
        string normalizedAgainHash = MapperTestHelpers.HashOf(ProjectMapper.Normalize(ProjectMapper.Normalize(
            ContractOf(ProjectTestData.NewProject()))));

        // Assert
        Assert.Equal(hash, pulledHash);
        Assert.Equal(hash, normalizedAgainHash);
        Assert.Equal(HashOf(withoutUserName), HashOf(reordered));
    }

    [Fact]
    public void Normalize_WhenEnumNamesHaveAnotherCase_GivesTheCanonicalNames()
    {
        // Arrange
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        contract.ProjectType = "isservice";
        contract.AllowToolsList = ["seeddata"];
        contract.Endpoints[0].HttpMethod = "GET";
        contract.ServerInfos[0].AllowToolsList = ["versionchecker"];
        contract.DevDatabaseParameters!.BackupType = "trlog";

        // Act
        StsProjectDataModel result = ProjectMapper.Normalize(contract);

        // Assert
        Assert.Equal("IsService", result.ProjectType);
        Assert.Equal(["SeedData"], result.AllowToolsList);
        Assert.Equal("Get", result.Endpoints[0].HttpMethod);
        Assert.Equal(["VersionChecker"], result.ServerInfos[0].AllowToolsList);
        Assert.Equal("TrLog", result.DevDatabaseParameters?.BackupType);
    }

    [Fact]
    public void FindUnknownEnumField_WhenEveryNameIsKnown_ReturnsNull()
    {
        // Act
        string? result = ProjectMapper.FindUnknownEnumField(ContractOf(ProjectTestData.NewProject()));

        // Assert
        Assert.Null(result);
    }

    //a name that a newer client wrote and this one does not know (e.g. a new tool)
    [Theory]
    [InlineData(0, "ProjectType")]
    [InlineData(1, "AllowToolsList")]
    [InlineData(2, "Endpoints.SaveItem")]
    [InlineData(3, "DevDatabaseParameters.BackupType")]
    [InlineData(4, "ProdCopyDatabaseParameters.DatabaseRecoveryModel")]
    [InlineData(5, "ServerInfos.Merinson|Test.AllowToolsList")]
    [InlineData(6, "ServerInfos.PAZISI|Production.NewDatabaseParameters.BackupType")]
    [InlineData(7, "Endpoints.GetItems")]
    [InlineData(8, "ServerInfos.PAZISI|Production.AllowToolsList")]
    public void FindUnknownEnumField_WhenANameIsUnknown_ReturnsItsField(int caseNumber, string expected)
    {
        // Arrange
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        switch (caseNumber)
        {
            case 0:
                contract.ProjectType = "IsRobot";
                break;
            case 1:
                contract.AllowToolsList.Add("FutureTool");
                break;
            case 2:
                contract.Endpoints[1].EndpointType = "Event";
                break;
            case 3:
                contract.DevDatabaseParameters!.BackupType = "Incremental";
                break;
            case 4:
                contract.ProdCopyDatabaseParameters!.DatabaseRecoveryModel = "Instant";
                break;
            case 5:
                contract.ServerInfos[1].AllowToolsList.Add("FutureServerTool");
                break;
            case 6:
                contract.ServerInfos[0].NewDatabaseParameters!.BackupType = "Snapshot";
                break;
            case 7:
                contract.Endpoints[0].HttpMethod = "Trace";
                break;
            default:
                //both ServerInfos: the first in the order of the contract is named
                contract.ServerInfos[0].AllowToolsList.Add("FutureServerTool");
                contract.ServerInfos[1].AllowToolsList.Add("FutureServerTool");
                break;
        }

        // Act
        string? result = ProjectMapper.FindUnknownEnumField(contract);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FindRepeatedValues_WhenNothingRepeats_ReturnsNothing()
    {
        // Act
        List<string> result = ProjectMapper.FindRepeatedValues(ContractOf(ProjectTestData.NewProject()));

        // Assert
        Assert.Empty(result);
    }

    //two local ServerInfos of one server and environment (also in another case) and names that differ only by case
    [Fact]
    public void FindRepeatedValues_WhenValuesRepeatIgnoringCase_DescribesEveryRepetition()
    {
        // Arrange
        ProjectModel project = ProjectTestData.NewProject();
        project.ServerInfos["8f9e0000-fake-guid"] =
            new ServerInfoModel { ServerName = "pazisi", EnvironmentName = "production" };
        project.GitProjectNames.Add("appfake");
        project.Endpoints["getitems"] = new EndpointModel { HttpMethod = EHttpMethod.Get };
        StsProjectDataModel contract = ContractOf(project);

        // Act
        List<string> result = ProjectMapper.FindRepeatedValues(contract);

        // Assert
        Assert.Equal([
            "GitProjectNames: AppFake/appfake", "Endpoints: GetItems/getitems",
            "ServerInfos: PAZISI|Production/pazisi|production"
        ], result);
    }

    //every set of the contract is checked, also an exact repetition
    [Theory]
    [InlineData(0, "GitProjectNames: SystemTools/SystemTools")]
    [InlineData(1, "ScaffoldSeederGitProjectNames: AppFakeScaffoldSeeder/appfakescaffoldseeder")]
    [InlineData(2, "FrontNpmPackageNames: react/REACT")]
    [InlineData(3, "RedundantFileNames: Class1.cs/class1.cs")]
    [InlineData(4, "AllowToolsList: SeedData/SeedData")]
    [InlineData(5, "RouteClasses: Items/items")]
    [InlineData(6, "ServerInfos.Merinson|Test.AllowToolsList: VersionChecker/VersionChecker")]
    [InlineData(7, "GitProjectNames: AppFake/APPFAKE, SystemTools/systemtools")]
    public void FindRepeatedValues_WhenASetRepeatsAValue_NamesTheSet(int caseNumber, string expected)
    {
        // Arrange
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        switch (caseNumber)
        {
            case 0:
                contract.GitProjectNames.Add("SystemTools");
                break;
            case 1:
                contract.ScaffoldSeederGitProjectNames.Add("appfakescaffoldseeder");
                break;
            case 2:
                contract.FrontNpmPackageNames.Add("REACT");
                break;
            case 3:
                contract.RedundantFileNames.Add("class1.cs");
                break;
            case 4:
                contract.AllowToolsList.Add("SeedData");
                break;
            case 5:
                contract.RouteClasses.Add(new StsProjectRouteClassDataModel { Name = "items" });
                break;
            case 6:
                contract.ServerInfos[1].AllowToolsList.Add("VersionChecker");
                break;
            default:
                contract.GitProjectNames.AddRange(["APPFAKE", "systemtools"]);
                break;
        }

        // Act
        List<string> result = ProjectMapper.FindRepeatedValues(contract);

        // Assert
        Assert.Equal([expected], result);
    }

    //the ServerInfos are sorted like the server returns them: by server, then environment, ignoring case; names that
    //differ only by case are sorted ordinally, so the order is always the same
    [Theory]
    [InlineData("PAZISI|Production,Merinson|Test", "Merinson|Test,PAZISI|Production")]
    [InlineData("PAZISI|Test,PAZISI|Production", "PAZISI|Production,PAZISI|Test")]
    [InlineData("pazisi|Production,PAZISI|Production", "PAZISI|Production,pazisi|Production")]
    [InlineData("PAZISI|production,PAZISI|Production", "PAZISI|Production,PAZISI|production")]
    [InlineData("merinson|Test,PAZISI|Production", "merinson|Test,PAZISI|Production")]
    public void Normalize_WhenServerInfosComeInAnyOrder_SortsThemByServerAndEnvironment(string input, string expected)
    {
        // Arrange
        StsProjectDataModel contract = ContractOf(new ProjectModel());
        contract.ServerInfos =
        [
            .. input.Split(',').Select(x => x.Split('|')).Select(x => new StsServerInfoDataModel
            {
                ServerName = x[0], EnvironmentName = x[1]
            })
        ];

        // Act
        StsProjectDataModel result = ProjectMapper.Normalize(contract);

        // Assert
        Assert.Equal(expected,
            string.Join(',',
                result.ServerInfos.Select(x => ServerInfoMapper.NaturalKey(x.ServerName, x.EnvironmentName))));
    }

    //"" and null are the same for every optional text, also inside the endpoints, route classes and ServerInfos
    [Fact]
    public void Normalize_WhenOptionalTextsAreEmpty_GivesTheHashOfMissingTexts()
    {
        // Arrange
        StsProjectDataModel empty = NewContractWithTexts("");
        StsProjectDataModel missing = NewContractWithTexts(null);

        // Act
        string emptyHash = HashOf(empty);
        string missingHash = HashOf(missing);

        // Assert
        Assert.Equal(missingHash, emptyHash);
        Assert.Equal((ProjectTestData.Name, "Standard"), (empty.Name, empty.ProjectType));
    }

    [Fact]
    public void Normalize_WhenPathsHaveALowerCaseDrive_GivesUpperCaseDrives()
    {
        // Arrange
        var project = new ProjectModel
        {
            ProjectFolderName = @"d:\1WorkDotnet\AppFake",
            MigrationSqlFilesFolder = @"d:\1WorkDotnet\AppFake\Sql",
            PairedDbObjectsResultFileName = @"d:\1WorkDotnet\Security\Paired.json"
        };

        // Act
        StsProjectDataModel result = ProjectMapper.Normalize(ContractOf(project));

        // Assert
        Assert.Equal(@"D:\1WorkDotnet\AppFake", result.ProjectFolderName);
        Assert.Equal(@"D:\1WorkDotnet\AppFake\Sql", result.MigrationSqlFilesFolder);
        Assert.Equal(@"D:\1WorkDotnet\Security\Paired.json", result.PairedDbObjectsResultFileName);
    }

    [Fact]
    public void Normalize_WhenSetsAreUnsorted_SortsThemByName()
    {
        // Arrange
        StsProjectDataModel contract = ContractOf(ProjectTestData.NewProject());
        contract.ScaffoldSeederGitProjectNames = ["b", "A"];
        contract.FrontNpmPackageNames = ["vite", "React"];
        contract.RedundantFileNames = ["z.cs", "a.cs"];
        contract.RouteClasses.Insert(0, new StsProjectRouteClassDataModel { Name = "Users" });

        // Act
        StsProjectDataModel result = ProjectMapper.Normalize(contract);

        // Assert
        Assert.Equal(["AppFake", "SystemTools"], result.GitProjectNames);
        Assert.Equal(["A", "b"], result.ScaffoldSeederGitProjectNames);
        Assert.Equal(["React", "vite"], result.FrontNpmPackageNames);
        Assert.Equal(["a.cs", "z.cs"], result.RedundantFileNames);
        Assert.Equal(["GenerateApiRoutes", "SeedData"], result.AllowToolsList);
        Assert.Equal(["GetItems", "SaveItem"], result.Endpoints.Select(x => x.Name));
        Assert.Equal(["Items", "Users"], result.RouteClasses.Select(x => x.Name));
    }

    //a contract with every optional text of the aggregate set to the given value
    private static StsProjectDataModel NewContractWithTexts(string? text)
    {
        return new StsProjectDataModel
        {
            Name = ProjectTestData.Name,
            ProjectType = "Standard",
            ProjectGroupName = text,
            ProjectDescription = text,
            EditorConfigPatternName = text,
            MainProjectName = text,
            ApiContractsProjectName = text,
            SpaProjectName = text,
            DbContextName = text,
            ProjectShortPrefix = text,
            ScaffoldSeederProjectName = text,
            DbContextProjectName = text,
            NewDataSeedingClassLibProjectName = text,
            ProgramArchiveDateMask = text,
            ProgramArchiveExtension = text,
            ParametersFileDateMask = text,
            ParametersFileExtension = text,
            ProjectFolderName = text,
            SolutionFileName = text,
            ProjectSecurityFolderPath = text,
            MigrationStartupProjectFilePath = text,
            MigrationProjectFilePath = text,
            DataSeederRulesByTableStartupProjectFilePath = text,
            OldDataConvertorForDataSeeder = text,
            SeedProjectFilePath = text,
            SeedProjectParametersFilePath = text,
            ExcludesRulesParametersFilePath = text,
            AppSetEnKeysJsonFileName = text,
            MigrationSqlFilesFolder = text,
            PrepareProdCopyDatabaseProjectFilePath = text,
            PrepareProdCopyDatabaseProjectParametersFilePath = text,
            PairedDbObjectsResultFileName = text,
            KeyGuidPart = text,
            DevDatabaseParameters = new StsDatabaseParametersDataModel { DatabaseName = text },
            ProdCopyDatabaseParameters = new StsDatabaseParametersDataModel { DateMask = text },
            Endpoints =
            [
                new StsProjectEndpointDataModel
                {
                    Name = "GetItems",
                    EndpointName = text,
                    EndpointRoute = text,
                    HttpMethod = "Get",
                    EndpointType = "Query",
                    ReturnType = text
                }
            ],
            RouteClasses =
            [
                new StsProjectRouteClassDataModel { Name = "Items", Root = text, ApiVersion = text, Base = text }
            ],
            ServerInfos =
            [
                new StsServerInfoDataModel
                {
                    ServerName = "PAZISI", EnvironmentName = "Production", ApiVersionId = text
                }
            ]
        };
    }

    private static StsProjectDataModel ContractOf(ProjectModel project)
    {
        return ProjectMapper.ToContract(ProjectTestData.Name, project, MapperTestHelpers.WindowsPathMapper());
    }

    private static string HashOf(StsProjectDataModel contract)
    {
        return MapperTestHelpers.HashOf(ProjectMapper.Normalize(contract));
    }

    private static IEnumerable<string> FieldsNotInContract(Type localType, Type contractType)
    {
        HashSet<string> contractFields = [.. contractType.GetProperties().Select(x => x.Name)];
        return localType.GetProperties().Where(x => !contractFields.Contains(x.Name))
            .Select(x => $"{localType.Name}.{x.Name}");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using LibSupportToolsServerWork.Registry;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

//the machine fields never reach the server, so no contract of the mappers has a property of that name
public sealed class MachineLocalFieldsContractTests
{
    [Fact]
    public void Contracts_WhenTheyHaveProperties_HaveNoMachineField()
    {
        // Act
        List<string> result =
        [
            .. MachineFieldsOf(typeof(StsGlobalSettingsDataModel), MachineLocalFields.TopLevel),
            .. MachineFieldsOf(typeof(StsDatabasesBackupFilesExchangeDataModel),
                MachineLocalFields.DatabasesBackupFilesExchange),
            .. MachineFieldsOf(typeof(StsServerDataModel), MachineLocalFields.Server),
            .. MachineFieldsOf(typeof(StsDotnetToolDataModel), MachineLocalFields.DotnetTool)
        ];

        // Assert
        Assert.Empty(result);
    }

    private static IEnumerable<string> MachineFieldsOf(Type contractType, IReadOnlyList<string> machineFields)
    {
        return contractType.GetProperties().Select(x => $"{contractType.Name}.{x.Name}")
            .Where(x => machineFields.Any(y => x.EndsWith($".{y}", StringComparison.Ordinal)));
    }
}

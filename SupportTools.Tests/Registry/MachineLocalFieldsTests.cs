using System.Linq;
using LibSupportToolsServerWork.Registry;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Registry;

public sealed class MachineLocalFieldsTests
{
    //D3: which projects are on a computer differs per computer, so hiding the others never syncs
    [Fact]
    public void TopLevel_WhenRead_ContainsHideRemoteOnlyProjects()
    {
        // Act
        bool result = MachineLocalFields.TopLevel.Contains(nameof(SupportToolsParameters.HideRemoteOnlyProjects));

        // Assert
        Assert.True(result);
    }
}

using System.Collections.Generic;
using LibSupportToolsServerWork.Registry.Sync;
using SupportTools.Menu.SyncRegistry;
using Xunit;

namespace SupportTools.Tests.Menu.SyncRegistry;

public sealed class RegistrySyncPlanSummaryTests
{
    //one line per collection in the order of the plan, then the total; record names are not shown
    [Fact]
    public void CreateLines_WhenPlanHasSeveralCollections_CountsTheActionsOfEveryCollection()
    {
        // Arrange
        var plan = new RegistrySyncPlan([
            Item("Environments", 10, "Dev", ERegistrySyncAction.Push),
            Item("Environments", 10, "Prod", ERegistrySyncAction.Push),
            Item("Environments", 10, "Test", ERegistrySyncAction.InSync),
            Item("Gits", 120, "AppA", ERegistrySyncAction.Conflict),
            Item("Gits", 120, "AppB", ERegistrySyncAction.Pull),
            Item("Projects", 170, "AppX", ERegistrySyncAction.Skipped)
        ]);

        // Act
        List<string> result = RegistrySyncPlanSummary.CreateLines(plan);

        // Assert
        Assert.Equal([
            "Collection                  Pull  Push  Conflict  InSync  Skipped",
            "Environments                   0     2         0       1        0",
            "Gits                           1     0         1       0        0",
            "Projects                       0     0         0       0        1",
            "Total                          1     2         1       1        1"
        ], result);
    }

    [Fact]
    public void CreateLines_WhenPlanIsEmpty_ShowsOnlyZeroTotal()
    {
        // Act
        List<string> result = RegistrySyncPlanSummary.CreateLines(new RegistrySyncPlan([]));

        // Assert
        Assert.Equal([
            "Collection                  Pull  Push  Conflict  InSync  Skipped",
            "Total                          0     0         0       0        0"
        ], result);
    }

    private static RegistrySyncPlanItem Item(string collectionName, int order, string key, ERegistrySyncAction action)
    {
        return new RegistrySyncPlanItem { CollectionName = collectionName, Order = order, Key = key, Action = action };
    }
}

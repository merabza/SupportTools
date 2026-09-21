using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;

namespace SupportTools.Tests;

internal static class CliMenuTestAccess
{
    //RunBody is protected, and its result is not observable through Run(): success and failure may both reload the menu
    public static async Task<bool> InvokeRunBody(CliMenuCommand command)
    {
        MethodInfo runBody = command.GetType()
            .GetMethod("RunBody", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return await (ValueTask<bool>)runBody.Invoke(command, [CancellationToken.None])!;
    }

    //CliMenuSet keeps its items (and their order) in private state only
    public static List<CliMenuItem> GetMenuItems(CliMenuSet menuSet)
    {
        PropertyInfo menuItemsProperty =
            typeof(CliMenuSet).GetProperty("MenuItems", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (List<CliMenuItem>)menuItemsProperty.GetValue(menuSet)!;
    }
}

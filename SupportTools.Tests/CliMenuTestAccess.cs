using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.CliParameters.FieldEditors;
using SystemTools.SystemToolsShared;

namespace SupportTools.Tests;

internal static class CliMenuTestAccess
{
    //RunBody is protected, and its result is not observable through Run(): success and failure may both reload the menu
    public static async Task<bool> InvokeRunBody(CliMenuCommand command)
    {
        MethodInfo runBody = command.GetType().GetMethod("RunBody", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return await (ValueTask<bool>)runBody.Invoke(command, [CancellationToken.None])!;
    }

    //CliMenuSet keeps its items (and their order) in private state only
    public static List<CliMenuItem> GetMenuItems(CliMenuSet menuSet)
    {
        PropertyInfo menuItemsProperty =
            typeof(CliMenuSet).GetProperty("MenuItems", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (List<CliMenuItem>)menuItemsProperty.GetValue(menuSet)!;
    }

    //RemoveRecordWithKey is protected, and DeleteRecord asks for a confirmation on the console
    public static async Task InvokeRemoveRecordWithKey(Cruder cruder, string recordKey)
    {
        MethodInfo removeRecordWithKey = cruder.GetType()
            .GetMethod("RemoveRecordWithKey", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (ValueTask)removeRecordWithKey.Invoke(cruder, [recordKey, CancellationToken.None])!;
    }

    //CreateNewItem is a protected override (its access cannot be widened); New calls it after asking for a console input
    public static ItemData InvokeCreateNewItem(Cruder cruder)
    {
        MethodInfo createNewItem = cruder.GetType()
            .GetMethod("CreateNewItem", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (ItemData)createNewItem.Invoke(cruder, [null, null])!;
    }

    //Cruder keeps its field editors in a protected list; New asks for those with EnterFieldDataOnCreate in this order
    public static List<FieldEditor> GetFieldEditors(Cruder cruder)
    {
        FieldInfo fieldEditorsField =
            typeof(Cruder).GetField("FieldEditors", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (List<FieldEditor>)fieldEditorsField.GetValue(cruder)!;
    }
}

using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;

namespace SupportTools.FieldEditors;

//პროექტის UseLocalPackages-ის რედაქტორი (Yes/No). ახალ პროექტს ეს ველი ავტომატურად ერთვება და შექმნისას აღარ
//იკითხება, შეცვლა მხოლოდ შექმნის შემდეგაა შესაძლებელი
public sealed class UseLocalPackagesFieldEditor : FieldEditor<bool>
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public UseLocalPackagesFieldEditor(string propertyName) : base(propertyName, false, true)
    {
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        SetValue(recordForUpdate, Inputer.InputBool(FieldName, GetValue(recordForUpdate, DefaultValue)));
        return ValueTask.CompletedTask;
    }
}

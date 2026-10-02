using System;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;

namespace SupportTools.FieldEditors;

//კომპიუტერის სახელის რედაქტორი. ცარიელი ველი ამ კომპიუტერის სახელს (Environment.MachineName) ნიშნავს, ამიტომ
//შეთავაზებულ სახელზე დათანხმება ველს ცარიელს ტოვებს: სხვა კომპიუტერზე გადატანილი ფაილი იქ თავის სახელს გამოიყენებს.
//ფაილში მხოლოდ განსხვავებული სახელი ჩაიწერება
public sealed class MachineNameFieldEditor : FieldEditor<string>
{
    private readonly Func<string, string?, string?> _inputText;

    public MachineNameFieldEditor(string propertyName) : this(propertyName, Environment.MachineName,
        (fieldName, defaultValue) => Inputer.InputText(fieldName, defaultValue))
    {
    }

    //კომპიუტერის სახელი და კონსოლიდან შეყვანა პარამეტრებადაა გამოტანილი, რომ ტესტებმა ორივე თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal MachineNameFieldEditor(string propertyName, string thisMachineName,
        Func<string, string?, string?> inputText) : base(propertyName, true, thisMachineName, true)
    {
        _inputText = inputText;
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        string? machineName = _inputText(FieldName, GetValueOrDefault(recordForUpdate))?.Trim();

        //Windows-ზე და DNS-ში კომპიუტერის სახელი რეგისტრს არ არჩევს
        SetValue(recordForUpdate,
            string.IsNullOrEmpty(machineName) ||
            string.Equals(machineName, DefaultValue, StringComparison.OrdinalIgnoreCase)
                ? null
                : machineName);
        return ValueTask.CompletedTask;
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;
using SystemTools.SystemToolsShared;

namespace SupportTools.FieldEditors;

//პაროლს ორჯერ ითხოვს და ინახავს მხოლოდ მის ჰეშს. ცარიელი პასუხი ძველ ჰეშს ტოვებს
public sealed class PasswordHashFieldEditor : FieldEditor<string>
{
    private readonly Func<string, bool> _confirm;
    private readonly Func<string, string> _hashPassword;
    private readonly Func<string, string?> _inputPassword;

    public PasswordHashFieldEditor(string propertyName, Func<string, string> hashPassword) : this(propertyName,
        hashPassword, InputPassword, ConfirmRemove)
    {
    }

    //კონსოლიდან შეყვანა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხები თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal PasswordHashFieldEditor(string propertyName, Func<string, string> hashPassword,
        Func<string, string?> inputPassword, Func<string, bool> confirm) : base(propertyName, true)
    {
        _hashPassword = hashPassword;
        _inputPassword = inputPassword;
        _confirm = confirm;
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        bool isSet = !string.IsNullOrEmpty(GetValue(recordForUpdate));
        string? password = _inputPassword($"{FieldName} (password{(isSet ? ", empty = keep" : string.Empty)})");
        if (string.IsNullOrEmpty(password))
        {
            if (isSet && _confirm($"Remove {FieldName}?"))
            {
                SetValue(recordForUpdate, null);
            }

            return ValueTask.CompletedTask;
        }

        if (_inputPassword("Repeat password") != password)
        {
            StShared.WriteErrorLine("Passwords do not match, nothing changed", true);
            return ValueTask.CompletedTask;
        }

        SetValue(recordForUpdate, _hashPassword(password));
        return ValueTask.CompletedTask;
    }

    public override string GetValueStatus(object? record)
    {
        return string.IsNullOrEmpty(GetValueOrDefault(record)) ? string.Empty : "set";
    }

    //პაროლი ეკრანზე ვარსკვლავებით ჩანს
    internal static string? InputPassword(string prompt)
    {
        return Inputer.InputText(prompt, null, '*');
    }

    //Enter ჰეშს ტოვებს
    internal static bool ConfirmRemove(string question)
    {
        return Inputer.InputBool(question, false, false);
    }
}

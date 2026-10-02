using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibMenuInput;
using LibSupportToolsServerWork.Registry;
using SupportToolsData.Models;

namespace SupportTools.FieldEditors;

//ამ კომპიუტერის ჩანაწერის არჩევა Servers-იდან. ServerDataNameFieldEditor-ისგან განსხვავებით ახალ ჩანაწერს არ ქმნის.
//(None) ნიშნავს, რომ ეს კომპიუტერი Servers-ში არ არის
public sealed class CurrentMachineServerNameFieldEditor : FieldEditor<string>
{
    private readonly Func<string, List<string>, string?, string?> _selectServerName;

    public CurrentMachineServerNameFieldEditor(string propertyName) : this(propertyName,
        (fieldName, serverNames, currentServerName) =>
        {
            var selectInput = new SelectFromListInput(fieldName, serverNames, currentServerName, true);
            return selectInput.DoInput() ? selectInput.Text : currentServerName;
        })
    {
    }

    //კონსოლიდან არჩევა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხები თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal CurrentMachineServerNameFieldEditor(string propertyName,
        Func<string, List<string>, string?, string?> selectServerName) : base(propertyName)
    {
        _selectServerName = selectServerName;
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)recordForUpdate;

        List<string> serverNames = [.. parameters.Servers.Keys.Order(StringComparer.OrdinalIgnoreCase)];
        SetValue(parameters, _selectServerName(FieldName, serverNames, GetValue(parameters)));

        //IsLocal-ები ახალ არჩევანს მაშინვე უნდა შეესაბამებოდეს და არა მხოლოდ პროგრამის შემდეგი გაშვებიდან
        ServersIsLocalCalculator.Recalculate(parameters);
        return ValueTask.CompletedTask;
    }
}

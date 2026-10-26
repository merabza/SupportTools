using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;
using LibAppInstallWork;
using SupportToolsData.Models;

namespace SupportTools.FieldEditors;

//ServerInfo-ს OpenPort-ის რედაქტორი (Yes/No). არჩევანის სტრიქონსა და მნიშვნელობის სტატუსში ჩანს პორტი, რომელიც
//App Settings Json Source File-ის Kestrel-ის Endpoints-შია, ან მიზეზი, რის გამოც პორტი ვერ დადგინდა
public sealed class OpenPortFieldEditor : FieldEditor<bool>
{
    private readonly Func<string, bool, bool> _inputBool;

    public OpenPortFieldEditor(string propertyName) : this(propertyName,
        (fieldName, defaultValue) => Inputer.InputBool(fieldName, defaultValue))
    {
    }

    //კონსოლიდან შეყვანა პარამეტრადაა გამოტანილი, რომ ტესტებმა თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal OpenPortFieldEditor(string propertyName, Func<string, bool, bool> inputBool) : base(propertyName, true)
    {
        _inputBool = inputBool;
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        SetValue(recordForUpdate,
            _inputBool($"{FieldName} ({GetPortsText(recordForUpdate)})", GetValue(recordForUpdate)));
        return ValueTask.CompletedTask;
    }

    public override string GetValueStatus(object? record)
    {
        return $"{(GetValue(record) ? "Yes" : "No")} ({GetPortsText(record)})";
    }

    internal static string GetPortsText(object? record)
    {
        List<int>? ports =
            KestrelPortsReader.ReadFile((record as ServerInfoModel)?.AppSettingsJsonSourceFileName, out string? error);
        if (ports is null)
        {
            return error ?? string.Empty;
        }

        return ports.Count == 0 ? "no Kestrel endpoint port" : $"port {string.Join(", ", ports)}";
    }
}

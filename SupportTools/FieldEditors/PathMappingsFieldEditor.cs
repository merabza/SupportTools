using System.Collections.Generic;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.FieldEditors;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData.Models;

namespace SupportTools.FieldEditors;

//გზების გარდაქმნის წესების სიის რედაქტორი (გადადის PathMappingCruder-ის სიის მენიუზე)
public sealed class PathMappingsFieldEditor : FieldEditor<List<PathMappingModel>>
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PathMappingsFieldEditor(string propertyName, IParametersManager parametersManager) : base(propertyName,
        false, null, false, null, true)
    {
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu(object record)
    {
        return new PathMappingCruder(_parametersManager, GetValue(record) ?? []).GetListMenu();
    }

    public override string GetValueStatus(object? record)
    {
        int count = GetValue(record)?.Count ?? 0;
        return count switch
        {
            0 => "No path mappings",
            1 => "1 path mapping",
            _ => $"{count} path mappings"
        };
    }
}

using System.Collections.Generic;
using AppCliTools.CliParameters.Cruders;
using ParametersManagement.LibParameters;

namespace SupportTools.Cruders;

//PairedTable.KeyFieldNames-ის რედაქტორი — Adjust შერწყმის ბუნებრივი გასაღების ველების უბრალო სია.
//ცვლილება წყვილების ფაილის მენეჯერით ინახება
public sealed class KeyFieldNamesLisCruder : SimpleNamesListCruder
{
    private readonly List<string> _currentValuesList;

    // ReSharper disable once ConvertToPrimaryConstructor
    public KeyFieldNamesLisCruder(IParametersManager parametersManager, List<string> currentValuesList) : base(
        parametersManager, "Key Field Name", "Key Field Names")
    {
        _currentValuesList = currentValuesList;
    }

    protected override List<string> GetList()
    {
        return _currentValuesList;
    }
}

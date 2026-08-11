using System.Collections.Generic;
using AppCliTools.CliParameters.Cruders;

namespace SupportTools.Cruders;

//PairedTable.KeyFieldNames-ის რედაქტორი — Adjust შერწყმის ბუნებრივი გასაღების ველების უბრალო სია
public sealed class KeyFieldNamesLisCruder : SimpleNamesListCruder
{
    private readonly List<string> _currentValuesList;

    // ReSharper disable once ConvertToPrimaryConstructor
    public KeyFieldNamesLisCruder(List<string> currentValuesList) : base("Key Field Name", "Key Field Names")
    {
        _currentValuesList = currentValuesList;
    }

    protected override List<string> GetList()
    {
        return _currentValuesList;
    }
}

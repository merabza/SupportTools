using System.Collections.Generic;
using AppCliTools.CliParameters.Cruders;
using ParametersManagement.LibParameters;

namespace SupportTools.Cruders;

//სერვერის ინსტალაციის მარტივი სიები: IPv4 მისამართები, DNS სერვერები, SSH გასაღებები
public sealed class ServerInstallationListCruder : SimpleNamesListCruder
{
    private readonly List<string> _currentValuesList;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServerInstallationListCruder(IParametersManager parametersManager, List<string> currentValuesList,
        string crudName, string crudNamePlural) : base(parametersManager, crudName, crudNamePlural)
    {
        _currentValuesList = currentValuesList;
    }

    protected override List<string> GetList()
    {
        return _currentValuesList;
    }
}

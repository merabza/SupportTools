using System.Collections.Generic;
using SupportToolsData;

namespace SupportTools.Menu;

public class SupportToolsMenuParameters
{
    public string ProjectGroupName { get; set; }
    public string ProjectName { get; set; }

    //"Check ... build"-ის შედეგები (პროექტის სახელი -> სტატუსი). ინახება მხოლოდ მეხსიერებაში და ჯეისონში
    //არ ეწერება, ამიტომ პროგრამის თავიდან გაშვებისას იკარგება. პროექტი/ჯგუფი სტატუსს აჩვენებს მხოლოდ მაშინ,
    //თუ აქ უკვე არსებობს მისი ჩანაწერი
    public Dictionary<string, EProjectBuildCheckStatus> ProjectBuildCheckStatuses { get; } = [];
}

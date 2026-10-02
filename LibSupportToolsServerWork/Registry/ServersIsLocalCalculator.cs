using System;
using System.Collections.Generic;
using SupportToolsData.Models;

namespace LibSupportToolsServerWork.Registry;

//ServerDataModel.IsLocal კომპიუტერზეა დამოკიდებული და სინქრონიზაციით არ გადადის (G6). თუ CurrentMachineServerName
//შევსებულია, ლოკალურია მხოლოდ ის სერვერი, რომლის სახელიც მას ემთხვევა (რეგისტრის გარეშე). თუ ცარიელია, ფაილში
//შენახული მნიშვნელობები რჩება, რომ ძველი პარამეტრების ფაილი ისევე იმუშაოს, როგორც აქამდე
public static class ServersIsLocalCalculator
{
    public static void Recalculate(SupportToolsParameters parameters)
    {
        string? currentMachineServerName = parameters.CurrentMachineServerName;
        if (string.IsNullOrWhiteSpace(currentMachineServerName))
        {
            return;
        }

        foreach (KeyValuePair<string, ServerDataModel> server in parameters.Servers)
        {
            server.Value.IsLocal =
                string.Equals(server.Key, currentMachineServerName, StringComparison.OrdinalIgnoreCase);
        }
    }
}

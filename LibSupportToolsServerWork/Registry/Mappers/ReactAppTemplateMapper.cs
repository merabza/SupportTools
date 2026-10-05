using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Mappers;

//React აპლიკაციის შაბლონი: SupportToolsParameters.ReactAppTemplates-ის ჩანაწერი (სახელი → create-react-app-ის
//--template მნიშვნელობა) ↔ StsReactAppTemplateDataModel
public static class ReactAppTemplateMapper
{
    public static StsReactAppTemplateDataModel ToContract(string name, string template)
    {
        return new StsReactAppTemplateDataModel { Name = name, Template = template };
    }

    public static string ToLocal(StsReactAppTemplateDataModel contract)
    {
        return contract.Template;
    }

    //Template სავალდებულოა (სერვერი ცარიელს უარყოფს), ამიტომ ნორმალიზაცია არაფერს ცვლის
    public static StsReactAppTemplateDataModel Normalize(StsReactAppTemplateDataModel contract)
    {
        return contract;
    }
}

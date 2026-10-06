using System;
using SupportToolsData;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Mappers;

//პროექტის შაბლონი: AppProjectCreatorAllParameters.Templates-ის ჩანაწერი (TemplateModel) ↔ StsProjectTemplateDataModel.
//SupportProjectType ESupportProjectType-ის სახელია
public static class ProjectTemplateMapper
{
    public static StsProjectTemplateDataModel ToContract(string name, TemplateModel template)
    {
        return new StsProjectTemplateDataModel
        {
            Name = name,
            SupportProjectType = template.SupportProjectType.ToString(),
            TestProjectName = template.TestProjectName,
            TestProjectShortName = template.TestProjectShortName,
            UseDatabase = template.UseDatabase,
            UseDbPartFolderForDatabaseProjects = template.UseDbPartFolderForDatabaseProjects,
            UseMenu = template.UseMenu,
            UseHttps = template.UseHttps,
            UseReact = template.UseReact,
            UseCarcass = template.UseCarcass,
            UseIdentity = template.UseIdentity,
            UseReCounter = template.UseReCounter,
            UseSignalR = template.UseSignalR,
            UseFluentValidation = template.UseFluentValidation,
            ReactTemplateName = template.ReactTemplateName
        };
    }

    //პროექტის ტიპი ამ კლიენტისთვის ცნობილი უნდა იყოს (ადაპტერი უცნობს წინასწარ გამორიცხავს)
    public static void ApplyToLocal(StsProjectTemplateDataModel contract, TemplateModel template)
    {
        template.SupportProjectType = Enum.Parse<ESupportProjectType>(contract.SupportProjectType, true);
        template.TestProjectName = contract.TestProjectName;
        template.TestProjectShortName = contract.TestProjectShortName;
        template.UseDatabase = contract.UseDatabase;
        template.UseDbPartFolderForDatabaseProjects = contract.UseDbPartFolderForDatabaseProjects;
        template.UseMenu = contract.UseMenu;
        template.UseHttps = contract.UseHttps;
        template.UseReact = contract.UseReact;
        template.UseCarcass = contract.UseCarcass;
        template.UseIdentity = contract.UseIdentity;
        template.UseReCounter = contract.UseReCounter;
        template.UseSignalR = contract.UseSignalR;
        template.UseFluentValidation = contract.UseFluentValidation;
        template.ReactTemplateName = contract.ReactTemplateName;
    }

    public static StsProjectTemplateDataModel Normalize(StsProjectTemplateDataModel contract)
    {
        contract.SupportProjectType = ContractNormalization.EnumName<ESupportProjectType>(contract.SupportProjectType);
        contract.TestProjectName = ContractNormalization.EmptyToNull(contract.TestProjectName);
        contract.TestProjectShortName = ContractNormalization.EmptyToNull(contract.TestProjectShortName);
        contract.ReactTemplateName = ContractNormalization.EmptyToNull(contract.ReactTemplateName);
        return contract;
    }

    public static bool HasUnknownSupportProjectType(StsProjectTemplateDataModel contract)
    {
        return !ContractNormalization.IsEnumName<ESupportProjectType>(contract.SupportProjectType);
    }
}

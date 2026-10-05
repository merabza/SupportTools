using System;
using System.Linq;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsServerApiContracts.Models;
using SystemTools.SystemToolsShared;

namespace LibSupportToolsServerWork.Registry.Mappers;

//ჭკვიანი სქემა: SmartSchema ↔ StsSmartSchemaDataModel, დეტალებით. დეტალის PeriodType EPeriodType-ის სახელია.
//დეტალების რიგს მნიშვნელობა არ აქვს: პერიოდის ტიპი სქემაში ერთხელ გვხვდება
public static class SmartSchemaMapper
{
    public static StsSmartSchemaDataModel ToContract(string name, SmartSchema smartSchema)
    {
        return new StsSmartSchemaDataModel
        {
            Name = name,
            LastPreserveCount = smartSchema.LastPreserveCount,
            Details =
            [
                .. smartSchema.Details.Select(x => new StsSmartSchemaDetailDataModel
                {
                    PeriodType = x.PeriodType.ToString(), PreserveCount = x.PreserveCount
                })
            ]
        };
    }

    //SmartSchema-ს თვისებები init-only-ა, ამიტომ ჩანაწერი ახალი ეგზემპლარით იცვლება; კომპიუტერის ველები მას არ აქვს.
    //პერიოდის ტიპები ამ კლიენტისთვის ცნობილი უნდა იყოს (ადაპტერი უცნობს წინასწარ გამორიცხავს)
    public static SmartSchema ToLocal(StsSmartSchemaDataModel contract)
    {
        return new SmartSchema
        {
            LastPreserveCount = contract.LastPreserveCount,
            Details =
            [
                .. contract.Details.Select(x => new SmartSchemaDetail
                {
                    PeriodType = Enum.Parse<EPeriodType>(x.PeriodType, true), PreserveCount = x.PreserveCount
                })
            ]
        };
    }

    public static StsSmartSchemaDataModel Normalize(StsSmartSchemaDataModel contract)
    {
        foreach (StsSmartSchemaDetailDataModel detail in contract.Details)
        {
            detail.PeriodType = ContractNormalization.EnumName<EPeriodType>(detail.PeriodType);
        }

        contract.Details = ContractNormalization.OrderByName(contract.Details, x => x.PeriodType);
        return contract;
    }

    //დეტალის პერიოდის ტიპი, რომელიც ამ კლიენტის EPeriodType-ში არ არის
    public static bool HasUnknownPeriodType(StsSmartSchemaDataModel contract)
    {
        return !contract.Details.TrueForAll(x => ContractNormalization.IsEnumName<EPeriodType>(x.PeriodType));
    }
}

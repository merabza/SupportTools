using System;
using System.Collections.Generic;
using System.Linq;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//Dev-ის NOT NULL სვეტები default-ის გარეშე, რომლებიც pairs-ში არ არის (მაგ. ახალი სვეტი, რომელიც ProdCopy-ს ჯერ არ აქვს):
//რიცხვითი და bit სვეტები 0-ით ივსება, დანარჩენი ტიპების სვეტებს ავტომატური მნიშვნელობა არ აქვს
internal static class UnpairedRequiredColumnsFinder
{
    private static readonly HashSet<string> ZeroFillableTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "bit",
        "tinyint",
        "smallint",
        "int",
        "bigint",
        "decimal",
        "numeric",
        "float",
        "real",
        "money",
        "smallmoney"
    };

    public static (List<string> ZeroFilled, List<string> Unfillable) Find(PairedTable pt, DevTableMeta meta)
    {
        var pairedDevFields = new HashSet<string>(pt.PairedFields.Values.Select(f => f.DevFieldName),
            StringComparer.Ordinal);
        List<string> zeroFilled = [];
        List<string> unfillable = [];
        foreach ((string column, string typeName) in meta.NotNullWithoutDefaultColumns.OrderBy(x => x.Key,
                     StringComparer.Ordinal))
        {
            if (pairedDevFields.Contains(column))
            {
                continue;
            }

            if (ZeroFillableTypes.Contains(typeName))
            {
                zeroFilled.Add(column);
            }
            else
            {
                unfillable.Add($"{column} ({typeName})");
            }
        }

        return (zeroFilled, unfillable);
    }
}

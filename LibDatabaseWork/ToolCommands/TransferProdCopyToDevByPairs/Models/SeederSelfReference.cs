using System.Collections.Generic;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;

//ცხრილის მიმართვა საკუთარ თავზე (მაგ. DataTypes.DtManyToManyJoinParentDataTypeId) — ID ჩაწერამდე ჯერ არ არსებობს,
//ამიტომ სამიზნე სვეტი ჩაწერის შემდეგ UPDATE-ით ივსება
public sealed class SeederSelfReference
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public SeederSelfReference(Dictionary<string, object?> row, SeederReferenceRule rule, string ownKey,
        string referencedKey)
    {
        Row = row;
        Rule = rule;
        OwnKey = ownKey;
        ReferencedKey = referencedKey;
    }

    //მწკრივი, რომელსაც მიმართვა ეკუთვნის (ჩაწერილ მწკრივებში reference-ით მოიძებნება)
    public Dictionary<string, object?> Row { get; }

    public SeederReferenceRule Rule { get; }

    //მწკრივის საკუთარი ბუნებრივი გასაღები (მაგ. DtTable)
    public string OwnKey { get; }

    //მიმართული მწკრივის ბუნებრივი გასაღები
    public string ReferencedKey { get; }
}

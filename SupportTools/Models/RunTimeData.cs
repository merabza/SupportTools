using LibParameters;

namespace SupportTools.Models;

public sealed class RunTimeData : ItemData
{
    public string? Description { get; set; }

    public override string? GetItemName()
    {
        return Description;
    }
}
using System;
using System.Collections.Generic;
using SupportTools.Domain.Primitives;

namespace SupportTools.Domain.GitIgnoreFileTypes;

public class GitIgnoreFileTypeId : ValueObject
{
    public Guid Value { get; }

    public GitIgnoreFileTypeId(Guid value)
    {
        Value = value;
    }

    public static GitIgnoreFileTypeId CreateUnique()
    {
        return new GitIgnoreFileTypeId(Guid.NewGuid());
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
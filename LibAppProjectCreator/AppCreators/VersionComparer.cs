using System;
using System.Collections.Generic;

namespace LibAppProjectCreator.AppCreators;

public class VersionComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        switch (x)
        {
            case null when y == null:
                return 0;
            case null:
                return -1;
        }

        if (y == null)
            return 1;

        var xDigs = StringToDigits(x);
        var yDigs = StringToDigits(y);

        var minLength = xDigs.Length < yDigs.Length ? xDigs.Length : yDigs.Length;

        for (var i = 0; i < minLength; i++)
        {
            var subst = xDigs[i] - yDigs[i];
            if (subst != 0)
                return subst;
        }

        if (xDigs.Length > minLength)
            return 1;

        if (yDigs.Length > minLength)
            return -1;

        return 0;
    }


    private static int[] StringToDigits(string str)
    {
        var digits = str.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var intDigits = new List<int>();
        foreach (var digit in digits)
            if (int.TryParse(digit, out var d))
                intDigits.Add(d);
        return intDigits.ToArray();
    }
}
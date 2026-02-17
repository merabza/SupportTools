using System;
using System.Collections.Generic;

namespace LibAppProjectCreator.AppCreators;

public sealed class VersionComparer : IComparer<string>
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
        {
            return 1;
        }

        int[] xDigs = StringToDigits(x);
        int[] yDigs = StringToDigits(y);

        int minLength = xDigs.Length < yDigs.Length ? xDigs.Length : yDigs.Length;

        for (int i = 0; i < minLength; i++)
        {
            int subst = xDigs[i] - yDigs[i];
            if (subst != 0)
            {
                return subst;
            }
        }

        if (xDigs.Length > minLength)
        {
            return 1;
        }

        if (yDigs.Length > minLength)
        {
            return -1;
        }

        return 0;
    }

    private static int[] StringToDigits(string str)
    {
        string[] digits = str.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var intDigits = new List<int>();
        foreach (string digit in digits)
        {
            if (int.TryParse(digit, out int d))
            {
                intDigits.Add(d);
            }
        }

        return intDigits.ToArray();
    }
}

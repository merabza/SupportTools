using System;
using System.Collections.Generic;
using LibGitWork.OutdatedPackages;

namespace SupportTools.Tests.OutdatedPackages;

//records the steps of one git's update and the lines it writes, with their colors
internal sealed class FakePackageUpdateOutput : IPackageUpdateOutput
{
    public List<string> Statuses { get; } = [];
    public List<(string Text, ConsoleColor? Color)> Lines { get; } = [];

    public void SetStatus(string status)
    {
        Statuses.Add(status);
    }

    public void WriteLine(string text, ConsoleColor? color)
    {
        Lines.Add((text, color));
    }
}

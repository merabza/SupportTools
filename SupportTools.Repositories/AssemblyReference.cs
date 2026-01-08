
using System.Reflection;

namespace SupportTools.Repositories;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
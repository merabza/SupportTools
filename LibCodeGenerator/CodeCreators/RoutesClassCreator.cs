using System;
using System.Globalization;
using AppCliTools.CodeTools;
using Microsoft.Extensions.Logging;

namespace LibCodeGenerator.CodeCreators;

internal sealed class RoutesClassCreator : CodeCreator
{
    private readonly string _className;

    private readonly string _classNamespace;

    //private readonly Dictionary<string, EndpointModel> _endpoints;
    private readonly string _root;

    //private readonly IEnumerable<RouteClassModel> _routeClasses;
    private readonly string _version;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RoutesClassCreator(ILogger logger, string placePath, string classNamespace, string codeFileName,
        string className, string version, string root) : base(logger, placePath, codeFileName)
    {
        _classNamespace = classNamespace;
        _className = className;
        _version = version;
        _root = root;
        //_routeClasses = routeClasses;
        //_endpoints = endpoints;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            string.Empty, $"namespace {_classNamespace}", string.Empty,
            new CodeBlock($"public static class {_className}",
                $"private const string Root = \"{_root.ToLower(CultureInfo.CurrentCulture)}\"",
                $"private const string Version = \"{_version.ToLower(CultureInfo.CurrentCulture)}\"",
                "public const string ApiBase = Root + \"/\" + Version", new CodeBlock("")), string.Empty);
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}

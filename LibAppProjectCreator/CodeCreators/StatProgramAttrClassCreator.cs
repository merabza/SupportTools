//using System;
//using CodeTools;
//using Microsoft.Extensions.Logging;
//using SystemToolsShared;

//namespace LibAppProjectCreator.CodeCreators;

//public sealed class StatProgramAttrClassCreator : CodeCreator
//{
//    private readonly string? _appKey;

//    private readonly string _projectNamespace;

//    public StatProgramAttrClassCreator(ILogger logger, string placePath, string projectNamespace, string? appKey = null,
//        string? codeFileName = null) : base(logger, placePath, codeFileName)
//    {
//        _projectNamespace = projectNamespace;
//        _appKey = appKey;
//    }


//    public override void CreateFileStructure()
//    {
//        var block = new CodeBlock(string.Empty,
//            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
//            "using SystemToolsShared",
//            string.Empty,
//            $"namespace {_projectNamespace}",
//            string.Empty,
//            new CodeBlock("public sealed class StatProgramAttr",
//                new CodeBlock("public static void SetAttr()",
//                    $"ProgramAttributes.Instance.SetAttribute(\"AppName\", \"{string.Join(" ", _projectNamespace.SplitUpperCase())}\")",
//                    _appKey is null ? null : $"ProgramAttributes.Instance.SetAttribute(\"AppKey\", \"{_appKey}\")"
//                )));
//        CodeFile.AddRange(block.CodeItems);
//        FinishAndSave();
//    }
//}


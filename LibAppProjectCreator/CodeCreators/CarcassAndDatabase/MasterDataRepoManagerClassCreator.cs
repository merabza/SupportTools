//using System;
//using CodeTools;
//using Microsoft.Extensions.Logging;
//using SystemToolsShared;

//namespace LibAppProjectCreator.CodeCreators.CarcassAndDatabase;

//public sealed class MasterDataRepoManagerClassCreator : CodeCreator
//{
//    private readonly string _projectNamespace;
//    private readonly string _projectShortName;

//    // ReSharper disable once ConvertToPrimaryConstructor
//    public MasterDataRepoManagerClassCreator(ILogger logger, string placePath, string projectNamespace,
//        string projectShortName,
//        string? codeFileName = null) : base(logger, placePath, codeFileName)
//    {
//        _projectNamespace = projectNamespace;
//        _projectShortName = projectShortName;
//    }


//    public override void CreateFileStructure()
//    {
//        var block = new CodeBlock(string.Empty,
//            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
//            "using System.Collections.Generic",
//            string.Empty,
//            $"namespace {_projectNamespace}MasterDataLoaders",
//            string.Empty,
//            new CodeBlock($"public sealed class {_projectShortName.Capitalize()}MasterDataRepoManager",
//                $"private readonly Dictionary<string, I{_projectShortName.Capitalize()}MdLoaderCreator> _custom{_projectShortName.Capitalize()}MdLoaderCreators = new()",
//                string.Empty,
//                new CodeBlock($"public {_projectShortName.Capitalize()}MasterDataRepoManager()",
//                    $"_custom{_projectShortName.Capitalize()}MdLoaderCreators.Add(\"testQuery\", new Test{_projectShortName.Capitalize()}MdLoaderCreator())"),
//                string.Empty,
//                new CodeBlock(
//                    $"public I{_projectShortName.Capitalize()}MdLoaderCreator? GetLoaderCreator(string tableName)",
//                    $"return _custom{_projectShortName.Capitalize()}MdLoaderCreators.ContainsKey(tableName) ? _custom{_projectShortName.Capitalize()}MdLoaderCreators[tableName] : null")
//            ));
//        CodeFile.AddRange(block.CodeItems);
//        FinishAndSave();
//    }
//}
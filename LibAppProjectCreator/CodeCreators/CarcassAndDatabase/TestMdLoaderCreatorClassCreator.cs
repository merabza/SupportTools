//using System;
//using CodeTools;
//using Microsoft.Extensions.Logging;
//using SystemToolsShared;

//namespace LibAppProjectCreator.CodeCreators.CarcassAndDatabase;

//public sealed class TestMdLoaderCreatorClassCreator : CodeCreator
//{
//    private readonly string _projectNamespace;
//    private readonly string _projectShortName;

//    // ReSharper disable once ConvertToPrimaryConstructor
//    public TestMdLoaderCreatorClassCreator(ILogger logger, string placePath, string projectNamespace,
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
//            "using CarcassRepositories.MasterDataLoaders",
//            $"using {_projectNamespace}Db",
//            string.Empty,
//            $"namespace {_projectNamespace}MasterDataLoaders",
//            string.Empty,
//            new CodeBlock(
//                $"public sealed class Test{_projectShortName.Capitalize()}MdLoaderCreator : I{_projectShortName.Capitalize()}MdLoaderCreator",
//                string.Empty,
//                new CodeBlock($"public ICustomMdLoader Create({_projectNamespace}DbContext ctx)",
//                    $"return new Test{_projectShortName.Capitalize()}MdLoader(ctx)")));
//        CodeFile.AddRange(block.CodeItems);
//        FinishAndSave();
//    }
//}


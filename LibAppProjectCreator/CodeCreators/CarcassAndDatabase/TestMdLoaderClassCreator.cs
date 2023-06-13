using System;
using CodeTools;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.CodeCreators.CarcassAndDatabase;

public sealed class TestMdLoaderClassCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly string _projectShortName;

    public TestMdLoaderClassCreator(ILogger logger, string placePath, string projectNamespace, string projectShortName,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _projectShortName = projectShortName;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System.Linq",
            "using CarcassDb",
            "using CarcassRepositories.MasterDataLoaders",
            $"using {_projectNamespace}Db",
            "",
            $"namespace {_projectNamespace}MasterDataLoaders",
            "",
            new CodeBlock($"public sealed class Test{_projectShortName.Capitalize()}MdLoader : ICustomMdLoader",
                "",
                $"private readonly {_projectNamespace}DbContext _{_projectShortName.UnCapitalize()}Ctx",
                "",
                new CodeBlock($"public Test{_projectShortName.Capitalize()}MdLoader({_projectNamespace}DbContext ctx)",
                    $"_{_projectShortName.UnCapitalize()}Ctx = ctx"),
                "",
                new CodeBlock("public IQueryable<IDataType> GetEntity()",
                    "return null")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}
using System;
using CodeTools;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.CodeCreators.CarcassAndDatabase;

public sealed class MdLoaderCreatorInterfaceCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly string _projectShortName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public MdLoaderCreatorInterfaceCreator(ILogger logger, string placePath, string projectNamespace,
        string projectShortName,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _projectShortName = projectShortName;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using CarcassRepositories.MasterDataLoaders",
            $"using {_projectNamespace}Db",
            "",
            $"namespace {_projectNamespace}MasterDataLoaders",
            "",
            new CodeBlock($"public interface I{_projectShortName.Capitalize()}MdLoaderCreator",
                $"ICustomMdLoader Create({_projectNamespace}DbContext ctx)"
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}
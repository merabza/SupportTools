﻿using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.Database;

public sealed class RepositoryCreatorFabricCreator : CodeCreator
{
    private readonly string _projectNamespace;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RepositoryCreatorFabricCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            string.Empty, "using System", "using Microsoft.Extensions.DependencyInjection", string.Empty,
            $"namespace Lib{_projectNamespace}Repositories", string.Empty,
            new CodeBlock(
                $"public sealed class {_projectNamespace}RepositoryCreatorFabric : I{_projectNamespace}RepositoryCreatorFabric",
                "private readonly IServiceProvider _services",
                new CodeBlock($"public {_projectNamespace}RepositoryCreatorFabric(IServiceProvider services)",
                    "_services = services"), string.Empty,
                new CodeBlock($"public I{_projectNamespace}Repository Get{_projectNamespace}Repository()",
                    "IServiceScope scope = _services.CreateScope()",
                    $"return scope.ServiceProvider.GetRequiredService<I{_projectNamespace}Repository>()")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}
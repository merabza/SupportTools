//Created by CreatorClassCreator at 7/16/2022 11:01:23 PM

using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.PagesCreators;

public sealed class ErrorModelClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ErrorModelClassCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using Microsoft.AspNetCore.Mvc", "using Microsoft.AspNetCore.Mvc.RazorPages",
            "using Microsoft.Extensions.Logging", "using System", "using System.Collections.Generic",
            "using System.Diagnostics", "using System.Linq", "using System.Threading.Tasks", string.Empty,
            new CodeBlock($"namespace {_projectNamespace}.Pages",
                new CodeBlock("public sealed class ErrorModel : PageModel",
                    "private readonly ILogger<ErrorModel> logger", string.Empty,
                    new CodeBlock("public ErrorModel(ILogger<ErrorModel> _logger)", "logger = _logger"), string.Empty,
                    new CodeBlock("public string RequestId", "get", "set"), string.Empty,
                    "public bool ShowRequestId => !string.IsNullOrEmpty(RequestId)", string.Empty,
                    new CodeBlock("public void OnGet()",
                        "RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier"))), string.Empty);
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}
//Created by CreatorClassCreator at 7/16/2022 11:01:23 PM

using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.PagesCreators;

public sealed class ErrorModelClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    public ErrorModelClassCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using Microsoft.AspNetCore.Mvc",
            "using Microsoft.AspNetCore.Mvc.RazorPages",
            "using Microsoft.Extensions.Logging",
            "using System",
            "using System.Collections.Generic",
            "using System.Diagnostics",
            "using System.Linq",
            "using System.Threading.Tasks",
            "",
            new CodeBlock($"namespace {_projectNamespace}.Pages",
                new CodeBlock("public sealed class ErrorModel : PageModel",
                    "private readonly ILogger<ErrorModel> logger",
                    "",
                    new CodeBlock("public ErrorModel(ILogger<ErrorModel> _logger)",
                        "logger = _logger"),
                    "",
                    new CodeBlock("public string RequestId", "get", "set"),
                    "",
                    "public bool ShowRequestId => !string.IsNullOrEmpty(RequestId)",
                    "",
                    new CodeBlock("public void OnGet()",
                        "RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier"))),
            "");
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}
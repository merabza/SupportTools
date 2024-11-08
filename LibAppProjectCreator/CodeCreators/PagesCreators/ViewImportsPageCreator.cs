using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.PagesCreators;

public sealed class ViewImportsPageCreator : CodeCreator
{
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ViewImportsPageCreator(ILogger logger, string placePath, string projectName, string? codeFileName = null) :
        base(logger, placePath, codeFileName)
    {
        _projectName = projectName;
    }

    public override void CreateFileStructure()
    {
        var text = new TextCode(@$"@using {_projectName}
@namespace {_projectName}.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers");
        CodeFile.Add(text);
        FinishAndSave();
    }
}
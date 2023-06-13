﻿using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.PagesCreators;

public sealed class ErrorPageCreator : CodeCreator
{
    public ErrorPageCreator(ILogger logger, string placePath, string? codeFileName = null) : base(logger, placePath,
        codeFileName)
    {
    }

    public override void CreateFileStructure()
    {
        var text = new TextCode(@"@page
@model ErrorModel
@{
    ViewData[""Title""] = ""Error"";
}

<h1 sealed class=""text-danger"">Error.</h1>
<h2 sealed class=""text-danger"">An error occurred while processing your request.</h2>

@if (Model.ShowRequestId)
{
    <p>
        <strong>Request ID:</strong> <code>@Model.RequestId</code>
    </p>
}

<h3>Development Mode</h3>
<p>
    Swapping to the <strong>Development</strong> environment displays detailed information about the error that occurred.
</p>
<p>
    <strong>The Development environment shouldn't be enabled for deployed applications.</strong>
    It can result in displaying sensitive information from exceptions to end users.
    For local debugging, enable the <strong>Development</strong> environment by setting the <strong>ASPNETCORE_ENVIRONMENT</strong> environment variable to <strong>Development</strong>
    and restarting the app.
</p>
");
        CodeFile.Add(text);
        FinishAndSave();
    }
}
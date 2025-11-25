using FluentValidation;
using SupportToolsData.Models;

namespace SupportTools.Validators;

public sealed class EndpointModelValidator : AbstractValidator<EndpointModel>
{
    public EndpointModelValidator()
    {
        //RuleFor(x => x.Root).NotEmpty().WithMessage("Root cannot be empty.").Matches(@"^[a-zA-Z0-9\-]+$")
        //    .WithMessage("Root can only contain alphanumeric characters and hyphens.");

        //RuleFor(x => x.Version).NotEmpty().WithMessage("Version cannot be empty.").Matches(@"^[a-zA-Z0-9\-\.]+$")
        //    .WithMessage("Version can only contain alphanumeric characters, hyphens, and dots.");

        //RuleFor(x => x.Base).NotEmpty().WithMessage("Base cannot be empty.").Matches(@"^[a-zA-Z0-9\-\/]+$")
        //    .WithMessage("Base can only contain alphanumeric characters, hyphens, and slashes.");

        RuleFor(x => x.EndpointName).NotEmpty().WithMessage("EndpointName cannot be empty.")
            .Matches(@"^[a-zA-Z0-9\-]+$")
            .WithMessage("EndpointName can only contain alphanumeric characters and hyphens.");

        //RuleFor(x => x.HttpMethod).NotEmpty().WithMessage("HttpMethod cannot be empty.").Must(method =>
        //    !string.IsNullOrWhiteSpace(method) && new List<string>
        //    {
        //        "GET",
        //        "POST",
        //        "PUT",
        //        "DELETE",
        //        "PATCH"
        //    }.Contains(method!.ToUpper())).WithMessage("HttpMethod must be one of GET, POST, PUT, DELETE, PATCH.");

        RuleFor(x => x.ReturnType).NotEmpty().WithMessage("ReturnType cannot be empty.")
            .Matches(@"^[a-zA-Z0-9\.\[\]]+$")
            .WithMessage("ReturnType can only contain alphanumeric characters, dots, and square brackets.");
    }
}
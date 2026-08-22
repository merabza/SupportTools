using SystemTools.SystemToolsShared.Errors;

namespace SupportTools.Errors;

public static class DotnetToolsManagerErrors
{
    public static readonly ErrorOmd CreateListOfDotnetToolsError = new()
    {
        Code = nameof(CreateListOfDotnetToolsError), Name = "ErrorOmd when Create List Of Dotnet Tools"
    };

    public static readonly ErrorOmd CreateListOfDotnetToolsInstalledError = new()
    {
        Code = nameof(CreateListOfDotnetToolsInstalledError),
        Name = "ErrorOmd when Create List Of Dotnet Tools Installed"
    };

    public static readonly ErrorOmd PackageIdIsEmpty = new()
    {
        Code = nameof(PackageIdIsEmpty), Name = "Package Id Is Empty"
    };

    public static readonly ErrorOmd GetAvailableVersionOfToolError = new()
    {
        Code = nameof(GetAvailableVersionOfToolError), Name = "ErrorOmd when detect Available Version Of Tool"
    };

    public static ErrorOmd CheckVersionsForOneToolError(string toolName)
    {
        return new ErrorOmd
        {
            Code = nameof(CheckVersionsForOneToolError),
            Name = $"ErrorOmd when Check Versions Of package {toolName}"
        };
    }
}

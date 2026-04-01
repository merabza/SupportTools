using SystemTools.SystemToolsShared.Errors;

namespace SupportTools.Errors;

public static class DotnetToolsManagerErrors
{
    public static readonly Error CreateListOfDotnetToolsError = new()
    {
        Code = nameof(CreateListOfDotnetToolsError), Name = "Error when Create List Of Dotnet Tools"
    };

    public static readonly Error CreateListOfDotnetToolsInstalledError = new()
    {
        Code = nameof(CreateListOfDotnetToolsInstalledError),
        Name = "Error when Create List Of Dotnet Tools Installed"
    };

    public static readonly Error PackageIdIsEmpty = new()
    {
        Code = nameof(PackageIdIsEmpty), Name = "Package Id Is Empty"
    };

    public static readonly Error GetAvailableVersionOfToolError = new()
    {
        Code = nameof(GetAvailableVersionOfToolError), Name = "Error when detect Available Version Of Tool"
    };

    public static Error CheckVersionsForOneToolError(string toolName)
    {
        return new Error
        {
            Code = nameof(CheckVersionsForOneToolError), Name = $"Error when Check Versions Of package {toolName}"
        };
    }
}

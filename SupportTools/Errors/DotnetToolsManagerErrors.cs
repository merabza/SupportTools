using SystemToolsShared.Errors;

namespace SupportTools.Errors;

public static class DotnetToolsManagerErrors
{
    public static readonly Err CreateListOfDotnetToolsError = new()
    {
        ErrorCode = nameof(CreateListOfDotnetToolsError), ErrorMessage = "Error when Create List Of Dotnet Tools"
    };

    public static readonly Err CreateListOfDotnetToolsInstalledError = new()
    {
        ErrorCode = nameof(CreateListOfDotnetToolsInstalledError),
        ErrorMessage = "Error when Create List Of Dotnet Tools Installed"
    };

    public static readonly Err PackageIdIsEmpty = new()
    {
        ErrorCode = nameof(PackageIdIsEmpty),
        ErrorMessage = "Package Id Is Empty"
    };

    public static readonly Err GetAvailableVersionOfToolError = new()
    {
        ErrorCode = nameof(GetAvailableVersionOfToolError),
        ErrorMessage = "Error when detect Available Version Of Tool"
    };

    public static Err CheckVersionsForOneToolError(string toolName) => new()
    {
        ErrorCode = nameof(CheckVersionsForOneToolError),
        ErrorMessage = $"Error when Check Versions Of package {toolName}"
    };

    
}
using SystemTools.SharedKernel;

namespace SupportTools.Errors;

public static class DotnetToolsManagerErrors
{
    public static readonly Error CreateListOfDotnetToolsError =
        Error.Problem(nameof(CreateListOfDotnetToolsError), "Error when Create List Of Dotnet Tools");

    public static readonly Error CreateListOfDotnetToolsInstalledError =
        Error.Problem(nameof(CreateListOfDotnetToolsInstalledError), "Error when Create List Of Dotnet Tools Installed");

    public static readonly Error PackageIdIsEmpty = Error.Problem(nameof(PackageIdIsEmpty), "Package Id Is Empty");

    public static readonly Error GetAvailableVersionOfToolError =
        Error.Problem(nameof(GetAvailableVersionOfToolError), "Error when detect Available Version Of Tool");

    public static Error CheckVersionsForOneToolError(string toolName)
    {
        return Error.Problem(nameof(CheckVersionsForOneToolError), $"Error when Check Versions Of package {toolName}");
    }
}

using LibSupportToolsServerWork.Registry.Sync;
using SupportToolsServerApiContracts.Errors;
using SystemTools.ApiContracts.Errors;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Sync;

public sealed class RegistrySyncServerErrorCodesTests
{
    //ApiClient returns this error when the request does not reach the server or times out
    [Fact]
    public void RequestFailed_IsTheCodeOfApiClientRequestFailedError()
    {
        // Arrange
        Error error = ApiClientErrors.ApiRequestFailed("http://sts/api: connection refused");

        // Act
        string result = RegistrySyncServerErrorCodes.RequestFailed;

        // Assert
        Assert.Equal(error.Code, result);
    }

    //B1's generic factories: the server answers with them, and the adapters that check the version themselves
    //create their errors with them too
    [Fact]
    public void ConcurrencyConflict_IsTheCodeOfTheServerFactory()
    {
        // Arrange
        Error error = SupportToolsServerApiClientErrors.ConcurrencyConflict("Environment", "Production", 1, 2);

        // Act
        string result = RegistrySyncServerErrorCodes.ConcurrencyConflict;

        // Assert
        Assert.Equal(error.Code, result);
    }

    [Fact]
    public void RecordWithNameNotFound_IsTheCodeOfTheServerFactory()
    {
        // Arrange
        Error error = SupportToolsServerApiClientErrors.RecordWithNameNotFound("Environment", "Production");

        // Act
        string result = RegistrySyncServerErrorCodes.RecordWithNameNotFound;

        // Assert
        Assert.Equal(error.Code, result);
    }
}

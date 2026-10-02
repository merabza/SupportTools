using LibSupportToolsServerWork.Registry.Sync;
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
}

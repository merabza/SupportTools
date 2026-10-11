using System;
using System.Net.Http;
using Moq;
using SupportTools.AutoSync;
using Xunit;

namespace SupportTools.Tests.AutoSync;

public sealed class TimeoutHttpClientFactoryTests
{
    [Fact]
    public void CreateClient_WhenCalled_SetsTheTimeoutOnTheClientOfTheInnerFactory()
    {
        // Arrange
        using var client = new HttpClient();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient("named")).Returns(client);
        var sut = new TimeoutHttpClientFactory(httpClientFactory.Object, TimeSpan.FromSeconds(7));

        // Act
        HttpClient result = sut.CreateClient("named");

        // Assert
        Assert.Same(client, result);
        Assert.Equal(TimeSpan.FromSeconds(7), result.Timeout);
    }
}

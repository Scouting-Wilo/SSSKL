using System.Net;
using FluentAssertions;
using SSSKL.Mobile.Domain.Auth;
using SSSKL.Mobile.Infrastructure.Auth;
using SSSKL.Mobile.Test.Util;

namespace SSSKL.Mobile.Test.Infrastructure.Auth;

[TestClass]
public class IdentityTokenRefresherTests
{
    [TestMethod]
    public async Task RefreshAsync_Success_ReturnsNewTokens()
    {
        // Arrange
        var inner = new StubHttpHandler((_, _) => StubHttpHandler.Json(
            """{"tokenType":"Bearer","accessToken":"a2","expiresIn":3600,"refreshToken":"r2"}"""));
        using var http = new HttpClient(inner) { BaseAddress = new Uri("https://api.test/") };
        var sut = new IdentityTokenRefresher(http);

        // Act
        var result = await sut.RefreshAsync("r1", TestContext.CancellationToken);

        // Assert
        result.Should().Be(new TokenSet("a2", "r2"));
        inner.Requests.Single().Request.RequestUri!.AbsolutePath.Should().Be("/api/v1/identity/refresh");
        inner.Requests.Single().Body.Should().Be("""{"refreshToken":"r1"}""");
    }

    [TestMethod]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.BadRequest)]
    public async Task RefreshAsync_Rejected_ReturnsNull(HttpStatusCode status)
    {
        // Arrange
        var inner = new StubHttpHandler((_, _) => StubHttpHandler.Status(status));
        using var http = new HttpClient(inner) { BaseAddress = new Uri("https://api.test/") };
        var sut = new IdentityTokenRefresher(http);

        // Act
        var result = await sut.RefreshAsync("r1", TestContext.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task RefreshAsync_ServerError_Throws()
    {
        // Arrange
        var inner = new StubHttpHandler((_, _) => StubHttpHandler.Status(HttpStatusCode.InternalServerError));
        using var http = new HttpClient(inner) { BaseAddress = new Uri("https://api.test/") };
        var sut = new IdentityTokenRefresher(http);

        // Act
        var act = () => sut.RefreshAsync("r1", TestContext.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    public TestContext TestContext { get; set; } = null!;
}

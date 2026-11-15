using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SSSKL.Domain.Constants;
using SSSKL.Web.Test.Integration.Infrastructure;
using SSSKL.Web.Test.Integration.Infrastructure.Fakes;

namespace SSSKL.Web.Test.Integration;

/// <summary>How the links in identity emails are built from WEBSITE_DOMAIN when it has no scheme.</summary>
[TestClass]
public class FrontendLinkIntegrationTests : IntegrationTestBase
{
    private static SssklWebApplicationFactory _factory = null!;
    protected override SssklWebApplicationFactory Factory => _factory;

    [ClassInitialize]
    public static void Initialize(TestContext _) =>
        _factory = new SssklWebApplicationFactory().WithSetting("WEBSITE_DOMAIN", "localhost:4200/");

    [ClassCleanup]
    public static async Task Cleanup() => await _factory.DisposeAsync();

    [TestMethod]
    public async Task ForgotPassword_LocalhostDomainWithoutScheme_LinksOverHttp()
    {
        var user = await CreateUserAsync(Roles.User);

        var response = await AnonymousClient().PostAsJsonAsync("/api/v1/identity/forgotPassword", new { email = user.Email }, CT);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Factory.Emails.LastFor(user.Email!, FakeEmailSender.ResetLink)!.Content
            .Should().StartWith("http://localhost:4200/reset-password?");
    }
}

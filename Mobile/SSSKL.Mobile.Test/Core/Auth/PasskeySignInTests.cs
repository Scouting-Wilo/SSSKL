using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Domain.Auth;
using SSSKL.Mobile.Infrastructure.Auth;
using SSSKL.Mobile.Test.Util;

namespace SSSKL.Mobile.Test.Core.Auth;

[TestClass]
public class PasskeySignInTests
{
    private const string Options = """{"challenge":"AAEC","rpId":"ssskl.test","allowCredentials":[]}""";
    private const string Assertion = """{"id":"abc","type":"public-key"}""";
    private const string Tokens = """{"tokenType":"Bearer","accessToken":"a1","expiresIn":3600,"refreshToken":"r1"}""";

    private sealed class FakeAuthenticator(Func<string, string> respond, bool supported = true) : IPasskeyAuthenticator
    {
        public List<string> Requests { get; } = [];
        public bool IsSupported => supported;

        public Task<string> GetAssertionAsync(string requestOptionsJson, CancellationToken cancellationToken)
        {
            Requests.Add(requestOptionsJson);
            return Task.FromResult(respond(requestOptionsJson));
        }
    }

    private static PasskeySignIn Create(StubHttpHandler handler, IPasskeyAuthenticator authenticator) =>
        // With the real Infrastructure adapter, so the HTTP side (paths, status codes, tokens) is covered too.
        new(new PasskeyApi(() => new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.test/") }),
            authenticator, NullLogger<PasskeySignIn>.Instance);

    private static StubHttpHandler Api(HttpStatusCode assertionStatus = HttpStatusCode.OK, string assertionBody = Tokens) =>
        new((request, _) => request.Method == HttpMethod.Get
            ? StubHttpHandler.Json(Options)
            : StubHttpHandler.Json(assertionBody, assertionStatus));

    [TestMethod]
    public async Task SignInAsync_Success_PostsAssertionAndReturnsTokens()
    {
        // Arrange
        var api = Api();
        var authenticator = new FakeAuthenticator(_ => Assertion);
        var sut = Create(api, authenticator);

        // Act
        var result = await sut.SignInAsync(TestContext.CancellationToken);

        // Assert
        result.Should().Be(new PasskeySignInResult(PasskeySignInStatus.Success, new TokenSet("a1", "r1")));
        authenticator.Requests.Should().Equal(Options);
        api.Requests.Should().HaveCount(2);
        api.Requests[0].Request.RequestUri!.PathAndQuery.Should().Be("/api/v1/identity/PasskeyRequestOptions");
        api.Requests[1].Request.Method.Should().Be(HttpMethod.Post);
        api.Requests[1].Request.RequestUri!.PathAndQuery.Should().Be("/api/v1/identity/PasskeyAssertion");
        api.Requests[1].Body.Should().Be(Assertion);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.BadRequest)]
    public async Task SignInAsync_ApiRejects_Rejected(HttpStatusCode status)
    {
        var sut = Create(Api(status, "{}"), new FakeAuthenticator(_ => Assertion));

        var result = await sut.SignInAsync(TestContext.CancellationToken);

        result.Status.Should().Be(PasskeySignInStatus.Rejected);
        result.Tokens.Should().BeNull();
    }

    [TestMethod]
    public async Task SignInAsync_UserCancels_CanceledWithoutPosting()
    {
        var api = Api();
        var sut = Create(api, new FakeAuthenticator(_ => throw new PasskeyCanceledException()));

        var result = await sut.SignInAsync(TestContext.CancellationToken);

        result.Status.Should().Be(PasskeySignInStatus.Canceled);
        api.Requests.Should().ContainSingle();
    }

    [TestMethod]
    public async Task SignInAsync_NoPasskeyOnDevice_NoPasskey()
    {
        var sut = Create(Api(), new FakeAuthenticator(_ => throw new PasskeyUnavailableException()));

        (await sut.SignInAsync(TestContext.CancellationToken)).Status.Should().Be(PasskeySignInStatus.NoPasskey);
    }

    [TestMethod]
    public async Task SignInAsync_Unsupported_NoPasskeyWithoutCallingApi()
    {
        var api = Api();
        var sut = Create(api, new FakeAuthenticator(_ => Assertion, supported: false));

        (await sut.SignInAsync(TestContext.CancellationToken)).Status.Should().Be(PasskeySignInStatus.NoPasskey);
        sut.IsSupported.Should().BeFalse();
        api.Requests.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SignInAsync_OptionsServerError_Failed()
    {
        var authenticator = new FakeAuthenticator(_ => Assertion);
        var sut = Create(new StubHttpHandler((_, _) => StubHttpHandler.Status(HttpStatusCode.InternalServerError)), authenticator);

        (await sut.SignInAsync(TestContext.CancellationToken)).Status.Should().Be(PasskeySignInStatus.Failed);
        authenticator.Requests.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SignInAsync_NoTokensInResponse_Failed()
    {
        var sut = Create(Api(assertionBody: "{}"), new FakeAuthenticator(_ => Assertion));

        (await sut.SignInAsync(TestContext.CancellationToken)).Status.Should().Be(PasskeySignInStatus.Failed);
    }

    [TestMethod]
    public async Task SignInAsync_PlatformFailure_Failed()
    {
        var sut = Create(Api(), new FakeAuthenticator(_ => throw new PasskeyPlatformException("boom", new InvalidOperationException("boom"))));

        (await sut.SignInAsync(TestContext.CancellationToken)).Status.Should().Be(PasskeySignInStatus.Failed);
    }

    [TestMethod]
    public async Task SignInAsync_NewClientPerAttempt()
    {
        var api = Api();
        var created = 0;
        var sut = new PasskeySignIn(
            new PasskeyApi(() => { created++; return new HttpClient(api, disposeHandler: false) { BaseAddress = new Uri("https://api.test/") }; }),
            new FakeAuthenticator(_ => Assertion), NullLogger<PasskeySignIn>.Instance);

        await sut.SignInAsync(TestContext.CancellationToken);
        await sut.SignInAsync(TestContext.CancellationToken);

        // Each attempt gets a fresh cookie container (the API's challenge cookie).
        created.Should().Be(2);
    }

    public TestContext TestContext { get; set; } = null!;
}

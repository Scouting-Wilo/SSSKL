using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SSSKL.Web.Registrations;

namespace SSSKL.Web.Test.Registrations;

[TestClass]
public class PasskeyOriginPolicyTests
{
    private const string AndroidOrigin = "android:apk-key-hash:yXZ85NFSZuigZpM69pbIeHMTduDd5CZnEU3oU7rzTA8";

    private readonly PasskeyOriginPolicy _policy = new("ssskl.example.nl", [AndroidOrigin, " ", "https://other.example.nl/"]);

    private static PasskeyOriginValidationContext Context(string origin, string? requestOrigin = null, bool crossOrigin = false)
    {
        var httpContext = new DefaultHttpContext();
        if (requestOrigin is not null)
        {
            httpContext.Request.Headers.Origin = requestOrigin;
        }
        return new PasskeyOriginValidationContext
        {
            HttpContext = httpContext,
            Origin = origin,
            CrossOrigin = crossOrigin,
        };
    }

    [TestMethod]
    public void IsAllowed_OriginMatchesRequestOriginHeader_True()
    {
        // The SPA in development, on an origin that isn't configured.
        _policy.IsAllowed(Context("http://localhost:4200", requestOrigin: "http://localhost:4200")).Should().BeTrue();
    }

    [TestMethod]
    public void IsAllowed_OriginDiffersFromRequestOriginHeader_False()
    {
        _policy.IsAllowed(Context("https://evil.example.com", requestOrigin: "http://localhost:4200")).Should().BeFalse();
    }

    [TestMethod]
    public void IsAllowed_WebsiteOriginWithoutOriginHeader_True()
    {
        // The iOS app: signs for the website's origin, its HttpClient sends no Origin header.
        _policy.IsAllowed(Context("https://ssskl.example.nl")).Should().BeTrue();
    }

    [TestMethod]
    public void IsAllowed_ConfiguredOrigins_True()
    {
        _policy.IsAllowed(Context(AndroidOrigin)).Should().BeTrue();
        _policy.IsAllowed(Context("https://other.example.nl")).Should().BeTrue();
    }

    [TestMethod]
    public void IsAllowed_UnknownOriginWithoutOriginHeader_False()
    {
        _policy.IsAllowed(Context("android:apk-key-hash:somebodyElse")).Should().BeFalse();
        _policy.IsAllowed(Context("http://ssskl.example.nl")).Should().BeFalse();
    }

    [TestMethod]
    public void IsAllowed_CrossOrigin_False()
    {
        _policy.IsAllowed(Context("https://ssskl.example.nl", requestOrigin: "https://ssskl.example.nl", crossOrigin: true))
            .Should().BeFalse();
    }

    [TestMethod]
    public void IsAllowed_NoExtraOrigins_OnlyWebsite()
    {
        var policy = new PasskeyOriginPolicy("ssskl.example.nl", null);

        policy.IsAllowed(Context("https://ssskl.example.nl")).Should().BeTrue();
        policy.IsAllowed(Context(AndroidOrigin)).Should().BeFalse();
    }
}

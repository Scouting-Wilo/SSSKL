using System.Text.Json;
using FluentAssertions;
using SSSKL.Mobile.Core.Auth;

namespace SSSKL.Mobile.Test.Auth;

[TestClass]
public class PasskeyJsonTests
{
    [TestMethod]
    public void ParseRequestOptions_IdentityOptions_ReadsValues()
    {
        // As ASP.NET Core Identity's MakePasskeyRequestOptionsAsync writes them.
        const string json = """
            {"challenge":"AAECAwQ","timeout":60000,"rpId":"ssskl.scoutingwilo.nl",
             "allowCredentials":[{"type":"public-key","id":"_-8","transports":["internal"]}],
             "userVerification":"preferred"}
            """;

        var options = PasskeyJson.ParseRequestOptions(json);

        options.Challenge.Should().Equal(0, 1, 2, 3, 4);
        options.RelyingPartyId.Should().Be("ssskl.scoutingwilo.nl");
        options.AllowedCredentialIds.Should().ContainSingle().Which.Should().Equal(0xFF, 0xEF);
        options.UserVerification.Should().Be("preferred");
    }

    [TestMethod]
    public void ParseRequestOptions_WrappedWithoutAllowList_ReadsValues()
    {
        var options = PasskeyJson.ParseRequestOptions("""{"publicKey":{"challenge":"AQ","rpId":"example.nl"}}""");

        options.Challenge.Should().Equal(1);
        options.RelyingPartyId.Should().Be("example.nl");
        options.AllowedCredentialIds.Should().BeEmpty();
        options.UserVerification.Should().BeNull();
    }

    [TestMethod]
    public void ParseRequestOptions_NoRpId_Throws()
    {
        var act = () => PasskeyJson.ParseRequestOptions("""{"challenge":"AQ","rpId":""}""");

        act.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void SerializeAssertion_WritesWebAuthnJson()
    {
        var json = PasskeyJson.SerializeAssertion([0xFF, 0xEF], [1], [2], [3], [4]);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.GetProperty("id").GetString().Should().Be("_-8");
        root.GetProperty("rawId").GetString().Should().Be("_-8");
        root.GetProperty("type").GetString().Should().Be("public-key");
        root.GetProperty("authenticatorAttachment").GetString().Should().Be("platform");
        root.GetProperty("clientExtensionResults").EnumerateObject().Should().BeEmpty();
        var response = root.GetProperty("response");
        response.GetProperty("clientDataJSON").GetString().Should().Be("AQ");
        response.GetProperty("authenticatorData").GetString().Should().Be("Ag");
        response.GetProperty("signature").GetString().Should().Be("Aw");
        response.GetProperty("userHandle").GetString().Should().Be("BA");
    }

    [TestMethod]
    public void SerializeAssertion_NoUserHandleOrAttachment_WritesNulls()
    {
        var json = PasskeyJson.SerializeAssertion([1], [1], [2], [3], userHandle: null, authenticatorAttachment: null);

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("response").GetProperty("userHandle").ValueKind.Should().Be(JsonValueKind.Null);
        document.RootElement.GetProperty("authenticatorAttachment").ValueKind.Should().Be(JsonValueKind.Null);
    }
}

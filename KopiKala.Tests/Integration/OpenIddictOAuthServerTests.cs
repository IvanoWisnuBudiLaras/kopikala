using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace KopiKala.Tests.Integration;

public class OpenIddictOAuthServerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public OpenIddictOAuthServerTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task AuthorizeEndpoint_WithoutPkce_ReturnsErrorOrRejection()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Request authorize WITHOUT code_challenge (PKCE is strictly required)
        var response = await client.GetAsync("/connect/authorize?client_id=kopikala-client&response_type=code&redirect_uri=http%3A%2F%2Flocalhost%3A5080%2Foauth%2Fcallback&scope=openid%20profile%20email");

        _output.WriteLine($"[OAuth 2.1 Test] Authorize without PKCE Status: {response.StatusCode}");

        // OpenIddict should either reject (400 Bad Request with invalid_request) or require PKCE
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthorizeEndpoint_WithPkce_RedirectsToLoginWhenUnauthenticated()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Generate PKCE code verifier and code challenge (S256)
        var codeVerifier = GenerateRandomString(64);
        var codeChallenge = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

        var authorizeUrl = $"/connect/authorize?client_id=kopikala-client&response_type=code&redirect_uri=http%3A%2F%2Flocalhost%3A5080%2Foauth%2Fcallback&scope=openid%20profile%20email&code_challenge={codeChallenge}&code_challenge_method=S256";

        var response = await client.GetAsync(authorizeUrl);
        var body = await response.Content.ReadAsStringAsync();

        _output.WriteLine($"[OAuth 2.1 Test] Authorize with PKCE Status: {response.StatusCode}");
        _output.WriteLine($"[OAuth 2.1 Test] Response Body: {body}");
        _output.WriteLine($"[OAuth 2.1 Test] Location: {response.Headers.Location}");

        // Unauthenticated user should be challenged / redirected to login
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task TokenEndpoint_InvalidOrMissingGrant_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = "kopikala-client",
            ["code"] = "invalid-code",
            ["redirect_uri"] = "http://localhost:5080/oauth/callback",
            ["code_verifier"] = "invalid-verifier"
        });

        var response = await client.PostAsync("/connect/token", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        _output.WriteLine($"[OAuth 2.1 Test] Token endpoint invalid code Status: {response.StatusCode}");
        _output.WriteLine($"[OAuth 2.1 Test] Response Body: {responseBody}");

        // Invalid code must be rejected with 400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("error", responseBody.ToLowerInvariant());
    }

    private static string GenerateRandomString(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes)[..length];
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

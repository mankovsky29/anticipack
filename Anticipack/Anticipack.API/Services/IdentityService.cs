using Anticipack.API.Models;
using Google.Apis.Auth;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace Anticipack.API.Services;

public class IdentityService : IIdentityService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _appleConfigurationManager;

    public IdentityService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;

        _appleConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            "https://appleid.apple.com/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });
    }

    public Task<ExternalIdentityResult?> VerifyIdentityTokenAsync(AuthProvider provider, string identityToken, CancellationToken cancellationToken = default)
        => provider switch
        {
            AuthProvider.Google => VerifyGoogleAsync(identityToken),
            AuthProvider.Facebook => VerifyFacebookAsync(identityToken, cancellationToken),
            AuthProvider.Apple => VerifyAppleAsync(identityToken, cancellationToken),
            _ => Task.FromResult<ExternalIdentityResult?>(null)
        };

    private async Task<ExternalIdentityResult?> VerifyGoogleAsync(string identityToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["Authentication:Google:ClientId"] ?? string.Empty }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(identityToken, settings);
            if (string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email))
            {
                return null;
            }

            return new ExternalIdentityResult(payload.Subject, payload.Email, payload.Name, payload.Picture);
        }
        catch
        {
            return null;
        }
    }

    private async Task<ExternalIdentityResult?> VerifyFacebookAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var endpoint = $"https://graph.facebook.com/me?fields=id,name,email,picture&access_token={Uri.EscapeDataString(accessToken)}";
            using var response = await client.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var root = document.RootElement;
            if (!root.TryGetProperty("id", out var idElement) || !root.TryGetProperty("email", out var emailElement))
            {
                return null;
            }

            var id = idElement.GetString();
            var email = emailElement.GetString();
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            string? name = root.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
            string? picture = null;

            if (root.TryGetProperty("picture", out var pictureElement) &&
                pictureElement.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("url", out var urlElement))
            {
                picture = urlElement.GetString();
            }

            return new ExternalIdentityResult(id, email, name, picture);
        }
        catch
        {
            return null;
        }
    }

    private async Task<ExternalIdentityResult?> VerifyAppleAsync(string identityToken, CancellationToken cancellationToken)
    {
        try
        {
            var configuration = await _appleConfigurationManager.GetConfigurationAsync(cancellationToken);
            var clientId = _configuration["Authentication:Apple:ClientId"];

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://appleid.apple.com",
                ValidateAudience = true,
                ValidAudience = clientId,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = configuration.SigningKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(identityToken, validationParameters, out _);

            var userId = principal.FindFirst("sub")?.Value;
            var email = principal.FindFirst("email")?.Value;
            var name = principal.FindFirst("name")?.Value;

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return new ExternalIdentityResult(userId, email, name, null);
        }
        catch
        {
            return null;
        }
    }
}

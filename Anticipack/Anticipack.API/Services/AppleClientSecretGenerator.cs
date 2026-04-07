using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Anticipack.API.Services;

public class AppleClientSecretGenerator
{
    private readonly IConfiguration _configuration;

    public AppleClientSecretGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateClientSecret()
    {
        var teamId = _configuration["Authentication:Apple:TeamId"];
        var clientId = _configuration["Authentication:Apple:ClientId"];
        var keyId = _configuration["Authentication:Apple:KeyId"];
        var privateKeyPem = _configuration["Authentication:Apple:PrivateKey"];

        if (string.IsNullOrWhiteSpace(teamId) ||
            string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(keyId) ||
            string.IsNullOrWhiteSpace(privateKeyPem))
        {
            throw new InvalidOperationException("Apple client secret configuration is incomplete.");
        }

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem.Replace("\\n", "\n"));

        var key = new ECDsaSecurityKey(ecdsa) { KeyId = keyId };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256);

        var now = DateTimeOffset.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Iss, teamId),
            new(JwtRegisteredClaimNames.Sub, clientId),
            new(JwtRegisteredClaimNames.Aud, "https://appleid.apple.com"),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = now.AddMonths(6).UtcDateTime,
            SigningCredentials = credentials
        };

        descriptor.AdditionalHeaderClaims = new Dictionary<string, object>
        {
            ["kid"] = keyId
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }
}

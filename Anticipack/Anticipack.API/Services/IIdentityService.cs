using Anticipack.API.Models;

namespace Anticipack.API.Services;

public interface IIdentityService
{
    Task<ExternalIdentityResult?> VerifyIdentityTokenAsync(AuthProvider provider, string identityToken, CancellationToken cancellationToken = default);
}

public sealed record ExternalIdentityResult(
    string ProviderUserId,
    string Email,
    string? Name,
    string? PictureUrl
);

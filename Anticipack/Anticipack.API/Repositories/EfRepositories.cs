using Anticipack.API.Data;
using Anticipack.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Anticipack.API.Repositories;

public class EfUserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EfUserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(string id)
        => _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByEmailAsync(string email)
        => _dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    public Task<User?> GetByExternalAuthIdAsync(string externalAuthId, AuthProvider provider)
    {
        return provider switch
        {
            AuthProvider.Google => _dbContext.Users.FirstOrDefaultAsync(u => u.GoogleId == externalAuthId),
            AuthProvider.Facebook => _dbContext.Users.FirstOrDefaultAsync(u => u.FacebookId == externalAuthId),
            AuthProvider.Apple => _dbContext.Users.FirstOrDefaultAsync(u => u.AppleId == externalAuthId),
            AuthProvider.Telegram => _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramId == externalAuthId),
            _ => _dbContext.Users.FirstOrDefaultAsync(u => u.ExternalAuthId == externalAuthId && u.AuthProvider == provider)
        };
    }

    public async Task<User> CreateAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
        {
            return false;
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}

public class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EfRefreshTokenRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        => _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();
        return refreshToken;
    }

    public async Task<RefreshToken> UpdateAsync(RefreshToken refreshToken)
    {
        _dbContext.RefreshTokens.Update(refreshToken);
        await _dbContext.SaveChangesAsync();
        return refreshToken;
    }
}

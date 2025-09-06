using DataAccess.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    
    private readonly ApplicationDbContext _db;

    public RefreshTokenRepository(ApplicationDbContext db) 
    {
        _db = db; 
    }
    public async Task AddAsync(RefreshToken token) => await _db.RefreshTokens.AddAsync(token);

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash) => await _db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash);

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();

    public async Task RevokeAsync(RefreshToken token)
    {
        token.Revoked = true;
        await _db.SaveChangesAsync();
    }
}

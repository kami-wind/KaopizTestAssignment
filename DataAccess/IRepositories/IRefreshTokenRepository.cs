using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.IRepositories;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> GetByHashAsync(string tokenHash);
    Task SaveChangesAsync();
    Task RevokeAsync(RefreshToken token);
}

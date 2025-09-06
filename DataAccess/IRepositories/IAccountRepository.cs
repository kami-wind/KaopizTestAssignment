using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.IRepositories;

public interface IAccountRepository
{
    Task<Account?> GetByEmailAsync(string email);
    Task<Account?> GetByIdAsync(Guid id);
    Task AddAsync(Account account);
    Task SaveChangesAsync();
}

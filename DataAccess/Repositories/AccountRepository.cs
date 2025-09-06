using DataAccess.IRepositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly ApplicationDbContext _db;

    public AccountRepository(ApplicationDbContext db) 
    {
        _db = db; 
    }
    public async Task<Account?> GetByEmailAsync(string email) => await _db.Accounts.SingleOrDefaultAsync(a => a.Email == email);

    public async Task<Account?> GetByIdAsync(Guid id) => await _db.Accounts.FindAsync(id);

    public async Task AddAsync(Account account) => await _db.Accounts.AddAsync(account);

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess;

public  class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string PasswordHash { get; set; } = null!; // hashed
    public UserType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public enum UserType { EndUser = 0, Admin = 1, Partner = 2 }

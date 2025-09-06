using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace BussinessLogic;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string name, string password, UserType type);
    Task<AuthResult> LoginAsync(string email, string password, bool rememberMe);
    Task<AuthResult> RefreshJwtAsync(string refreshToken);
    Task LogoutAsync(string refreshToken); // revoke refresh
    bool ValidatePassword(string password, out string? error);
}


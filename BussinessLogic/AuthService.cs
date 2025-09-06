using DataAccess;
using DataAccess.IRepositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BussinessLogic;

public class AuthService : IAuthService
{
    private readonly IAccountRepository _accountRepo;
    private readonly IRefreshTokenRepository _refreshRepo;
    private readonly IConfiguration _cfg;
    private readonly PasswordHasher<Account> _pwHasher = new();
    public AuthService(IAccountRepository accountRepo,

    IRefreshTokenRepository refreshRepo, IConfiguration cfg)
    {
        _accountRepo = accountRepo;
        _refreshRepo = refreshRepo;
        _cfg = cfg;
    }

    public bool ValidatePassword(string password, out string? error)
    {
        error = null;
        if (!PasswordValidator.IsValid(password))
        {
            error = "Password must be at least 8 chars, include one uppercase letter, one number and one special character.";
        return false;
        }
        return true;
    }

    public async Task<AuthResult> RegisterAsync(string email, string name,string password, UserType type)
    {
        if (!ValidatePassword(password, out var err))
        {
            return new AuthResult
            { Success = false, Error = err };
        }

        var existing = await _accountRepo.GetByEmailAsync(email);
        if (existing != null) return new AuthResult
        {
            Success = false,
            Error = "Email already in use"
        };

        var acc = new Account { Email = email, Name = name, Type = type };
        acc.PasswordHash = _pwHasher.HashPassword(acc, password);
        await _accountRepo.AddAsync(acc);
        await _accountRepo.SaveChangesAsync();
        return new AuthResult { Success = true, Account = acc };
    }

    public async Task<AuthResult> LoginAsync(string email, string password,bool rememberMe)
    {
        var acc = await _accountRepo.GetByEmailAsync(email);

        if (acc == null) return new AuthResult
        {
            Success = false,
            Error = "Invalid credentials"
        };

        var verify = _pwHasher.VerifyHashedPassword(acc, acc.PasswordHash, password);

        if (verify == PasswordVerificationResult.Failed)
        {
            return new AuthResult { Success = false, Error = "Invalid credentials" };
        }
        var jwt = GenerateJwt(acc);
        string? refreshPlain = null;

        if (rememberMe)
        {
            refreshPlain = GenerateRandomToken();
            var hashed = Hash(refreshPlain);
            var rt = new RefreshToken
            {
                AccountId = acc.Id,
                TokenHash = hashed,
                ExpiresAt = DateTime.UtcNow.AddDays(30) // rememberme for 1 month
            };
            await _refreshRepo.AddAsync(rt);
            await _refreshRepo.SaveChangesAsync();
        }

        return new AuthResult
        {
            Success = true,
            JwtToken = jwt,
            RefreshToken = refreshPlain,
            Account = acc
        };
    }

    public async Task<AuthResult> RefreshJwtAsync(string refreshTokenPlain)
    {
        var hash = Hash(refreshTokenPlain);
        var rt = await _refreshRepo.GetByHashAsync(hash);

        if (rt == null || rt.ExpiresAt < DateTime.UtcNow || rt.Revoked)
        { 
            return new AuthResult { Success = false, Error = "Invalid refresh token" }; 
        }

        var acc = await _accountRepo.GetByIdAsync(rt.AccountId);

        if (acc == null)
        {
            return new AuthResult
            {
                Success = false,
                Error = "Account not found"
            };
        }

        var jwt = GenerateJwt(acc);

        return new AuthResult
        {
            Success = true,
            JwtToken = jwt,
            Account = acc
        };

            }
    public async Task LogoutAsync(string refreshTokenPlain)
    {
        if (string.IsNullOrEmpty(refreshTokenPlain)) return;
        var hash = Hash(refreshTokenPlain);
        var rt = await _refreshRepo.GetByHashAsync(hash);
        if (rt != null)
        {
            await _refreshRepo.RevokeAsync(rt);
        }
    }


    // Helpers
    private string GenerateJwt(Account acc)
    {
        var keyStr = _cfg["Jwt:Key"] ?? throw new Exception("JWT Key missing");
   
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        var creds = new SigningCredentials(key,
        SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
        new Claim(JwtRegisteredClaimNames.Sub, acc.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, acc.Email),
        new Claim("name", acc.Name),
        new Claim("userType", acc.Type.ToString())
        };
        var token = new JwtSecurityToken(
        issuer: _cfg["Jwt:Issuer"],
        audience: _cfg["Jwt:Audience"],
        claims: claims,
    
        expires: DateTime.UtcNow.AddMinutes(1), // 1 minute for testing
        signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRandomToken()
    {
        var b = new byte[64];
        RandomNumberGenerator.Fill(b);
        return  Convert.ToBase64String(b);
    }

    private static string Hash(string s)
    {
        using var sha = SHA256.Create();
        var data = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        return Convert.ToBase64String(data);
    }
}

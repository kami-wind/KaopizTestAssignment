using BussinessLogic;
using DataAccess;
using DataAccess.IRepositories;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace KaopizTestAssignment;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        // Configurations
        builder.Configuration.AddJsonFile("appsettings.json", false, true);

        // Dbcontext
        builder.Services.AddDbContext<ApplicationDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        //Repositories
        builder.Services.AddScoped<IAccountRepository, AccountRepository>();
        builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // BusinessLogic
        builder.Services.AddScoped<IAuthService, AuthService>();

        // JWT authentication
        var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("Jwt:Key is missing");
        var key = Encoding.UTF8.GetBytes(jwtKey);

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).
        AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    if (ctx.Request.Cookies.TryGetValue("AuthToken", out var token))
                    {
                        ctx.Token = token;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();

        app.Use(async (context, next) =>
        {
            // Only attempt auto-refresh on GET requests (avoid interfering with POST/API)
            if (!HttpMethods.IsGet(context.Request.Method))
            {
                await next();
                return;
            }

            // If already authenticated, nothing to do
            if (context.User?.Identity?.IsAuthenticated ?? false)
            {
                await next();
                return;
            }

            // Paths we treat as "login page": root or Auth/Login
            var path = context.Request.Path.Value ?? "/";
            var isLoginPath = path.Equals("/", StringComparison.OrdinalIgnoreCase)
                              || path.Equals("/Auth", StringComparison.OrdinalIgnoreCase)
                              || path.Equals("/Auth/Login", StringComparison.OrdinalIgnoreCase);

            if (!isLoginPath)
            {
                await next();
                return;
            }

            // If there's a refresh token cookie, try to refresh
            if (context.Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
            {
                try
                {
                    var authSvc = context.RequestServices.GetRequiredService<IAuthService>();
                    var res = await authSvc.RefreshJwtAsync(refreshToken);
                    if (res.Success && !string.IsNullOrEmpty(res.JwtToken))
                    {
                        // Set new JWT cookie (client will be authenticated on subsequent requests)
                        context.Response.Cookies.Append("AuthToken", res.JwtToken, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTimeOffset.UtcNow.AddMinutes(1)
                        });

                        context.Response.Redirect("/Home/Index");
                        return; 
                    }
                }
                catch
                {
                    
                }
            }

            await next();
        });

        app.UseAuthorization();


        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Auth}/{action=Login}/{id?}");


        app.Run();
    }
}

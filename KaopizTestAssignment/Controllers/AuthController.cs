using BussinessLogic;
using DataAccess;
using KaopizTestAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaopizTestAssignment.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User?.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToAction("Index", "Home");

        }


        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginPost(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var res = await _auth.LoginAsync(model.Email.Trim(), model.Password, model.RememberMe);

        if (!res.Success)
        {
            ModelState.AddModelError(string.Empty, res.Error ?? "Login failed");
            return View("Login");
        }

        Response.Cookies.Append("AuthToken", res.JwtToken!, new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = System.DateTimeOffset.UtcNow.AddMinutes(1) // 1 minute for testing
        }); ;

        if (model.RememberMe && !string.IsNullOrEmpty(res.RefreshToken))
        {
            Response.Cookies.Append("RefreshToken", res.RefreshToken, new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = System.DateTimeOffset.UtcNow.AddDays(30) // rememberme for 1 month
            });
        }

        return RedirectToAction("Index", "Home");
    }


    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterPost(string email, string name, string password, string userType)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name) || string .IsNullOrEmpty(password))

        {
            ModelState.AddModelError(string.Empty, "All fields can not be null");
            return View();
        }

        var parsed = System.Enum.TryParse<UserType>(userType, true, out var type) ? type : UserType.EndUser;
        var res = await _auth.RegisterAsync(email.Trim(), name.Trim(), password.Trim(), parsed);

        if (!res.Success)
        {
            ModelState.AddModelError(string.Empty, res.Error ?? "Register failed");
            return View("Register");
        }

        return RedirectToAction("Login");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue("RefreshToken", out var refresh))
        {
            await _auth.LogoutAsync(refresh);
            Response.Cookies.Delete("RefreshToken");
        }

        Response.Cookies.Delete("AuthToken");
        return RedirectToAction("Login");
    }
}

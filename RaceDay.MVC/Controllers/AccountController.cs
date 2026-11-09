using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;


public class AccountController : BaseController
{
    public AccountController(RaceDayApiClient api) : base(api) { }
    [HttpGet] public IActionResult Login() => View(new LoginViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        using var response = await Api.SendAsync(HttpMethod.Post, "api/Auth/login", model);
        if (!response.IsSuccessStatusCode) { ModelState.AddModelError("", await Api.ErrorAsync(response)); return View(model); }
        var user = await Api.ReadAsync<UserSummary>(response);
        if (user == null) { ModelState.AddModelError("", "The API returned an invalid response."); return View(model); }
        HttpContext.Session.SetInt32("UserID", user.UserID); HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetString("FullName", $"{user.FirstName} {user.LastName}");
        return user.Role == "Organiser" ? RedirectToAction("Dashboard", "Organiser") : RedirectToAction("MyEvents", "Participant");
    }
    [HttpGet] public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        using var response = await Api.SendAsync(HttpMethod.Post, "api/Auth/forgot-password", new { email = model.Email });
        if (!response.IsSuccessStatusCode) { ModelState.AddModelError("", await Api.ErrorAsync(response)); return View(model); }
        var result = await Api.ReadAsync<ForgotPasswordResponse>(response);
        model.Message = result?.Message ?? "If an account exists for that email address, a password reset link has been sent.";
        model.ResetLink = result?.ResetLink;
        ModelState.Clear();
        return View(model);
    }
    [HttpGet]
    public IActionResult ResetPassword(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["Error"] = "That password reset link is not valid. Please request a new one.";
            return RedirectToAction(nameof(ForgotPassword));
        }
        return View(new ResetPasswordViewModel { Token = token });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        using var response = await Api.SendAsync(HttpMethod.Post, "api/Auth/reset-password", new { token = model.Token, newPassword = model.NewPassword });
        if (!response.IsSuccessStatusCode) { ModelState.AddModelError("", await Api.ErrorAsync(response)); return View(model); }
        TempData["Success"] = "Your password has been reset. Please log in with your new password.";
        return RedirectToAction(nameof(Login));
    }
    [HttpGet] public IActionResult Register() => View(new RegisterViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        using var response = await Api.SendAsync(HttpMethod.Post, "api/Auth/register", model);
        if (!response.IsSuccessStatusCode) { ModelState.AddModelError("", await Api.ErrorAsync(response)); return View(model); }
        TempData["Success"] = "Your account has been created. Please log in.";
        return RedirectToAction(nameof(Login));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        using var response = await Api.SendAsync(HttpMethod.Post, "api/Auth/logout");
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}

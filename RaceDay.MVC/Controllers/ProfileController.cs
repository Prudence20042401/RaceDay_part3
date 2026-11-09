using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class ProfileController : BaseController
{
    public ProfileController(RaceDayApiClient api) : base(api) { }
    public async Task<IActionResult> Index()
    {
        if (!SignedIn) return RedirectToAction("Login", "Account");
        using var response = await Api.SendAsync(HttpMethod.Get, "api/Profile");
        if (!response.IsSuccessStatusCode) { TempData["Error"] = await Api.ErrorAsync(response); return RedirectToAction("Index", "Home"); }
        return View(await Api.ReadAsync<ProfileDto>(response));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileEditViewModel model)
    {
        if (!SignedIn) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index));
        using var response = await Api.SendAsync(HttpMethod.Put, "api/Profile", model);
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Profile updated." : await Api.ErrorAsync(response);
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile file)
    {
        if (Role != "Participant") return RedirectToAction("Index");
        using var response = await Api.SendAsync(HttpMethod.Post, "api/storage/profile-picture", file: file);
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Profile picture uploaded." : await Api.ErrorAsync(response);
        return RedirectToAction(nameof(Index));
    }
}

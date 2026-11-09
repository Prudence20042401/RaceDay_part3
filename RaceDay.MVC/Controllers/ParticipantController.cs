using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class ParticipantController : BaseController
{
    public ParticipantController(RaceDayApiClient api) : base(api) { }
    public async Task<IActionResult> MyEvents()
    {
        if (Role != "Participant") return RedirectToAction("Login", "Account");
        using var response = await Api.SendAsync(HttpMethod.Get, "api/Enrolments/my");
        if (!response.IsSuccessStatusCode) { TempData["Error"] = await Api.ErrorAsync(response); return View(new List<EnrolmentDto>()); }
        return View(await Api.ReadAsync<List<EnrolmentDto>>(response) ?? new());
    }
    public async Task<IActionResult> MyResults()
    {
        if (Role != "Participant") return RedirectToAction("Login", "Account");
        using var response = await Api.SendAsync(HttpMethod.Get, "api/Results/my");
        if (!response.IsSuccessStatusCode) { TempData["Error"] = await Api.ErrorAsync(response); return View(new List<ResultDto>()); }
        return View(await Api.ReadAsync<List<ResultDto>>(response) ?? new());
    }
}

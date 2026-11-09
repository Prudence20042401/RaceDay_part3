using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class OrganiserController : BaseController
{
    public OrganiserController(RaceDayApiClient api) : base(api) { }
    public async Task<IActionResult> Dashboard()
    {
        if (Role != "Organiser") return RedirectToAction("Login", "Account");
        using var response = await Api.SendAsync(HttpMethod.Get, "api/Events/mine");
        if (!response.IsSuccessStatusCode) { TempData["Error"] = await Api.ErrorAsync(response); return View(new List<EventDto>()); }
        return View(await Api.ReadAsync<List<EventDto>>(response) ?? new());
    }
    public async Task<IActionResult> Enrolments(int eventId)
    {
        if (Role != "Organiser") return RedirectToAction("Login", "Account");
        using var response = await Api.SendAsync(HttpMethod.Get, $"api/Enrolments/event/{eventId}");
        ViewBag.EventId = eventId;
        if (!response.IsSuccessStatusCode) { TempData["Error"] = await Api.ErrorAsync(response); return View(new List<EnrolmentDto>()); }
        return View(await Api.ReadAsync<List<EnrolmentDto>>(response) ?? new());
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, int eventId, string status)
    {
        if (Role != "Organiser") return RedirectToAction("Login", "Account");
        using var response = await Api.SendAsync(HttpMethod.Put, $"api/Enrolments/{id}/status", new { Status = status });
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Enrolment status updated." : await Api.ErrorAsync(response);
        return RedirectToAction(nameof(Enrolments), new { eventId });
    }
    [HttpGet]
    public async Task<IActionResult> AddCategory(int eventId)
    {
        if (Role != "Organiser") return RedirectToAction("Login", "Account");
        using var eventResponse = await Api.SendAsync(HttpMethod.Get, $"api/Events/{eventId}");
        ViewBag.EventName = (await Api.ReadAsync<EventDto>(eventResponse))?.Name ?? "this event";
        using var categoryResponse = await Api.SendAsync(HttpMethod.Get, $"api/Categories/event/{eventId}");
        ViewBag.Categories = await Api.ReadAsync<List<CategoryDto>>(categoryResponse) ?? new List<CategoryDto>();
        return View(new CategoryDto { EventID = eventId });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryDto model)
    {
        if (Role != "Organiser") return RedirectToAction("Login", "Account");
        using var response = await Api.SendAsync(HttpMethod.Post, "api/Categories", model);
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Category added." : await Api.ErrorAsync(response);
        return RedirectToAction(nameof(AddCategory), new { eventId = model.EventID });
    }
    [HttpGet] public async Task<IActionResult> Results(int eventId)
    {
        if (Role != "Organiser") return RedirectToAction("Login", "Account");
        using var response = await Api.SendAsync(HttpMethod.Get, $"api/Enrolments/event/{eventId}");
        ViewBag.EventId = eventId;
        return View(await Api.ReadAsync<List<EnrolmentDto>>(response) ?? new());
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordResult(ResultEntryViewModel model, int eventId)
    {
        if (Role != "Organiser") return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) { TempData["Error"] = "Enter a valid finishing time and position."; return RedirectToAction(nameof(Results), new { eventId }); }
        using var response = await Api.SendAsync(HttpMethod.Post, "api/Results", model);
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Result recorded." : await Api.ErrorAsync(response);
        return RedirectToAction(nameof(Results), new { eventId });
    }
}

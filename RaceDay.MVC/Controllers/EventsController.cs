using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class EventsController : BaseController
{
    public EventsController(RaceDayApiClient api) : base(api) { }
    public async Task<IActionResult> Index(string? search, string? type)
    {
        using var response = await Api.SendAsync(HttpMethod.Get, "api/Events");
        ViewBag.Error = response.IsSuccessStatusCode ? null : await Api.ErrorAsync(response);
        var events = await Api.ReadAsync<List<EventDto>>(response) ?? new();
        if (!string.IsNullOrWhiteSpace(search)) events = events.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || e.Location.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(type) && type != "All") events = events.Where(e => e.EventType.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        ViewBag.Search = search; ViewBag.Type = type;
        return View(events);
    }
    public async Task<IActionResult> Details(int id)
    {
        using var eventResponse = await Api.SendAsync(HttpMethod.Get, $"api/Events/{id}");
        if (!eventResponse.IsSuccessStatusCode) return NotFound();
        var model = await Api.ReadAsync<EventDto>(eventResponse);
        using var categoryResponse = await Api.SendAsync(HttpMethod.Get, $"api/Categories/event/{id}");
        ViewBag.Categories = await Api.ReadAsync<List<CategoryDto>>(categoryResponse) ?? new();
        return View(model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enter(EnrolViewModel model)
    {
        if (!SignedIn || Role != "Participant") return RedirectToAction("Login", "Account");
        using var response = await Api.SendAsync(HttpMethod.Post, "api/Enrolments", model);
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Your event entry has been recorded." : await Api.ErrorAsync(response);
        return RedirectToAction(nameof(Details), new { id = model.EventID });
    }
    [HttpGet] public IActionResult Create()
    {
        if (Role != "Organiser") return RedirectToAction("Login", "Account");
        return View(new EventDto { EventDate = DateTime.Today.AddDays(14) });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventDto model, IFormFile? banner)
    {
        if (Role != "Organiser") return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return View(model);
        using var response = await Api.SendAsync(HttpMethod.Post, "api/Events", model);
        if (!response.IsSuccessStatusCode) { ModelState.AddModelError("", await Api.ErrorAsync(response)); return View(model); }
        var created = await Api.ReadAsync<EventDto>(response);
        var bannerFailed = false;
        if (created != null && banner != null)
        {
            using var upload = await Api.SendAsync(HttpMethod.Post, $"api/storage/events/{created.EventID}/banner", file: banner);
            bannerFailed = !upload.IsSuccessStatusCode;
        }
        TempData[bannerFailed ? "Error" : "Success"] = bannerFailed ? "Event created, but the banner could not be uploaded. Check Azure Blob Storage settings." : "Event created successfully.";
        return RedirectToAction("Dashboard", "Organiser");
    }
    [HttpGet] public async Task<IActionResult> Edit(int id)
    {
        if (Role != "Organiser") return RedirectToAction("Login", "Account");
        using var response = await Api.SendAsync(HttpMethod.Get, $"api/Events/{id}");
        if (!response.IsSuccessStatusCode) return NotFound();
        return View(await Api.ReadAsync<EventDto>(response));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventDto model, IFormFile? banner)
    {
        if (Role != "Organiser") return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return View(model);
        using var response = await Api.SendAsync(HttpMethod.Put, $"api/Events/{id}", model);
        if (!response.IsSuccessStatusCode) { ModelState.AddModelError("", await Api.ErrorAsync(response)); return View(model); }
        var bannerFailed = false;
        if (banner != null)
        {
            using var upload = await Api.SendAsync(HttpMethod.Post, $"api/storage/events/{id}/banner", file: banner);
            bannerFailed = !upload.IsSuccessStatusCode;
        }
        TempData[bannerFailed ? "Error" : "Success"] = bannerFailed ? "Event updated, but the banner could not be uploaded. Check Azure Blob Storage settings." : "Event updated.";
        return RedirectToAction("Dashboard", "Organiser");
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (Role != "Organiser") return RedirectToAction("Login", "Account");
        using var response = await Api.SendAsync(HttpMethod.Delete, $"api/Events/{id}");
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Event deleted." : await Api.ErrorAsync(response);
        return RedirectToAction("Dashboard", "Organiser");
    }
}

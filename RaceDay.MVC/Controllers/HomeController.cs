using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class HomeController : BaseController
{
    public HomeController(RaceDayApiClient api) : base(api) { }
    public async Task<IActionResult> Index(string? search, string? type)
    {
        using var response = await Api.SendAsync(HttpMethod.Get, "api/Events");
        var events = (await Api.ReadAsync<List<EventDto>>(response) ?? new()).Where(e => e.EventDate.HasValue && e.EventDate.Value.Date >= DateTime.Today).ToList();
        if (!string.IsNullOrWhiteSpace(search)) events = events.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || e.Location.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(type) && type != "All") events = events.Where(e => e.EventType.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        ViewBag.Search = search; ViewBag.Type = type;
        return View(events);
    }
    public IActionResult Error() => View();
}

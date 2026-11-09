using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public abstract class BaseController : Controller
{
    protected readonly RaceDayApiClient Api;
    protected BaseController(RaceDayApiClient api) => Api = api;
    protected bool SignedIn => HttpContext.Session.GetInt32("UserID") != null;
    protected string Role => HttpContext.Session.GetString("Role") ?? "";
}

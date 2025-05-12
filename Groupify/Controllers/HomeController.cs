using System.Diagnostics;
using Groupify.Data;
using Microsoft.AspNetCore.Mvc;
using Groupify.Models;

namespace Groupify.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly InsightService _insight;

    public HomeController(ILogger<HomeController> logger, InsightService insight)
    {
        _logger = logger;
        _insight = insight;
    }

    public async Task<IActionResult> Index()
    {
        return View();
    }

    [HttpGet("/privacy")]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
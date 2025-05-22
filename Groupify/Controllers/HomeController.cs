using System.Diagnostics;
using Groupify.Data;
using Groupify.Data.Services;
using Groupify.Data.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Groupify.Models;

namespace Groupify.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IInsightService _insight;

    public HomeController(ILogger<HomeController> logger, IInsightService insight)
    {
        _logger = logger;
        _insight = insight;
    }

    public IActionResult Index()
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
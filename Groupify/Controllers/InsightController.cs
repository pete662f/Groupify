using System.Security.Claims;
using Groupify.Data;
using Groupify.Models.Domain;
using Groupify.Models.Identity;
using Groupify.ViewModels.Insight;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Groupify.Controllers;

public class InsightController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly InsightService _insight;
    private readonly UserManager<ApplicationUser> _userManager;

    public InsightController(ILogger<HomeController> logger, InsightService insight, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _insight = insight;
        _userManager = userManager;
    }
    
    [HttpGet("/profile")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        
        // Check if the user has an insight profile
        var hasInsightProfile = await _insight.HasInsightProfileAsync(user.Id);
        if (!hasInsightProfile)
            return RedirectToAction("CreateProfile");
        
        var insight = await _insight.GetInsightByUserIdAsync(user.Id);
        
        return View(insight);
    }
    
    [HttpGet("/profile/show/{id}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        
        // TODO: Check user is in a room owned by the teacher before showing the profile
        
        // Check if the user has an insight profile
        var hasInsightProfile = await _insight.HasInsightProfileAsync(id);
        if (!hasInsightProfile)
            return RedirectToAction("CreateProfile");
        
        var insight = await _insight.GetInsightByUserIdAsync(id);
        
        return View("Profile", insight);
    }
    
    [HttpGet("/profile/create")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> CreateProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();
        
        // Check if the user already has an insight profile
        var hasInsightProfile = await _insight.HasInsightProfileAsync(userId);
        if (hasInsightProfile)
            return RedirectToAction("Profile");
        
        return View();
    }

    [HttpPost("/profile/create")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> CreateProfile(CreateInsightProfileViewModel vm)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();
        
        // Check if the user already has an insight profile
        var hasInsightProfile = await _insight.HasInsightProfileAsync(userId);
        if (hasInsightProfile)
            return RedirectToAction("Profile");
        
        if (!ModelState.IsValid)
            return View("CreateProfile", vm);
        
        try
        {
            Insight insight = new Insight
            {
                Red = vm.Red,
                Blue = vm.Blue,
                Green = vm.Green,
                Yellow = vm.Yellow,
                WheelPosition = vm.WheelPosition
            };
            
            await _insight.CreateInsightProfileAsync(userId, insight);
            return RedirectToAction("Profile");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("CreateProfile", vm);
        } 
    }
    
    [HttpGet("/profile/update")]
    [Authorize(Roles = "Student")]
    public IActionResult UpdateProfile()
    {
        return View();
    }
    
    [HttpPost("/profile/update")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> UpdateProfile(CreateInsightProfileViewModel vm)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();
        
        if (!ModelState.IsValid)
            return View("UpdateProfile", vm);
        
        try
        {
            Insight insight = new Insight
            {
                Red = vm.Red,
                Blue = vm.Blue,
                Green = vm.Green,
                Yellow = vm.Yellow,
                WheelPosition = vm.WheelPosition
            };
            
            await _insight.UpdateInsightAsync(userId, insight);
            return RedirectToAction("Profile");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("UpdateProfile", vm);
        } 
    }
}
using Groupify.Data;
using Groupify.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Groupify.Controllers;

public class GroupController : Controller
{
    private readonly GroupService _groupService;
    private readonly UserManager<ApplicationUser> _userManager;
    
    public GroupController(GroupService groupService, UserManager<ApplicationUser> userManager)
    {
        _groupService = groupService;
        _userManager = userManager;
    }
    
    // TODO: This function should be moved to another controller or updated to use a different method
    [HttpPost]
    public async Task<IActionResult> CreateGroops(Guid roomId, int groupSize)
    {
        await _groupService.CreateGroupsAsync(roomId, groupSize);
        
        return RedirectToAction("Index");
    }
    
    // GET
    public IActionResult Index()
    {
        return View();
    }
}
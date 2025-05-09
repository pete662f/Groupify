using Groupify.Data;
using Groupify.Models.Identity;
using Groupify.ViewModels.Group;
using Groupify.ViewModels.Room;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;

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
    
    [HttpGet("/groups")]
    [Authorize(Roles = "Teacher, Student")]
    public IActionResult Index()
    {
        return View();
    }
    
    [HttpGet("/group")]
    public IActionResult RedirectToGroups()
    {
        return Redirect("/groups");
    }
    
    [HttpGet("/group/{id}")]
    [Authorize(Roles = "Teacher, Student")]
    public async Task<IActionResult> Details(Guid id)
    {
        var group = await _groupService.GetGroupByIdAsync(id);
        if (group == null)
            return NotFound();
        
        var vm = new DetailsGroupViewModel
        {
            Group = group,
            Users = await _userManager.Users.ToListAsync()
        };
        
        return View(vm);
    }
}
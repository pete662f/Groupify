using Groupify.Data;
using Groupify.Models.Identity;
using Groupify.ViewModels.Group;
using Groupify.ViewModels.Room;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    
    // GET
    public IActionResult Index()
    {
        return View();
    }
}
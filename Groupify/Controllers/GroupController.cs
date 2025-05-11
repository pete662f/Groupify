using System.Numerics;
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
    private readonly RoomService _roomService;
    private readonly UserManager<ApplicationUser> _userManager;
    
    public GroupController(GroupService groupService, RoomService roomService, UserManager<ApplicationUser> userManager)
    {
        _groupService = groupService;
        _roomService = roomService;
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
        
        // TODO: Check if the user is part of the group
        
        var vm = new DetailsGroupViewModel
        {
            Group = group,
            Users = await _userManager.Users.ToListAsync(),
            // TODO: Replace with IEnergies interface or something to access the 4 colors by name
            GroupInsight = await _groupService.GroupInsightAsync(group.Id)
        };
        
        return View(vm);
    }
    
    [HttpPost("/group/create")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> CreateGroups(CompositeRoomViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { success = false, message = "Unauthorized" });

        var room = await _roomService.GetRoomByIdAsync(vm.CreateGroup.RoomId);
        if (room == null)
            return Json(new { success = false, message = "Room not found" });

        bool isOwner = room.OwnerId == user.Id;
        if (!isOwner)
            return Json(new { success = false, message = "Forbidden" });

        if (!ModelState.IsValid)
        {
            var error = ModelState.Values
                .SelectMany(v => v?.Errors ?? [])
                .FirstOrDefault()?.ErrorMessage ?? "Invalid input";
            return Json(new { success = false, message = error });
        }

        try
        {
            await _groupService.CreateGroupsAsync(vm.CreateGroup.RoomId, vm.CreateGroup.GroupSize);
            return Json(new { success = true });
        }
        catch (Exception e)
        {
            return Json(new { success = false, message = e.Message });
        }
    }
}
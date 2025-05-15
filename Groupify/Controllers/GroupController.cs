using System.Numerics;
using Groupify.Data;
using Groupify.Models.Domain;
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
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized(); // User not authenticated
        
        IEnumerable<Group> groups = await _groupService.GetGroupsByUserIdAsync(user.Id);
        
        GroupsViewModel groupsViewModel = new GroupsViewModel
        {
            Groups = groups
        };
        
        return View(groupsViewModel);
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
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        
        var group = await _groupService.GetGroupByIdAsync(id);
        if (group == null)
            return NotFound();
        
        var room = await _roomService.GetRoomByIdAsync(group.RoomId);
        if (room == null)
            return NotFound();
        
        // Check if the user is part of the room
        bool isInRoom = room.Users.Any(u => u.Id == user.Id);
        bool isOwner = room.OwnerId == user.Id;
        bool isAdmin = User.IsInRole("Admin");
        if (!isInRoom && !isOwner && !isAdmin)
            return Forbid();
        
        var vm = new DetailsGroupViewModel
        {
            Group = group,
            Users = await _userManager.Users.ToListAsync(),
            // TODO: Replace with IEnergies interface or something to access the 4 colors by name
            GroupInsight = await _groupService.GroupInsightAsync(group.Id)
        };
        
        return View(vm);
    }

    [HttpPost("/group/moveUser")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> MoveUserToGroup(Guid newGroupId, string userId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { success = false, message = "Unauthorized" });
        
        var group = await _groupService.GetGroupByIdAsync(newGroupId);
        if (group == null)
            return Json(new { success = false, message = "Group not found" });

        var room = await _roomService.GetRoomByIdAsync(group.RoomId);
        if (room == null)
            return Json(new { success = false, message = "Room not found" });

        bool isOwner = room.OwnerId == user.Id;
        bool isAdmin = User.IsInRole("Admin");
        if (!isOwner && !isAdmin)
            return Json(new { success = false, message = "Forbidden" });

        try
        { 
            await _groupService.MoveUserToGroupAsync(userId, newGroupId);
            return Json(new { success = true });
        }
        catch (Exception e)
        {
            return Json(new { success = false, message = e.Message });
        }
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
        bool isAdmin = User.IsInRole("Admin");
        if (!isOwner && !isAdmin)
            return Json(new { success = false, message = "Forbidden" });

        if (!ModelState.IsValid)
        {
            // Return the first error message
            var error = ModelState.Values
                .SelectMany(v => v?.Errors ?? [])
                .FirstOrDefault()?.ErrorMessage ?? "Invalid input";
            return Json(new { success = false, message = error });
        }

        try
        {
            // Remove all groups in the room before creating new ones
            await _groupService.RemoveAllGroupsByRoomIdAsync(vm.CreateGroup.RoomId);
            
            // Create new groups
            await _groupService.CreateGroupsAsync(vm.CreateGroup.RoomId, vm.CreateGroup.GroupSize);
            return Json(new { success = true });
        }
        catch (Exception e)
        {
            return Json(new { success = false, message = e.Message });
        }
    }
}
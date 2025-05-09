using Groupify.Data;
using Groupify.Models.Domain;
using Groupify.Models.Identity;
using Groupify.ViewModels.Group;
using Groupify.ViewModels.Room;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Groupify.Controllers;

public class RoomController : Controller
{
    private readonly RoomService _roomService;
    private readonly GroupService _groupService;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoomController(RoomService roomService, GroupService groupService, UserManager<ApplicationUser> userManager)
    {
        _roomService = roomService;
        _groupService = groupService;
        _userManager = userManager;
    }
    
    [HttpGet("/rooms")]
    [Authorize(Roles = "Teacher, Student")]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized(); // User not authenticated

        IEnumerable<Room> rooms;
        
        if (await _userManager.IsInRoleAsync(user, "Teacher"))
        {
            rooms = await _roomService.GetOwnedRoomsByUserIdAsync(user.Id);
        }
        else if (await _userManager.IsInRoleAsync(user, "Student"))
        {
            rooms = await _roomService.GetRoomsByUserIdAsync(user.Id);
        }
        else
        {
            rooms = []; // No rooms for other roles
        }

        return View(rooms);
    }
    
    [HttpGet("/room")]
    public IActionResult RedirectToRooms()
    {
        return Redirect("/rooms");
    }
    
    [HttpPost]
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
    
    [HttpGet("/room/show/{roomId}")]
    [Authorize(Roles = "Teacher, Student")]
    public async Task<IActionResult> Details(Guid roomId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized(); // User not authenticated
        
        // Check if the user is in the room
        Room? room = await _roomService.GetRoomByIdAsync(roomId);
        if (room == null)
            return NotFound();

        // Check if the user is the owner or a member of the room
        bool isOwner  = room.OwnerId == user.Id;
        bool isMember = room.Users.Any(u => u.Id == user.Id);
        if (!isOwner && !isMember)
            return Forbid();
        
        // Create the view model
        CompositeRoomViewModel vm = new CompositeRoomViewModel
        {
            RoomDetails = new DetailsRoomViewModel
            {
                Room = room,
                Groups = room.Groups
            }
        };
        
        if (isOwner)
            return View("DetailsTeacher", vm);
        
        return View("DetailsStudent", vm);
    }
    
    [HttpGet("/room/join/{roomId}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> JoinRoom(Guid roomId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized(); // User not authenticated
        
        if (user.Insight == null)
            return BadRequest("You need to create an insight before joining a room.");
        
        try
        {
            // Check if the user is already in the room
            var room = await _roomService.GetRoomByIdAsync(roomId);
            if (room.Users.Any(u => u.Id == user.Id))
            {
                TempData["InfoMessage"] = "You’re already in that room.";
            }
            else
            {
                await _roomService.AddUserToRoomAsync(user.Id, roomId);
                TempData["SuccessMessage"] = "You’ve successfully joined the room!";
            }
            
            return RedirectToAction(nameof(Details), new { roomId });
        }
        catch (InvalidOperationException e)
        {
            return NotFound(e.Message);
        }
    }

    // Only for teachers
    [Authorize(Roles = "Teacher")]
    public IActionResult Create()
    {
        return View();
    }

    
    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> CreateRoom(CreateRoomViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized(); // User not authenticated
        
        if (!ModelState.IsValid)
            return View("Create", vm);
        
        try
        {
            Guid roomId = await _roomService.CreateRoomAsync(vm.Name, user.Id);
            return RedirectToAction(nameof(Details), new { roomId });
        }
        catch (InvalidOperationException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> RemoveRoom(Guid roomId)
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user == null)
            return Unauthorized(); // User not authenticated
        
        try
        {
            // Check if the user is the owner of the room
            var room = await _roomService.GetRoomByIdAsync(roomId);
            
            if (room.OwnerId != user.Id)
            {
                return Forbid(); // Only owner can delete
            }
            
            await _roomService.RemoveRoomAsync(roomId, user.Id);
            return RedirectToAction("Index"); // Redirect to the index
        }
        catch (InvalidOperationException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> ChangeRoomName(Guid roomId, string newName)
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user == null)
            return Unauthorized(); // User not authenticated
        
        try
        {
            // Check if the user is the owner of the room
            var room = await _roomService.GetRoomByIdAsync(roomId);
            
            if (room.OwnerId != user.Id)
            {
                return Forbid();
            }
            
            await _roomService.ChangeRoomNameAsync(roomId, newName);
            return RedirectToAction("Index"); // Redirect to the index
        }
        catch (InvalidOperationException e)
        {
            return NotFound(e.Message);
        }
    }
    
    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> AddUser(string userId, Guid roomId)
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user == null)
            return Unauthorized(); // User not authenticated
        
        try
        {
            // Check if the user is the owner of the room
            var room = await _roomService.GetRoomByIdAsync(roomId);
            
            if (room.OwnerId != user.Id)
            {
                return Forbid();
            }
            
            await _roomService.AddUserToRoomAsync(userId, roomId);
            return RedirectToAction("Index"); // Redirect to the index
        }
        catch (InvalidOperationException e)
        {
            return NotFound(e.Message);
        }
    }
    
    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> RemoveUser(string userId, Guid roomId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { success = false, message = "Unauthorized" });

        try
        {
            var room = await _roomService.GetRoomByIdAsync(roomId);
            if (room.OwnerId != user.Id)
                return Json(new { success = false, message = "Forbidden" });

            await _roomService.RemoveUserFromRoomAsync(userId, roomId);
            return Json(new { success = true });
        }
        catch (InvalidOperationException e)
        {
            return Json(new { success = false, message = e.Message });
        }
    }
}
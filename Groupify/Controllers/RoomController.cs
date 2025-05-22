using Groupify.Data;
using Groupify.Data.Services;
using Groupify.Data.Services.Interfaces;
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
    private readonly IRoomService _roomService;
    private readonly IGroupService _groupService;
    private readonly IInsightService _insightService;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoomController(IRoomService roomService, IGroupService groupService, IInsightService insightService, UserManager<ApplicationUser> userManager)
    {
        _roomService = roomService;
        _groupService = groupService;
        _insightService = insightService;
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
        
        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            rooms = await _roomService.GetAllRoomsAsync();
        }
        else if (await _userManager.IsInRoleAsync(user, "Teacher"))
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
        bool isAdmin = User.IsInRole("Admin");
        if (!isOwner && !isMember && !isAdmin)
            return Forbid();
        
        // Create the view model
        CompositeRoomViewModel vm = new CompositeRoomViewModel
        {
            UserGroupId = await _groupService.GetGroupByUserIdAndRoomIdAsync(user.Id, roomId),
            RoomDetails = new DetailsRoomViewModel
            {
                Room = room,
                InviteLink = Url.Action("JoinRoom", "Room", new { roomId }, Request.Scheme)!,
                Groups = room.Groups,
            },
        };
        
        if (isOwner || isAdmin)
            return View("DetailsTeacher", vm);
        
        vm.SingleMatchs = await _roomService.GetSingleMatchesAsync(roomId, user.Id);
        return View("DetailsStudent", vm);
    }
    
    [HttpGet("/room/join/{roomId}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> JoinRoom(Guid roomId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized(); // User not authenticated
        
        if (!await _insightService.HasInsightProfileAsync(user.Id))
        {
            TempData["WarningMessage"] = "Please complete your insight profile before joining a room.";
            return RedirectToAction("CreateProfile", "Insight");
        }
        
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
    public async Task<IActionResult> UpdateName(ChangeRoomNameViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user == null)
            return Json(new {sucess = false, message = "Unauthorized"});
        
        try
        {
            // Check if the user is the owner of the room
            var room = await _roomService.GetRoomByIdAsync(vm.RoomId);
            
            if (room.OwnerId != user.Id && !User.IsInRole("Admin"))
                return Json(new {sucess = false, message = "Forbid"});
            
            Console.WriteLine("Name: " + vm.NewName);
            await _roomService.ChangeRoomNameAsync(vm.RoomId, vm.NewName);
            return Json(new { success = true });
        }
        catch (InvalidOperationException e)
        {
            return Json(new { success = false, message = e.Message });
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
            
            if (room.OwnerId != user.Id && !User.IsInRole("Admin"))
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
            if (room.OwnerId != user.Id && !User.IsInRole("Admin"))
                return Json(new { success = false, message = "Forbidden" });

            await _roomService.RemoveUserFromRoomAsync(userId, roomId);
            return Json(new { success = true });
        }
        catch (InvalidOperationException e)
        {
            return Json(new { success = false, message = e.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Delete(Guid roomId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { success = false, message = "Unauthorized" });

        try
        {
            var room = await _roomService.GetRoomByIdAsync(roomId);
            if (room.OwnerId != user.Id && !User.IsInRole("Admin"))
                return Json(new { success = false, message = "Forbidden" });

            await _roomService.RemoveRoomAsync(User, roomId, user.Id);
            return Json(new { success = true });
        }
        catch (InvalidOperationException e)
        {
            return Json(new { success = false, message = e.Message });
        }
    }
}
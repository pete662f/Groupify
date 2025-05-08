using Groupify.Data;
using Groupify.Models.Domain;
using Groupify.Models.Identity;
using Groupify.ViewModels.Room;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Groupify.Controllers;

public class RoomController : Controller
{
    private readonly RoomService _roomService;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoomController(RoomService roomService, UserManager<ApplicationUser> userManager)
    {
        _roomService = roomService;
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
        
        DetailsRoomViewModel vm = new DetailsRoomViewModel
        {
            Room = room,
            Groups = room.Groups
        };

        // ReSharper disable HeuristicUnreachableCode
        if (isOwner)
            return View("DetailsTeacher", vm);
        else
            return View("DetailsStudent", vm);
    }
    
    [HttpGet("/room/join/{roomId}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> JoinRoom(Guid roomId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized(); // User not authenticated
        
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
    public async Task<IActionResult> AddUserToRoom(string userId, Guid roomId)
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
    public async Task<IActionResult> RemoveUserFromRoom(string userId, Guid roomId)
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
            
            await _roomService.RemoveUserFromRoomAsync(userId, roomId);
            return RedirectToAction("Index"); // Redirect to the index
        }
        catch (InvalidOperationException e)
        {
            return NotFound(e.Message);
        }
    }
}
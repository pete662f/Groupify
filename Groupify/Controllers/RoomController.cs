using Groupify.Data;
using Groupify.Models.Domain;
using Groupify.Models.Identity;
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
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

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

        return View("Index", rooms);
    }
    
    [HttpGet("/room")]
    public IActionResult RedirectToRooms()
    {
        return Redirect("/rooms");
    }

    
    [HttpPost]
    public async Task<IActionResult> CreateRoom(string roomName, bool addSelf)
    {
        var user = await _userManager.GetUserAsync(User);
        
        if (user == null)
            return Unauthorized(); // User not authenticated
        
        try
        {
            await _roomService.CreateRoomAsync(roomName, user.Id, addSelf);
            return RedirectToAction("Index"); // Redirect to the index
        }
        catch (InvalidOperationException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
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
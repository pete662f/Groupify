// RoomControllerTests.cs
using System.Security.Claims;
using Groupify.Controllers;
using Groupify.Data.Services.Interfaces;
using Groupify.Models.Domain;
using Groupify.Models.Identity;
using Groupify.Tests.Helpers;
using Groupify.ViewModels.Room;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using Groupify.Models.DTO;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Groupify.Tests.Controllers;
public class RoomControllerTests
{
    private readonly Mock<IRoomService> _mockRoomSvc = new();
    private readonly Mock<IGroupService> _mockGroupSvc = new();
    private readonly Mock<IInsightService> _mockInsightSvc = new();
    private readonly Mock<UserManager<ApplicationUser>> _mockUserMgr;
    private readonly RoomController _controller;

    public RoomControllerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserMgr = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!
        );

        _controller = new RoomController(
            _mockRoomSvc.Object,
            _mockGroupSvc.Object,
            _mockInsightSvc.Object,
            _mockUserMgr.Object
        );
        
        // initialize HttpContext and TempData
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext {
            HttpContext = httpContext
        };
        _controller.TempData = new TempDataDictionary(
            httpContext,
            Mock.Of<ITempDataProvider>()
        );
    }

    // Stub the URL helper to return a fixed URL for the invite link
    private void StubUrl()
    {
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("inviteLink");
        _controller.Url = url.Object;
        _controller.ControllerContext.HttpContext.Request.Scheme = "https";
    }

    // INDEX

    [Fact]
    public async Task Index_UserNull_ReturnsUnauthorized()
    {
        _controller.SetUser("u1", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync((ApplicationUser)null!);

        var r = await _controller.Index();
        Assert.IsType<UnauthorizedResult>(r);
    }

    [Fact]
    public async Task Index_Admin_ReturnsAllRooms()
    {
        _controller.SetUser("admin", "Admin");
        var rooms = new[] { new Room(), new Room() };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "admin" });
        _mockUserMgr.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Admin"))
                    .ReturnsAsync(true);
        _mockRoomSvc.Setup(s => s.GetAllRoomsAsync()).ReturnsAsync(rooms);

        var vr = Assert.IsType<ViewResult>(await _controller.Index());
        Assert.Same(rooms, vr.Model);
    }

    [Fact]
    public async Task Index_Teacher_ReturnsOwnedRooms()
    {
        _controller.SetUser("t1", "Teacher");
        var rooms = new[] { new Room() };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t1" });
        _mockUserMgr.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Admin"))
                    .ReturnsAsync(false);
        _mockUserMgr.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Teacher"))
                    .ReturnsAsync(true);
        _mockRoomSvc.Setup(s => s.GetOwnedRoomsByUserIdAsync("t1")).ReturnsAsync(rooms);

        var vr = Assert.IsType<ViewResult>(await _controller.Index());
        Assert.Same(rooms, vr.Model);
    }

    [Fact]
    public async Task Index_Student_ReturnsMemberRooms()
    {
        _controller.SetUser("s1", "Student");
        var rooms = new[] { new Room() };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "s1" });
        _mockUserMgr.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Admin"))
                    .ReturnsAsync(false);
        _mockUserMgr.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Teacher"))
                    .ReturnsAsync(false);
        _mockUserMgr.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Student"))
                    .ReturnsAsync(true);
        _mockRoomSvc.Setup(s => s.GetRoomsByUserIdAsync("s1")).ReturnsAsync(rooms);

        var vr = Assert.IsType<ViewResult>(await _controller.Index());
        Assert.Same(rooms, vr.Model);
    }

    [Fact]
    public async Task Index_OtherRole_ReturnsEmpty()
    {
        _controller.SetUser("x1", "Guest");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "x1" });
        _mockUserMgr.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                    .ReturnsAsync(false);

        var vr = Assert.IsType<ViewResult>(await _controller.Index());
        Assert.Empty((IEnumerable<Room>)vr.Model!);
    }

    // REDIRECT

    [Fact]
    public void RedirectToRooms_ReturnsExpected()
    {
        var r = _controller.RedirectToRooms();
        var rr = Assert.IsType<RedirectResult>(r);
        Assert.Equal("/rooms", rr.Url);
    }

    // DETAILS

    [Fact]
    public async Task Details_UserNull_ReturnsUnauthorized()
    {
        _controller.SetUser("u2", "Student");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync((ApplicationUser)null!);

        var r = await _controller.Details(Guid.NewGuid());
        Assert.IsType<UnauthorizedResult>(r);
    }

    [Fact]
    public async Task Details_RoomNull_ReturnsNotFound()
    {
        _controller.SetUser("u2", "Student");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "u2" });
        _mockRoomSvc.Setup(s => s.GetRoomByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync((Room)null!);

        var r = await _controller.Details(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(r);
    }

    [Fact]
    public async Task Details_NotOwnerOrMemberOrAdmin_Forbids()
    {
        var roomId = Guid.NewGuid();
        _controller.SetUser("u3", "Student");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "u3" });
        _mockRoomSvc.Setup(s => s.GetRoomByIdAsync(roomId))
                    .ReturnsAsync(new Room {
                        Id = roomId,
                        OwnerId = "other",
                        Users = new List<ApplicationUser>()
                    });

        var r = await _controller.Details(roomId);
        Assert.IsType<ForbidResult>(r);
    }

    [Fact]
    public async Task Details_Owner_ShowsDetailsTeacher()
    {
        var roomId = Guid.NewGuid();
        _controller.SetUser("owner", "Teacher");
        StubUrl();

        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "owner" });
        _mockRoomSvc.Setup(s => s.GetRoomByIdAsync(roomId))
                    .ReturnsAsync(new Room {
                        Id = roomId,
                        OwnerId = "owner",
                        Users = new List<ApplicationUser>(),
                        Groups = new List<Group> { new() }
                    });
        _mockGroupSvc.Setup(s => s.GetGroupByUserIdAndRoomIdAsync("owner", roomId))
                     .ReturnsAsync(Guid.Empty);

        var vr = Assert.IsType<ViewResult>(await _controller.Details(roomId));
        Assert.Equal("DetailsTeacher", vr.ViewName);
        var vm = Assert.IsType<CompositeRoomViewModel>(vr.Model);
        Assert.Equal(Guid.Empty, vm.UserGroupId);
        Assert.Equal("inviteLink", vm.RoomDetails.InviteLink);
    }

    [Fact]
    public async Task Details_Member_ShowsDetailsStudent()
    {
        var roomId = Guid.NewGuid();
        var user = new ApplicationUser { Id = "m1" };
        var matches = new List<UserMatchDto>
        {
            new UserMatchDto {
                User = user,
                MatchPercentage = 0.75f
            }
        };
        _controller.SetUser("m1", "Student");
        StubUrl();

        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(user);
        _mockRoomSvc.Setup(s => s.GetRoomByIdAsync(roomId))
                    .ReturnsAsync(new Room {
                        Id = roomId,
                        OwnerId = "other",
                        Users = new List<ApplicationUser>{ user },
                        Groups = new List<Group>()
                    });
        _mockGroupSvc.Setup(s => s.GetGroupByUserIdAndRoomIdAsync("m1", roomId))
                     .ReturnsAsync(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        _mockRoomSvc.Setup(s => s.GetSingleMatchesAsync(roomId, "m1", 10))
                     .ReturnsAsync(matches);

        var vr = Assert.IsType<ViewResult>(await _controller.Details(roomId));
        Assert.Equal("DetailsStudent", vr.ViewName);
        var vm = Assert.IsType<CompositeRoomViewModel>(vr.Model);
        Assert.Single(vm.SingleMatchs!);
    }

    [Fact]
    public async Task Details_Admin_ShowsDetailsTeacher()
    {
        var roomId = Guid.NewGuid();
        _controller.SetUser("a1", "Admin");
        StubUrl();

        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "a1" });
        _mockRoomSvc.Setup(s => s.GetRoomByIdAsync(roomId))
                    .ReturnsAsync(new Room {
                        Id = roomId,
                        OwnerId = "other",
                        Users = new List<ApplicationUser>(),
                        Groups = (ICollection<Group>)Enumerable.Empty<Group>()
                    });
        _mockGroupSvc.Setup(s => s.GetGroupByUserIdAndRoomIdAsync("a1", roomId))
                     .ReturnsAsync(Guid.Empty);

        var vr = Assert.IsType<ViewResult>(await _controller.Details(roomId));
        Assert.Equal("DetailsTeacher", vr.ViewName);
    }

    // JOINROOM

    [Fact]
    public async Task JoinRoom_UserNull_ReturnsUnauthorized()
    {
        _controller.SetUser("x1", "Student");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync((ApplicationUser)null!);

        var r = await _controller.JoinRoom(Guid.NewGuid());
        Assert.IsType<UnauthorizedResult>(r);
    }

    [Fact]
    public async Task JoinRoom_NoInsightProfile_RedirectsToCreateProfile()
    {
        var roomId = Guid.NewGuid();
        _controller.SetUser("s1", "Student");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "s1" });
        _mockInsightSvc.Setup(s => s.HasInsightProfileAsync("s1"))
                       .ReturnsAsync(false);

        var r = Assert.IsType<RedirectToActionResult>(await _controller.JoinRoom(roomId));
        Assert.Equal("CreateProfile", r.ActionName);
        Assert.Equal("Insight", r.ControllerName);
        Assert.Equal("Please complete your insight profile before joining a room.",
                     _controller.TempData["WarningMessage"]);
    }

    [Fact]
    public async Task JoinRoom_AlreadyInRoom_SetsInfoMessage()
    {
        var roomId = Guid.NewGuid();
        var user = new ApplicationUser { Id = "s2" };
        _controller.SetUser("s2", "Student");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(user);
        _mockInsightSvc.Setup(s => s.HasInsightProfileAsync("s2"))
                       .ReturnsAsync(true);
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(roomId))
                    .ReturnsAsync(new Room { Users = new List<ApplicationUser>{ user } });

        var r = Assert.IsType<RedirectToActionResult>(await _controller.JoinRoom(roomId));
        Assert.Equal("Details", r.ActionName);
        Assert.Equal("You’re already in that room.", _controller.TempData["InfoMessage"]);
    }

    [Fact]
    public async Task JoinRoom_Success_SetsSuccessMessage()
    {
        var roomId = Guid.NewGuid();
        var user = new ApplicationUser { Id = "s3" };
        _controller.SetUser("s3", "Student");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(user);
        _mockInsightSvc.Setup(s => s.HasInsightProfileAsync("s3"))
                       .ReturnsAsync(true);
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(roomId))
                    .ReturnsAsync(new Room { Users = new List<ApplicationUser>() });
        _mockRoomSvc.Setup(r => r.AddUserToRoomAsync("s3", roomId))
                    .Returns(Task.CompletedTask);

        var r = Assert.IsType<RedirectToActionResult>(await _controller.JoinRoom(roomId));
        Assert.Equal("Details", r.ActionName);
        Assert.Equal("You’ve successfully joined the room!", _controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task JoinRoom_Exception_ReturnsNotFoundWithMessage()
    {
        var roomId = Guid.NewGuid();
        _controller.SetUser("s4", "Student");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "s4" });
        _mockInsightSvc.Setup(s => s.HasInsightProfileAsync("s4"))
                       .ReturnsAsync(true);
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(roomId))
                    .ThrowsAsync(new InvalidOperationException("boom"));

        var nf = Assert.IsType<NotFoundObjectResult>(await _controller.JoinRoom(roomId));
        Assert.Equal("boom", nf.Value);
    }

    // CREATE

    [Fact]
    public void Create_Get_ReturnsView() => Assert.IsType<ViewResult>(_controller.Create());

    [Fact]
    public async Task CreateRoom_UserNull_ReturnsUnauthorized()
    {
        _controller.SetUser("x5", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync((ApplicationUser)null!);

        var vm = new CreateRoomViewModel { Name = "R" };
        var r = await _controller.CreateRoom(vm);
        Assert.IsType<UnauthorizedResult>(r);
    }

    [Fact]
    public async Task CreateRoom_InvalidModel_ReturnsCreateView()
    {
        _controller.SetUser("t5", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t5" });
        _controller.ModelState.AddModelError("X", "err");

        var vm = new CreateRoomViewModel { Name = "" };
        var vr = Assert.IsType<ViewResult>(await _controller.CreateRoom(vm));
        Assert.Equal("Create", vr.ViewName);
        Assert.Equal(vm, vr.Model);
    }

    [Fact]
    public async Task CreateRoom_Exception_ReturnsNotFoundWithMessage()
    {
        _controller.SetUser("t6", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t6" });
        _mockRoomSvc.Setup(r => r.CreateRoomAsync("R", "t6"))
                    .ThrowsAsync(new InvalidOperationException("fail"));

        var nf = Assert.IsType<NotFoundObjectResult>(
            await _controller.CreateRoom(new CreateRoomViewModel { Name = "R" }));
        Assert.Equal("fail", nf.Value);
    }

    [Fact]
    public async Task CreateRoom_Success_RedirectsToDetails()
    {
        _controller.SetUser("t7", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t7" });
        _mockRoomSvc.Setup(r => r.CreateRoomAsync("N", "t7"))
                    .ReturnsAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var red = Assert.IsType<RedirectToActionResult>(
            await _controller.CreateRoom(new CreateRoomViewModel { Name = "N" }));
        Assert.Equal("Details", red.ActionName);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"),
                     red.RouteValues!["roomId"]);
    }

    // UPDATE NAME

    [Fact]
    public async Task UpdateName_UserNull_ReturnsJsonUnauthorized()
    {
        _controller.SetUser("x6", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync((ApplicationUser)null!);

        var json = Assert.IsType<JsonResult>(
            await _controller.UpdateName(new ChangeRoomNameViewModel()));
        dynamic d = json.Value!;
        Assert.False((bool)d.sucess);
        Assert.Equal("Unauthorized", (string)d.message);
    }

    [Fact]
    public async Task UpdateName_NotOwnerOrAdmin_ReturnsJsonForbid()
    {
        _controller.SetUser("t8", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t8" });
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(new Room { OwnerId = "other" });

        var json = Assert.IsType<JsonResult>(
            await _controller.UpdateName(new ChangeRoomNameViewModel { RoomId = Guid.NewGuid() }));
        dynamic d = json.Value!;
        Assert.False((bool)d.sucess);
        Assert.Equal("Forbid", (string)d.message);
    }

    [Fact]
    public async Task UpdateName_Success_ReturnsJsonSuccess()
    {
        var vm = new ChangeRoomNameViewModel { RoomId = Guid.NewGuid(), NewName = "NN" };
        _controller.SetUser("t9", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t9" });
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(vm.RoomId))
                    .ReturnsAsync(new Room { OwnerId = "t9" });

        var json = Assert.IsType<JsonResult>(await _controller.UpdateName(vm));
        dynamic d = json.Value!;
        Assert.True((bool)d.success);
        _mockRoomSvc.Verify(r => r.ChangeRoomNameAsync(vm.RoomId, "NN"), Times.Once);
    }

    [Fact]
    public async Task UpdateName_Exception_ReturnsJsonErrorMessage()
    {
        var vm = new ChangeRoomNameViewModel { RoomId = Guid.NewGuid(), NewName = "XX" };
        _controller.SetUser("t10", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t10" });
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(vm.RoomId))
                    .ReturnsAsync(new Room { OwnerId = "t10" });
        _mockRoomSvc.Setup(r => r.ChangeRoomNameAsync(vm.RoomId, "XX"))
                    .ThrowsAsync(new InvalidOperationException("bad"));

        var json = Assert.IsType<JsonResult>(await _controller.UpdateName(vm));
        dynamic d = json.Value!;
        Assert.False((bool)d.success);
        Assert.Equal("bad", (string)d.message);
    }

    // ADDUSER

    [Fact]
    public async Task AddUser_UserNull_ReturnsUnauthorized()
    {
        _controller.SetUser("x7", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync((ApplicationUser)null!);

        var r = await _controller.AddUser("u", Guid.NewGuid());
        Assert.IsType<UnauthorizedResult>(r);
    }

    [Fact]
    public async Task AddUser_NotOwnerOrAdmin_Forbids()
    {
        _controller.SetUser("t11", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t11" });
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(new Room { OwnerId = "other" });

        Assert.IsType<ForbidResult>(
            await _controller.AddUser("u", Guid.NewGuid()));
    }

    [Fact]
    public async Task AddUser_Success_RedirectsIndex()
    {
        _controller.SetUser("t12", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t12" });
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(new Room { OwnerId = "t12" });
        _mockRoomSvc.Setup(r => r.AddUserToRoomAsync("u", It.IsAny<Guid>()))
                    .Returns(Task.CompletedTask);

        var red = Assert.IsType<RedirectToActionResult>(
            await _controller.AddUser("u", Guid.NewGuid()));
        Assert.Equal("Index", red.ActionName);
    }

    [Fact]
    public async Task AddUser_Exception_ReturnsNotFoundWithMessage()
    {
        _controller.SetUser("t13", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t13" });
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(new Room { OwnerId = "t13" });
        _mockRoomSvc.Setup(r => r.AddUserToRoomAsync("u", It.IsAny<Guid>()))
                    .ThrowsAsync(new InvalidOperationException("oops"));

        var nf = Assert.IsType<NotFoundObjectResult>(
            await _controller.AddUser("u", Guid.NewGuid()));
        Assert.Equal("oops", nf.Value);
    }

    // REMOVEUSER

    [Fact]
    public async Task RemoveUser_UserNull_ReturnsJsonUnauthorized()
    {
        _controller.SetUser("x8", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync((ApplicationUser)null!);

        var json = Assert.IsType<JsonResult>(
            await _controller.RemoveUser("u", Guid.NewGuid()));
        dynamic d = json.Value!;
        Assert.False((bool)d.success);
        Assert.Equal("Unauthorized", (string)d.message);
    }

    [Fact]
    public async Task RemoveUser_NotOwnerOrAdmin_ReturnsJsonForbidden()
    {
        _controller.SetUser("t14", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t14" });
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(new Room { OwnerId = "other" });

        var json = Assert.IsType<JsonResult>(
            await _controller.RemoveUser("u", Guid.NewGuid()));
        dynamic d = json.Value!;
        Assert.False((bool)d.success);
        Assert.Equal("Forbidden", (string)d.message);
    }

    [Fact]
    public async Task RemoveUser_Success_ReturnsJsonSuccess()
    {
        _controller.SetUser("t15", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t15" });
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(new Room { OwnerId = "t15" });
        _mockRoomSvc.Setup(r => r.RemoveUserFromRoomAsync("u", It.IsAny<Guid>()))
                    .Returns(Task.CompletedTask);

        var json = Assert.IsType<JsonResult>(
            await _controller.RemoveUser("u", Guid.NewGuid()));
        dynamic d = json.Value!;
        Assert.True((bool)d.success);
    }

    [Fact]
    public async Task RemoveUser_Exception_ReturnsJsonError()
    {
        _controller.SetUser("t16", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t16" });
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(new Room { OwnerId = "t16" });
        _mockRoomSvc.Setup(r => r.RemoveUserFromRoomAsync("u", It.IsAny<Guid>()))
                    .ThrowsAsync(new InvalidOperationException("err"));

        var json = Assert.IsType<JsonResult>(
            await _controller.RemoveUser("u", Guid.NewGuid()));
        dynamic d = json.Value!;
        Assert.False((bool)d.success);
        Assert.Equal("err", (string)d.message);
    }

    // DELETE

    [Fact]
    public async Task Delete_UserNull_ReturnsJsonUnauthorized()
    {
        _controller.SetUser("x9", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync((ApplicationUser)null!);

        var json = Assert.IsType<JsonResult>(
            await _controller.Delete(Guid.NewGuid()));
        dynamic d = json.Value!;
        Assert.False((bool)d.success);
        Assert.Equal("Unauthorized", (string)d.message);
    }

    [Fact]
    public async Task Delete_NotOwnerOrAdmin_ReturnsJsonForbidden()
    {
        _controller.SetUser("t17", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t17" });
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(new Room { OwnerId = "other" });

        var json = Assert.IsType<JsonResult>(
            await _controller.Delete(Guid.NewGuid()));
        dynamic d = json.Value!;
        Assert.False((bool)d.success);
        Assert.Equal("Forbidden", (string)d.message);
    }

    [Fact]
    public async Task Delete_Success_ReturnsJsonSuccess()
    {
        _controller.SetUser("t18", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t18" });
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(new Room { OwnerId = "t18" });
        _mockRoomSvc.Setup(r => r.RemoveRoomAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Guid>(), "t18"))
                    .Returns(Task.CompletedTask);

        var json = Assert.IsType<JsonResult>(
            await _controller.Delete(Guid.NewGuid()));
        dynamic d = json.Value!;
        Assert.True((bool)d.success);
    }

    [Fact]
    public async Task Delete_Exception_ReturnsJsonError()
    {
        _controller.SetUser("t19", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t19" });
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(new Room { OwnerId = "t19" });
        _mockRoomSvc.Setup(r => r.RemoveRoomAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Guid>(), "t19"))
                    .ThrowsAsync(new InvalidOperationException("oops"));

        var json = Assert.IsType<JsonResult>(
            await _controller.Delete(Guid.NewGuid()));
        dynamic d = json.Value!;
        Assert.False((bool)d.success);
        Assert.Equal("oops", (string)d.message);
    }
}

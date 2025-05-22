using System.Numerics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Groupify.Controllers;
using Groupify.Data.Services.Interfaces;
using Groupify.Models.Domain;
using Groupify.Models.Identity;
using Groupify.Tests.Helpers;
using Groupify.ViewModels.Group;
using Groupify.ViewModels.Room;
using Microsoft.AspNetCore.Identity;
using Groupify.Tests.Helpers;

namespace Groupify.Tests.Controllers;
public class GroupControllerTests
{
    private readonly Mock<IGroupService> _mockGroupSvc = new();
    private readonly Mock<IRoomService> _mockRoomSvc = new();
    private readonly Mock<UserManager<ApplicationUser>> _mockUserMgr;
    private readonly GroupController _controller;

    public GroupControllerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserMgr = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!
        );

        _controller = new GroupController(
            _mockGroupSvc.Object,
            _mockRoomSvc.Object,
            _mockUserMgr.Object
        );

        // default principal is a Teacher; individual tests can override
        _controller.SetUser("user-123", "Teacher");
    }

    // Index
    [Theory]
    [InlineData("Admin")]
    [InlineData("Teacher")]
    [InlineData("Student")]
    public async Task Index_UserNull_ReturnsUnauthorized(string role)
    {
        _controller.SetUser("user-123", role);
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync((ApplicationUser)null!);

        var result = await _controller.Index();
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Index_UserNotNull_ReturnsView()
    {
        var fakeUser = new ApplicationUser { Id = "user-123" };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(fakeUser);
        _mockGroupSvc.Setup(s => s.GetGroupsByUserIdAsync("user-123"))
                     .ReturnsAsync(new List<Group>());

        var result = await _controller.Index();
        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<GroupsViewModel>(view.Model);
    }

    // Redirect
    [Fact]
    public void RedirectToGroups_ReturnsRedirectToGroups()
    {
        var result = _controller.RedirectToGroups();
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/groups", redirect.Url);
    }

    // Details
    [Fact]
    public async Task Details_UserNull_ReturnsUnauthorized()
    {
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync((ApplicationUser)null!);

        var result = await _controller.Details(Guid.NewGuid());
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Details_GroupNull_ReturnsNotFound()
    {
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "user-123" });
        _mockGroupSvc.Setup(s => s.GetGroupByIdAsync(It.IsAny<Guid>()))
                     .ReturnsAsync((Group)null!);

        var result = await _controller.Details(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_RoomNull_ReturnsNotFound()
    {
        var fakeGroup = new Group { Id = Guid.NewGuid(), RoomId = Guid.NewGuid() };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "user-123" });
        _mockGroupSvc.Setup(s => s.GetGroupByIdAsync(fakeGroup.Id))
                     .ReturnsAsync(fakeGroup);
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(fakeGroup.RoomId))
                    .ReturnsAsync((Room)null!);

        var result = await _controller.Details(fakeGroup.Id);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_UserNotInRoomAndNotOwnerOrAdmin_ReturnsForbid()
    {
        var fakeGroup = new Group { Id = Guid.NewGuid(), RoomId = Guid.NewGuid() };
        var fakeRoom  = new Room { Id = fakeGroup.RoomId, OwnerId = "other", Users = new List<ApplicationUser>() };

        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "user-123" });
        _mockGroupSvc.Setup(s => s.GetGroupByIdAsync(fakeGroup.Id))
                     .ReturnsAsync(fakeGroup);
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(fakeGroup.RoomId))
                    .ReturnsAsync(fakeRoom);

        var result = await _controller.Details(fakeGroup.Id);
        Assert.IsType<ForbidResult>(result);
    }
    
    [Fact]
    public async Task Details_UserInRoom_ReturnsViewWithModel()
    {
        var fakeUser = new ApplicationUser { Id = "user-123" };
        var fakeGroup = new Group { Id = Guid.NewGuid(), RoomId = Guid.NewGuid() };
        var fakeRoom  = new Room
        {
            Id = fakeGroup.RoomId,
            OwnerId = "other",
            Users = new List<ApplicationUser> { fakeUser }
        };

        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(fakeUser);
        _mockGroupSvc.Setup(s => s.GetGroupByIdAsync(fakeGroup.Id))
                     .ReturnsAsync(fakeGroup);
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(fakeGroup.RoomId))
                    .ReturnsAsync(fakeRoom);
        _mockGroupSvc.Setup(s => s.GroupInsightAsync(fakeGroup.Id))
                     .ReturnsAsync(new Vector4());
        var usersAsync = new TestAsyncEnumerable<ApplicationUser>([fakeUser]);
        _mockUserMgr.Setup(m => m.Users).Returns(usersAsync);

        var result = await _controller.Details(fakeGroup.Id);
        var view = Assert.IsType<ViewResult>(result);
        var vm   = Assert.IsType<DetailsGroupViewModel>(view.Model);
        Assert.Equal(fakeGroup, vm.Group);
    }

    [Fact]
    public async Task Details_UserIsOwner_ReturnsViewWithModel()
    {
        var fakeUser = new ApplicationUser { Id = "user-123" };
        var fakeGroup = new Group { Id = Guid.NewGuid(), RoomId = Guid.NewGuid() };
        var fakeRoom  = new Room
        {
            Id = fakeGroup.RoomId,
            OwnerId = "user-123",
            Users = new List<ApplicationUser>()
        };

        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(fakeUser);
        _mockGroupSvc.Setup(s => s.GetGroupByIdAsync(fakeGroup.Id))
                     .ReturnsAsync(fakeGroup);
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(fakeGroup.RoomId))
                    .ReturnsAsync(fakeRoom);
        _mockGroupSvc.Setup(s => s.GroupInsightAsync(fakeGroup.Id))
                     .ReturnsAsync(new Vector4());
        var usersAsync = new TestAsyncEnumerable<ApplicationUser>([fakeUser]);
        _mockUserMgr.Setup(m => m.Users).Returns(usersAsync);

        var result = await _controller.Details(fakeGroup.Id);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Details_UserIsAdmin_ReturnsViewWithModel()
    {
        // set Admin role on principal
        _controller.SetUser("user-123", "Admin");

        var fakeUser = new ApplicationUser { Id = "user-123" };
        var fakeGroup = new Group { Id = Guid.NewGuid(), RoomId = Guid.NewGuid() };
        var fakeRoom  = new Room
        {
            Id = fakeGroup.RoomId,
            OwnerId = "other",
            Users = new List<ApplicationUser>()
        };

        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(fakeUser);
        _mockGroupSvc.Setup(s => s.GetGroupByIdAsync(fakeGroup.Id))
                     .ReturnsAsync(fakeGroup);
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(fakeGroup.RoomId))
                    .ReturnsAsync(fakeRoom);
        _mockGroupSvc.Setup(s => s.GroupInsightAsync(fakeGroup.Id))
                     .ReturnsAsync(new Vector4());
        var usersAsync = new TestAsyncEnumerable<ApplicationUser>([fakeUser]);
        _mockUserMgr.Setup(m => m.Users).Returns(usersAsync);

        var result = await _controller.Details(fakeGroup.Id);
        Assert.IsType<ViewResult>(result);
    }

    // MoveUserToGroup
    [Fact]
    public async Task MoveUserToGroup_UserNull_ReturnsJsonUnauthorized()
    {
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync((ApplicationUser)null!);

        var result = await _controller.MoveUserToGroup(Guid.NewGuid(), "user-123");
        var json = Assert.IsType<JsonResult>(result);
        dynamic data = json.Value!;
        Assert.False((bool)data.success);
        Assert.Equal("Unauthorized", (string)data.message);
    }

    [Fact]
    public async Task MoveUserToGroup_GroupNull_ReturnsJsonGroupNotFound()
    {
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "user-123" });
        _mockGroupSvc.Setup(s => s.GetGroupByIdAsync(It.IsAny<Guid>()))
                     .ReturnsAsync((Group)null!);

        var result = await _controller.MoveUserToGroup(Guid.NewGuid(), "user-123");
        var json = Assert.IsType<JsonResult>(result);
        dynamic data = json.Value!;
        Assert.False((bool)data.success);
        Assert.Equal("Group not found", (string)data.message);
    }

    [Fact]
    public async Task MoveUserToGroup_RoomNull_ReturnsJsonRoomNotFound()
    {
        var fg = new Group { Id = Guid.NewGuid(), RoomId = Guid.NewGuid() };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "user-123" });
        _mockGroupSvc.Setup(s => s.GetGroupByIdAsync(fg.Id))
                     .ReturnsAsync(fg);
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(fg.RoomId))
                    .ReturnsAsync((Room)null!);

        var result = await _controller.MoveUserToGroup(fg.Id, "user-123");
        var json = Assert.IsType<JsonResult>(result);
        dynamic data = json.Value!;
        Assert.False((bool)data.success);
        Assert.Equal("Room not found", (string)data.message);
    }

    [Fact]
    public async Task MoveUserToGroup_Forbidden_ReturnsJsonForbidden()
    {
        var fg = new Group { Id = Guid.NewGuid(), RoomId = Guid.NewGuid() };
        var fr = new Room  { Id = fg.RoomId, OwnerId = "other" };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "user-123" });
        _mockGroupSvc.Setup(s => s.GetGroupByIdAsync(fg.Id)).ReturnsAsync(fg);
        _mockRoomSvc .Setup(r => r.GetRoomByIdAsync(fg.RoomId)).ReturnsAsync(fr);

        var result = await _controller.MoveUserToGroup(fg.Id, "user-123");
        var json = Assert.IsType<JsonResult>(result);
        dynamic data = json.Value!;
        Assert.False((bool)data.success);
        Assert.Equal("Forbidden", (string)data.message);
    }

    [Fact]
    public async Task MoveUserToGroup_Success_ReturnsJsonSuccess()
    {
        var fg = new Group { Id = Guid.NewGuid(), RoomId = Guid.NewGuid() };
        var fr = new Room  { Id = fg.RoomId, OwnerId = "user-123" };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "user-123" });
        _mockGroupSvc.Setup(s => s.GetGroupByIdAsync(fg.Id)).ReturnsAsync(fg);
        _mockRoomSvc .Setup(r => r.GetRoomByIdAsync(fg.RoomId)).ReturnsAsync(fr);
        _mockGroupSvc.Setup(s => s.MoveUserToGroupAsync("user-123", fg.Id))
                     .Returns(Task.CompletedTask);

        var result = await _controller.MoveUserToGroup(fg.Id, "user-123");
        var json = Assert.IsType<JsonResult>(result);
        dynamic data = json.Value!;
        Assert.True((bool)data.success);
    }

    [Fact]
    public async Task MoveUserToGroup_ServiceThrows_ReturnsJsonError()
    {
        var fg = new Group { Id = Guid.NewGuid(), RoomId = Guid.NewGuid() };
        var fr = new Room  { Id = fg.RoomId, OwnerId = "user-123" };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "user-123" });
        _mockGroupSvc.Setup(s => s.GetGroupByIdAsync(fg.Id)).ReturnsAsync(fg);
        _mockRoomSvc .Setup(r => r.GetRoomByIdAsync(fg.RoomId)).ReturnsAsync(fr);
        _mockGroupSvc.Setup(s => s.MoveUserToGroupAsync("user-123", fg.Id))
                     .ThrowsAsync(new InvalidOperationException("fail"));

        var result = await _controller.MoveUserToGroup(fg.Id, "user-123");
        var json = Assert.IsType<JsonResult>(result);
        dynamic data = json.Value!;
        Assert.False((bool)data.success);
        Assert.Equal("fail", (string)data.message);
    }

    // CreateGroups
    [Fact]
    public async Task CreateGroups_Unauthorized_ReturnsJsonUnauthorized()
    {
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync((ApplicationUser)null!);

        var vm = new CompositeRoomViewModel { CreateGroup = new() { RoomId = Guid.NewGuid(), GroupSize = 2 }};
        var result = await _controller.CreateGroups(vm);
        var json = Assert.IsType<JsonResult>(result);
        dynamic data = json.Value!;
        Assert.False((bool)data.success);
        Assert.Equal("Unauthorized", (string)data.message);
    }

    [Fact]
    public async Task CreateGroups_RoomNotFound_ReturnsJsonRoomNotFound()
    {
        var fakeUser = new ApplicationUser { Id = "user-123" };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(fakeUser);
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync((Room)null!);

        var vm = new CompositeRoomViewModel { CreateGroup = new() { RoomId = Guid.NewGuid(), GroupSize = 2 }};
        var result = await _controller.CreateGroups(vm);
        var json = Assert.IsType<JsonResult>(result);
        dynamic data = json.Value!;
        Assert.False((bool)data.success);
        Assert.Equal("Room not found", (string)data.message);
    }

    [Fact]
    public async Task CreateGroups_Forbidden_ReturnsJsonForbidden()
    {
        var fakeUser = new ApplicationUser { Id = "user-123" };
        var fakeRoom = new Room { Id = Guid.NewGuid(), OwnerId = "other" };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(fakeUser);
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(fakeRoom.Id))
                    .ReturnsAsync(fakeRoom);

        var vm = new CompositeRoomViewModel { CreateGroup = new() { RoomId = fakeRoom.Id, GroupSize = 2 }};
        var result = await _controller.CreateGroups(vm);
        var json = Assert.IsType<JsonResult>(result);
        dynamic data = json.Value!;
        Assert.False((bool)data.success);
        Assert.Equal("Forbidden", (string)data.message);
    }

    [Fact]
    public async Task CreateGroups_InvalidModelState_ReturnsJsonFirstError()
    {
        var fakeUser = new ApplicationUser { Id = "user-123" };
        var fakeRoom = new Room { Id = Guid.NewGuid(), OwnerId = "user-123" };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(fakeUser);
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(fakeRoom.Id))
                    .ReturnsAsync(fakeRoom);

        _controller.ModelState.AddModelError("CreateGroup.GroupSize", "Size required");
        var vm = new CompositeRoomViewModel { CreateGroup = new() { RoomId = fakeRoom.Id }};

        var result = await _controller.CreateGroups(vm);
        var json = Assert.IsType<JsonResult>(result);
        dynamic data = json.Value!;
        Assert.False((bool)data.success);
        Assert.Equal("Size required", (string)data.message);
    }

    [Fact]
    public async Task CreateGroups_ExceptionInService_ReturnsJsonErrorMessage()
    {
        var fakeUser = new ApplicationUser { Id = "user-123" };
        var fakeRoom = new Room { Id = Guid.NewGuid(), OwnerId = "user-123" };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(fakeUser);
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(fakeRoom.Id))
                    .ReturnsAsync(fakeRoom);

        _mockGroupSvc.Setup(s => s.RemoveAllGroupsByRoomIdAsync(fakeRoom.Id))
                     .Returns(Task.CompletedTask);
        _mockGroupSvc.Setup(s => s.CreateGroupsAsync(fakeRoom.Id, It.IsAny<int>()))
                     .ThrowsAsync(new InvalidOperationException("Service failed"));

        var vm = new CompositeRoomViewModel { CreateGroup = new() { RoomId = fakeRoom.Id, GroupSize = 2 }};
        var result = await _controller.CreateGroups(vm);
        var json = Assert.IsType<JsonResult>(result);
        dynamic data = json.Value!;
        Assert.False((bool)data.success);
        Assert.Equal("Service failed", (string)data.message);
    }

    [Fact]
    public async Task CreateGroups_Success_ReturnsJsonSuccess()
    {
        var fakeUser = new ApplicationUser { Id = "user-123" };
        var fakeRoom = new Room { Id = Guid.NewGuid(), OwnerId = "user-123" };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(fakeUser);
        _mockRoomSvc.Setup(r => r.GetRoomByIdAsync(fakeRoom.Id))
                    .ReturnsAsync(fakeRoom);

        _mockGroupSvc.Setup(s => s.RemoveAllGroupsByRoomIdAsync(fakeRoom.Id))
                     .Returns(Task.CompletedTask);
        _mockGroupSvc.Setup(s => s.CreateGroupsAsync(fakeRoom.Id, It.IsAny<int>()))
                     .Returns(Task.CompletedTask);

        var vm = new CompositeRoomViewModel { CreateGroup = new() { RoomId = fakeRoom.Id, GroupSize = 2 }};
        var result = await _controller.CreateGroups(vm);
        var json = Assert.IsType<JsonResult>(result);
        dynamic data = json.Value!;
        Assert.True((bool)data.success);
    }
}

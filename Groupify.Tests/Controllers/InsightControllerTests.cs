// InsightControllerTests.cs
using System.Security.Claims;
using Groupify.Controllers;
using Groupify.Data.Services.Interfaces;
using Groupify.Models.Domain;
using Groupify.Models.Identity;
using Groupify.Tests.Helpers;
using Groupify.ViewModels.Insight;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Groupify.Tests.Controllers;
public class InsightControllerTests
{
    private readonly Mock<ILogger<HomeController>> _mockLogger = new();
    private readonly Mock<IInsightService> _mockInsight = new();
    private readonly Mock<UserManager<ApplicationUser>> _mockUserMgr;
    private readonly InsightController _controller;

    public InsightControllerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserMgr = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!
        );

        _controller = new InsightController(
            _mockLogger.Object,
            _mockInsight.Object,
            _mockUserMgr.Object
        );
        
        // Ensure ControllerContext is non-null
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        };
    }

    [Fact]
    public async Task Profile_UserNull_ReturnsUnauthorized()
    {
        _controller.SetUser("u1", "Student");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync((ApplicationUser)null!);

        var result = await _controller.Profile();
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Profile_NoInsightProfile_RedirectsToCreateProfile()
    {
        _controller.SetUser("u2", "Student");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "u2" });
        _mockInsight.Setup(s => s.HasInsightProfileAsync("u2"))
                    .ReturnsAsync(false);

        var result = await _controller.Profile();
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("CreateProfile", redirect.ActionName);
    }

    [Fact]
    public async Task Profile_HasInsightProfile_ReturnsViewWithModel()
    {
        _controller.SetUser("u3", "Student");
        var insight = new Insight { Red = 1, Blue = 2, Green = 3, Yellow = 4, WheelPosition = 5 };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "u3" });
        _mockInsight.Setup(s => s.HasInsightProfileAsync("u3"))
                    .ReturnsAsync(true);
        _mockInsight.Setup(s => s.GetInsightByUserIdAsync("u3"))
                    .ReturnsAsync(insight);

        var result = await _controller.Profile();
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(insight, view.Model);
    }

    // Details

    [Fact]
    public async Task Details_UserNull_ReturnsUnauthorized()
    {
        _controller.SetUser("t1", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync((ApplicationUser)null!);

        var result = await _controller.Details("any");
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Details_NoInsightProfile_RedirectsToCreateProfile()
    {
        _controller.SetUser("t2", "Teacher");
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t2" });
        _mockInsight.Setup(s => s.HasInsightProfileAsync("uX"))
                    .ReturnsAsync(false);

        var result = await _controller.Details("uX");
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("CreateProfile", redirect.ActionName);
    }

    [Fact]
    public async Task Details_HasInsightProfile_ReturnsProfileViewWithModel()
    {
        _controller.SetUser("t3", "Teacher");
        var insight = new Insight { Red = 9, Blue = 8, Green = 7, Yellow = 6, WheelPosition = 0 };
        _mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = "t3" });
        _mockInsight.Setup(s => s.HasInsightProfileAsync("uY"))
                    .ReturnsAsync(true);
        _mockInsight.Setup(s => s.GetInsightByUserIdAsync("uY"))
                    .ReturnsAsync(insight);

        var result = await _controller.Details("uY");
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Profile", view.ViewName);
        Assert.Equal(insight, view.Model);
    }

    // CreateProfile

    [Fact]
    public async Task CreateProfile_Get_NoUserId_ReturnsUnauthorized()
    {
        // no NameIdentifier claim
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        var result = await _controller.CreateProfile();
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateProfile_Get_AlreadyHasProfile_RedirectsToProfile()
    {
        _controller.SetUser("s1", "Student");
        _mockInsight.Setup(s => s.HasInsightProfileAsync("s1"))
                    .ReturnsAsync(true);

        var result = await _controller.CreateProfile();
        var red = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Profile", red.ActionName);
    }

    [Fact]
    public async Task CreateProfile_Get_NoProfile_ReturnsView()
    {
        _controller.SetUser("s2", "Student");
        _mockInsight.Setup(s => s.HasInsightProfileAsync("s2"))
                    .ReturnsAsync(false);

        var result = await _controller.CreateProfile();
        Assert.IsType<ViewResult>(result);
    }
    

    [Fact]
    public async Task CreateProfile_Post_NoUserId_ReturnsUnauthorized()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        var vm = new CreateInsightProfileViewModel();
        var result = await _controller.CreateProfile(vm);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateProfile_Post_AlreadyHasProfile_RedirectsToProfile()
    {
        _controller.SetUser("s3", "Student");
        _mockInsight.Setup(s => s.HasInsightProfileAsync("s3"))
                    .ReturnsAsync(true);

        var vm = new CreateInsightProfileViewModel();
        var res = await _controller.CreateProfile(vm);
        var red = Assert.IsType<RedirectToActionResult>(res);
        Assert.Equal("Profile", red.ActionName);
    }

    [Fact]
    public async Task CreateProfile_Post_InvalidModelState_ReturnsCreateViewWithVm()
    {
        _controller.SetUser("s4", "Student");
        _mockInsight.Setup(s => s.HasInsightProfileAsync("s4"))
                    .ReturnsAsync(false);
        _controller.ModelState.AddModelError("X", "err");

        var vm = new CreateInsightProfileViewModel { Red = 1 };
        var res = await _controller.CreateProfile(vm);
        var view = Assert.IsType<ViewResult>(res);
        Assert.Equal("CreateProfile", view.ViewName);
        Assert.Equal(vm, view.Model);
    }

    [Fact]
    public async Task CreateProfile_Post_ThrowsInvalidOperation_ReturnsCreateViewWithError()
    {
        _controller.SetUser("s5", "Student");
        _mockInsight.Setup(s => s.HasInsightProfileAsync("s5"))
                    .ReturnsAsync(false);
        _mockInsight.Setup(s => s.CreateInsightProfileAsync("s5", It.IsAny<Insight>()))
                    .ThrowsAsync(new InvalidOperationException("oops"));

        var vm = new CreateInsightProfileViewModel
        {
            Red = 1, Blue = 2, Green = 3, Yellow = 4, WheelPosition = 5
        };
        var res = await _controller.CreateProfile(vm);
        var view = Assert.IsType<ViewResult>(res);
        Assert.Equal("CreateProfile", view.ViewName);
        Assert.True(_controller.ModelState.ErrorCount > 0);
    }

    [Fact]
    public async Task CreateProfile_Post_Success_RedirectsToProfile()
    {
        _controller.SetUser("s6", "Student");
        _mockInsight.Setup(s => s.HasInsightProfileAsync("s6"))
                    .ReturnsAsync(false);
        _mockInsight.Setup(s => s.CreateInsightProfileAsync("s6", It.IsAny<Insight>()))
                    .Returns(Task.CompletedTask);

        var vm = new CreateInsightProfileViewModel
        {
            Red = 1, Blue = 2, Green = 3, Yellow = 4, WheelPosition = 5
        };
        var res = await _controller.CreateProfile(vm);
        var red = Assert.IsType<RedirectToActionResult>(res);
        Assert.Equal("Profile", red.ActionName);
    }

    // === UpdateProfile

    [Fact]
    public void UpdateProfile_Get_ReturnsView()
    {
        var result = _controller.UpdateProfile();
        Assert.IsType<ViewResult>(result);
    }
    

    [Fact]
    public async Task UpdateProfile_Post_NoUserId_ReturnsUnauthorized()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        var vm = new CreateInsightProfileViewModel();
        var res = await _controller.UpdateProfile(vm);
        Assert.IsType<UnauthorizedResult>(res);
    }

    [Fact]
    public async Task UpdateProfile_Post_InvalidModelState_ReturnsUpdateViewWithVm()
    {
        _controller.SetUser("s7", "Student");
        _controller.ModelState.AddModelError("X", "err");

        var vm = new CreateInsightProfileViewModel();
        var res = await _controller.UpdateProfile(vm);
        var view = Assert.IsType<ViewResult>(res);
        Assert.Equal("UpdateProfile", view.ViewName);
        Assert.Equal(vm, view.Model);
    }

    [Fact]
    public async Task UpdateProfile_Post_ThrowsInvalidOperation_ReturnsUpdateViewWithError()
    {
        _controller.SetUser("s8", "Student");
        _mockInsight.Setup(s => s.UpdateInsightAsync("s8", It.IsAny<Insight>()))
                    .ThrowsAsync(new InvalidOperationException("bad"));

        var vm = new CreateInsightProfileViewModel
        {
            Red = 1, Blue = 2, Green = 3, Yellow = 4, WheelPosition = 5
        };
        var res = await _controller.UpdateProfile(vm);
        var view = Assert.IsType<ViewResult>(res);
        Assert.Equal("UpdateProfile", view.ViewName);
        Assert.True(_controller.ModelState.ErrorCount > 0);
    }

    [Fact]
    public async Task UpdateProfile_Post_Success_RedirectsToProfile()
    {
        _controller.SetUser("s9", "Student");
        _mockInsight.Setup(s => s.UpdateInsightAsync("s9", It.IsAny<Insight>()))
                    .Returns(Task.CompletedTask);

        var vm = new CreateInsightProfileViewModel
        {
            Red = 1, Blue = 2, Green = 3, Yellow = 4, WheelPosition = 5
        };
        var res = await _controller.UpdateProfile(vm);
        var red = Assert.IsType<RedirectToActionResult>(res);
        Assert.Equal("Profile", red.ActionName);
    }

    // DeleteProfile

    [Fact]
    public async Task DeleteProfile_UserNotFound_ReturnsNotFound()
    {
        _controller.SetUser("admin", "Admin");
        _mockUserMgr.Setup(m => m.FindByIdAsync("X"))
                    .ReturnsAsync((ApplicationUser)null!);

        var res = await _controller.DeleteProfile("X");
        Assert.IsType<NotFoundResult>(res);
    }

    [Fact]
    public async Task DeleteProfile_NotStudent_ReturnsBadRequest()
    {
        _controller.SetUser("admin", "Admin");
        var u = new ApplicationUser { Id = "U" };
        _mockUserMgr.Setup(m => m.FindByIdAsync("U"))
                    .ReturnsAsync(u);
        _mockUserMgr.Setup(m => m.IsInRoleAsync(u, "Student"))
                    .ReturnsAsync(false);

        var res = await _controller.DeleteProfile("U");
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal("Only students can be deleted.", bad.Value);
    }

    [Fact]
    public async Task DeleteProfile_DeleteFails_ThrowsInvalidOperation()
    {
        _controller.SetUser("admin", "Admin");
        var u = new ApplicationUser { Id = "U2" };
        _mockUserMgr.Setup(m => m.FindByIdAsync("U2")).ReturnsAsync(u);
        _mockUserMgr.Setup(m => m.IsInRoleAsync(u, "Student")).ReturnsAsync(true);
        _mockUserMgr.Setup(m => m.DeleteAsync(u))
                    .ReturnsAsync(IdentityResult.Failed());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.DeleteProfile("U2"));
    }

    [Fact]
    public async Task DeleteProfile_Success_RedirectsHome()
    {
        _controller.SetUser("admin", "Admin");
        var u = new ApplicationUser { Id = "U3" };
        _mockUserMgr.Setup(m => m.FindByIdAsync("U3")).ReturnsAsync(u);
        _mockUserMgr.Setup(m => m.IsInRoleAsync(u, "Student")).ReturnsAsync(true);
        _mockUserMgr.Setup(m => m.DeleteAsync(u))
                    .ReturnsAsync(IdentityResult.Success);

        var res = await _controller.DeleteProfile("U3");
        var red = Assert.IsType<RedirectResult>(res);
        Assert.Equal("~/", red.Url);
    }
}

using Microsoft.AspNetCore.Identity;
using Groupify.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace Groupify.Data;

public class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GroupifyDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roomManager = scope.ServiceProvider.GetRequiredService<RoomService>();
        var insightService = scope.ServiceProvider.GetRequiredService<InsightService>();
        
        await context.Database.MigrateAsync();

        Guid roomId = Guid.Empty;
        
        // Create a teacher and room
        string teacherEmail = $"teacher@demo.com";
        const string teacherPassword = "P@$$w0rd";
        if (userManager.FindByEmailAsync(teacherEmail).Result == null)
        {
            var teacher = new ApplicationUser
            {
                UserName = teacherEmail,
                Email = teacherEmail,
                EmailConfirmed = true,
                FirstName = "Teacher",
                LastName = "Doe",
            };
            var result = await userManager.CreateAsync(teacher, teacherPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(teacher, "Teacher");
                
                // Create room with teacher as owner
                string ownerId = teacher.Id;
                string roomName = "Room";
                roomId = await roomManager.CreateRoomAsync(roomName, ownerId);
            }
        }
         
        // Create 20 students and add them to a room
        for (int i = 0; i < 20; i++)
        {
            string studentEmail = $"student{i}@demo.com";
            const string studentPassword = "P@$$w0rd";
            if (userManager.FindByEmailAsync(studentEmail).Result == null)
            {
                var student = new ApplicationUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    EmailConfirmed = true,
                    FirstName = "Student",
                    LastName = $"{i}",
                };
                
                var result = await userManager.CreateAsync(student, studentPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(student, "Student");
                    await insightService.CreateInsightProfileAsync(student.Id);
                    
                    // Add student to room
                    if (roomId != Guid.Empty)
                        await roomManager.AddUserToRoomAsync(student.Id, roomId);
                }
            }
        }
    }
}
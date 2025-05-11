using ExcelDataReader;
using Groupify.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Groupify.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace Groupify.Data;

public class SeedData
{
    private static readonly ILogger<SeedData> _logger;
    
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GroupifyDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roomManager = scope.ServiceProvider.GetRequiredService<RoomService>();
        var insightService = scope.ServiceProvider.GetRequiredService<InsightService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedData>>();
        
        
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
        for (int i = 0; i < 60; i++)
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
                    
                    // Create a new insight profile for the student from xlsx file
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "DummyProfiles.xlsx");
                    
                    Insight insight = new Insight
                    {
                        Red = 0,
                        Blue = 0,
                        Green = 0,
                        Yellow = 0,
                        WheelPosition = 0
                    };
                    
                    // Read the xlsx file and populate the insight profile
                    await using (var xStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = ExcelReaderFactory.CreateReader(xStream))
                        {
                            // Get the first worksheet
                            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                            {
                                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                                {
                                    UseHeaderRow = true // Use the first row as column names (just used to skip it)
                                }
                            });
                            
                            var worksheet = dataSet.Tables[0];

                            if (worksheet.Rows.Count > 1)
                            {
                                var row = worksheet.Rows[i]; // Use i from student loop to get different rows
                                insight.Blue = Convert.ToSingle(row[0]);
                                insight.Green = Convert.ToSingle(row[1]);
                                insight.Yellow = Convert.ToSingle(row[2]);
                                insight.Red = Convert.ToSingle(row[3]);
                                insight.WheelPosition = Convert.ToInt32(row[4]);
                            }
                        }
                    }
                    
                    // Update insight profile
                    await insightService.UpdateInsightAsync(student.Id, insight);
                    
                    // Add student to room
                    if (roomId != Guid.Empty)
                        await roomManager.AddUserToRoomAsync(student.Id, roomId);
                }
            }
        }
    }
}
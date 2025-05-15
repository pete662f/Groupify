using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ExcelDataReader;
using Groupify.Models.Domain;
using Groupify.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Groupify.Data
{
    public class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GroupifyDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roomManager = scope.ServiceProvider.GetRequiredService<RoomService>();
            var insightService = scope.ServiceProvider.GetRequiredService<InsightService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedData>>();

            await context.Database.MigrateAsync();
            
            
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "DummyProfiles.xlsx");
            var insight = new Insight();
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });
            var worksheet = dataSet.Tables[0];
            

            const string testPassword = "P@$$w0rd";
            
            // Create an admin user owning a large room with 1000 students
            string adminEmail = "admin0@demo.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                ApplicationUser admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Teacher",
                    LastName = "Doe",
                };
                var result = await userManager.CreateAsync(admin, testPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    await userManager.AddToRoleAsync(admin, "Teacher");
                    
                    
                    Guid bigRoomId = await roomManager.CreateRoomAsync("Mega Room", admin.Id);
                    
                    // Create a single large room with 1000 students
                    for (int i = 0; i < 1000; i++)
                    {
                        string bulkEmail = $"bulkstudent{i}@demo.com";
                        if (await userManager.FindByEmailAsync(bulkEmail) == null)
                        {
                            var bulkStudent = new ApplicationUser
                            {
                                UserName = bulkEmail,
                                Email = bulkEmail,
                                EmailConfirmed = true,
                                FirstName = "Bulk",
                                LastName = $"Student{i}",
                            };
                            var studentResult = await userManager.CreateAsync(bulkStudent, testPassword);
                            if (studentResult.Succeeded)
                            {
                                await userManager.AddToRoleAsync(bulkStudent, "Student");

                                // Create insight profile for the student
                                int nr = i % 490; // To cycle through the rows in the Excel
                                if (worksheet.Rows.Count > nr + 1)
                                {
                                    var row = worksheet.Rows[nr + 1];
                                    insight.Blue = Convert.ToSingle(row[0]);
                                    insight.Green = Convert.ToSingle(row[1]);
                                    insight.Yellow = Convert.ToSingle(row[2]);
                                    insight.Red = Convert.ToSingle(row[3]);
                                    insight.WheelPosition = Convert.ToInt32(row[4]);
                                }
                                await insightService.CreateInsightProfileAsync(bulkStudent.Id, insight);

                                // Add to big room
                                await roomManager.AddUserToRoomAsync(bulkStudent.Id, bigRoomId);
                            }
                        }
                    }
                }
            }

            // Create 2 teachers, each with 2 rooms, and 60 students per teacher assigned to all rooms
            for (int i = 0; i < 2; i++)
            {
                string teacherEmail = $"teacher{i}@demo.com";
                if (await userManager.FindByEmailAsync(teacherEmail) == null)
                {
                    var teacher = new ApplicationUser
                    {
                        UserName = teacherEmail,
                        Email = teacherEmail,
                        EmailConfirmed = true,
                        FirstName = "Teacher",
                        LastName = "Doe",
                    };
                    var teacherResult = await userManager.CreateAsync(teacher, testPassword);
                    if (teacherResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(teacher, "Teacher");

                        // Create 2 rooms for this teacher and collect their IDs
                        var roomIds = new List<Guid>();
                        for (int j = 0; j < 2; j++)
                        {
                            string roomName = $"Room {i + 1}-{j + 1}";
                            var newRoomId = await roomManager.CreateRoomAsync(roomName, teacher.Id);
                            roomIds.Add(newRoomId);
                        }

                        // Create 60 students and add each to all rooms
                        for (int j = 0; j < 60; j++)
                        {
                            string studentEmail = $"student{i}_{j}@demo.com";
                            if (await userManager.FindByEmailAsync(studentEmail) == null)
                            {
                                var student = new ApplicationUser
                                {
                                    UserName = studentEmail,
                                    Email = studentEmail,
                                    EmailConfirmed = true,
                                    FirstName = "Student",
                                    LastName = $"{i}_{j}",
                                };

                                var studentResult = await userManager.CreateAsync(student, testPassword);
                                if (studentResult.Succeeded)
                                {
                                    await userManager.AddToRoleAsync(student, "Student");

                                    // Create insight profile for the student
                                    if (worksheet.Rows.Count > j + 1)
                                    {
                                        var row = worksheet.Rows[j + 1];
                                        insight.Blue = Convert.ToSingle(row[0]);
                                        insight.Green = Convert.ToSingle(row[1]);
                                        insight.Yellow = Convert.ToSingle(row[2]);
                                        insight.Red = Convert.ToSingle(row[3]);
                                        insight.WheelPosition = Convert.ToInt32(row[4]);
                                    }
                                    await insightService.CreateInsightProfileAsync(student.Id, insight);

                                    // Add student to all rooms
                                    foreach (var rid in roomIds)
                                    {
                                        await roomManager.AddUserToRoomAsync(student.Id, rid);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

using Groupify.Data;
using Groupify.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace Groupify.Models
{
    public class SeedData
    {
            public static void SeedRolesUser(RoleManager<IdentityRole> roleManager, UserManager<UserAccount> userManager, ApplicationDbContext context)
            {
                SeedRoles(roleManager);
                SeedUsers(userManager, context);
            }

            private static void SeedRoles(RoleManager<IdentityRole> roleManager)
            {
                if (!roleManager.RoleExistsAsync("Student").Result)
                {
                    IdentityRole role = new IdentityRole();
                    role.Name = "Student";
                    IdentityResult roleResult = roleManager.
                    CreateAsync(role).Result;
                }

                if (!roleManager.RoleExistsAsync("Employee").Result)
                {
                    IdentityRole role = new IdentityRole();
                    role.Name = "Employee";
                    IdentityResult roleResult = roleManager.
                    CreateAsync(role).Result;
                }

                if (!roleManager.RoleExistsAsync("Admin").Result)
                {
                    IdentityRole role = new IdentityRole();
                    role.Name = "Admin";
                    IdentityResult roleResult = roleManager.
                    CreateAsync(role).Result;
                }
            }

            private static void SeedUsers(UserManager<UserAccount> userManager, ApplicationDbContext context)
            {
                if (userManager.FindByEmailAsync("student1@mail.com").Result == null)
                {
                    Student student = new()
                    {
                        Email = "student1@mail.com",
                        FirstName = "John",
                        LastName = "Student"
                    };
                    context.Add(student);
                    context.SaveChanges();

                    UserAccount user = new()
                    {
                        UserName = student.Email,
                        Email = student.Email,
                        FirstName = student.FirstName,
                        LastName = student.LastName,
                        StudentNum = student.Id,
                        IsStudent = true

                    };

                    IdentityResult result = userManager.CreateAsync(user, "K*de0rd").Result;

                    if (result.Succeeded)
                    {
                        userManager.AddToRoleAsync(user, "Student").Wait();
                    }
                }

                if (userManager.FindByEmailAsync("employee1@mail.com").Result == null)
                {
                    Employee employee = new()
                    {
                        Email = "employee1@mail.com",
                        FirstName = "Jane",
                        LastName = "Employee"
                    };
                    context.Add(employee);
                    context.SaveChanges();

                    UserAccount user = new()
                    {
                        UserName = employee.Email,
                        Email = employee.Email,
                        FirstName = employee.FirstName,
                        LastName = employee.LastName,
                        EmployeeNum = employee.Id,
                        IsEmployee = true
                    };

                    IdentityResult result = userManager.CreateAsync(user, "K*de0rd").Result;

                    if (result.Succeeded)
                        userManager.AddToRoleAsync(user, "Employee").Wait();

                }

                if (userManager.FindByEmailAsync("admin@mail.com").Result == null)
                {
                    // Perhaps add an admin model, if necessary?
                    UserAccount user = new()
                    {
                        UserName = "admin@mail.com",
                        Email = "admin@mail.com",
                        FirstName = "Allan",
                        LastName = "Admin",
                    };

                    IdentityResult result = userManager.CreateAsync(user, "K*de0rd").Result;

                    if (result.Succeeded)
                        userManager.AddToRoleAsync(user, "Admin").Wait();
                }
            }
    }
}

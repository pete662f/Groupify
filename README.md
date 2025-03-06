# Groupify
Use Visual Studio. It works for C#.

## After cloning
1. Open "Developer Powershell" and run command: `msbuild -t:restore` (Theoretically not neccessary but just to be sure). If the console is not found on the bottom already goto: "Tools" -> "Command Line" ->  "Developer Powershell".
2. Open "Package Manager Console" also called PMC. If the console is not found on the bottom already goto: "Tools" -> "NuGet Package Manager" -> "Package Manager Console".
  3. If an error along the lines of "The term 'Update-Database' is not recognized as the name of a cmdlet" is thrown, restart Visual Studio and do it again.
4. To Add/update database: use command `Update-Database` in PMC.
5. Run the program (The green arrow button)

![image](https://github.com/user-attachments/assets/60fab5fb-6efa-480d-a4bc-b3c0fcf0a5ba)


### Seed data
I have created seed data (see: Models\SeedData.cs). This means the database will be populated with the following first time it runs:
- 3 user accounts
  - A student with email student1@mail.com
  - A employee with email employee1@mail.com
  - An admin with email admin@mail.com
- 3 roles
  - Student
  - Employee
  - Admin
All users share the same hardcoded unbreakable password: K*de0rd

## Working with database
1. Add any new models to Data\ApplicationDbContext.cs (if you are changing existing models, this step is already done).
2.   E.g. to add a table based of the model "Employee" the following line is used: public DbSet<Employee> Employees { get; set; }
3. After adding the models, a migration need to be added. In the PMC use command: `Add-Migration InsertAShortDescriptionOfChangesHereWithNoSpaces`
4. Check the migration file. Sucks if you don't read SQL, sorry.
5. If migration file does what you want it to, in the PMC use command: *Update-Database*.

**View data in databases**
1. Open "SQL Server Object Explorer" under view.
2. Navigate to wanted table.
3. This is usually found under: "(localdb)\MSSQLLocalDB (Some more random stuff)" -> "Databases" -> "aspnet-Groupify-random stuff" -> "Tables" -> "dbo.whatever table you want"
4. Right click on the wanted table and choose "View data"
5. **NOTE** IT IS VERY IMPORTANT NOT TO CHANGE ANYTHING USING THE SQL Server Object Explorer. THE CODE WILL BREAK. I PROMISE! Everything has to be changed from the actual code.
6. Important tables: dbo.AspNetUsers (this contains the users that can login); abo.AspNetRoles (this contains the roles a user can have. Currently 3 roles: student, employee, admin)

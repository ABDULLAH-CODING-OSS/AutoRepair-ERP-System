using System;
using System.Linq;
using System.Threading.Tasks;
using AutoRepairERD.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Services
{
    /// <summary>
    /// Seeds the database with the roles the application depends on (RoleAuthorize attributes
    /// reference these by exact name) plus one demo login per role so the system is usable the
    /// first time it is run, without shipping any fake business data into the UI itself.
    ///
    /// This runs once at startup (see Program.cs) and is fully idempotent - it only inserts rows
    /// that do not already exist, so it is safe to leave in place permanently.
    /// </summary>
    public static class DbSeeder
    {
        // NOTE: passwords are stored as plain text in this project (see AuthController, which
        // compares PasswordHash == model.Password directly). That matches the existing codebase's
        // behavior. Before going to production you should replace this with a real hashing
        // scheme (e.g. ASP.NET Core Identity's PasswordHasher) - see the integration notes.
        private const string DemoPassword = "Passw0rd!";

        public static async Task SeedAsync(ApplicationDbContext context)
        {
            await context.Database.MigrateAsync();

            // ---- Roles -----------------------------------------------------------
            // These exact names are referenced by [RoleAuthorize("...")] attributes throughout
            // the Controllers, so they must match exactly.
            var roleNames = new[]
            {
                ("Owner", "Full system access: dashboards, reports, payroll, all modules."),
                ("Admin", "User management, audit logs, all operational views."),
                ("Service Advisor", "Customers, vehicles, job orders, invoices."),
                ("Mechanic", "Assigned jobs and personal attendance."),
                ("Inventory Manager", "Parts, suppliers, purchase orders, stock."),
                ("Receptionist", "Front-desk customer and vehicle intake."),
            };

            foreach (var (name, description) in roleNames)
            {
                if (!await context.Roles.AnyAsync(r => r.RoleName == name))
                {
                    context.Roles.Add(new Role { RoleName = name, Description = description });
                }
            }
            await context.SaveChangesAsync();

            // ---- Demo users (one per primary role, mirrors the four roles the UI is built for) ----
            var demoUsers = new[]
            {
                new { Username = "owner", FullName = "Fawad Ahmad", Role = "Owner", Designation = "Owner", Email = "owner@autorepair.local" },
                new { Username = "admin", FullName = "Abdullah Javed", Role = "Admin", Designation = "Admin", Email = "admin@autorepair.local" },
                new { Username = "advisor", FullName = "Muhammad Bazil", Role = "Service Advisor", Designation = "Service Advisor", Email = "advisor@autorepair.local" },
                new { Username = "mechanic", FullName = "Muhammad Zain", Role = "Mechanic", Designation = "Mechanic", Email = "mechanic@autorepair.local" },
            };

            foreach (var d in demoUsers)
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.Username == d.Username);
                if (user == null)
                {
                    user = new User
                    {
                        Username = d.Username,
                        PasswordHash = DemoPassword,
                        Email = d.Email,
                        FullName = d.FullName,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    context.Users.Add(user);
                    await context.SaveChangesAsync();
                }

                var role = await context.Roles.FirstAsync(r => r.RoleName == d.Role);
                var hasRole = await context.UserRoles.AnyAsync(ur => ur.UserId == user.UserId && ur.RoleId == role.RoleId);
                if (!hasRole)
                {
                    context.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
                }

                // Give Service Advisor / Mechanic demo users an Employee record so they show up
                // in advisor/mechanic assignment dropdowns and can use MyAssignedJobs / Attendance.
                if (d.Role is "Service Advisor" or "Mechanic" or "Owner" or "Admin")
                {
                    var hasEmployee = await context.Employees.AnyAsync(e => e.UserId == user.UserId);
                    if (!hasEmployee)
                    {
                        var nextNumber = await context.Employees.CountAsync() + 1;
                        var nameParts = d.FullName.Split(' ', 2);
                        context.Employees.Add(new Employee
                        {
                            UserId = user.UserId,
                            EmployeeCode = $"EMP{nextNumber:D3}",
                            FirstName = nameParts[0],
                            LastName = nameParts.Length > 1 ? nameParts[1] : "",
                            Cnic = "00000-0000000-0",
                            Phone = "03000000000",
                            HireDate = DateOnly.FromDateTime(DateTime.Now),
                            Designation = d.Designation,
                            BasicSalary = 0,
                            HourlyRate = 0,
                            IsActive = true
                        });
                    }
                }

                await context.SaveChangesAsync();
            }
        }
    }
}

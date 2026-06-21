
using AutoRepairERD.Filters;
using AutoRepairERD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
[RoleAuthorize("Admin")]

[RoleAuthorize("Admin","Owner")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.NotificationService _notificationService;

    public UsersController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    // GET: USERS
    public async Task<IActionResult> Index()
    {
        // Basic listing with related data to avoid N+1 queries
        var usersQuery = _context.Users
            .Include(u => u.Employees)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsQueryable();

        return View(await usersQuery.ToListAsync());
    }

    // GET: USERS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users
            .Include(u => u.Employees)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(m => m.UserId == id);
        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    // NOTE: Create actions removed. Users are created by the Employee module only.

    // GET: USERS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id  == null)
        {
            return NotFound();
        }
        var user = await _context.Users
            .Include(u => u.Employees)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null)
        {
            return NotFound();
        }
        // Edit page is account management only: employee and roles are read-only in the view
        return View(user);
    }

    // POST: USERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, string? email, bool? isActive)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users
            .Include(u => u.Employees)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            return NotFound();
        }

        // Validate email
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("Email", "Email is required.");
        }
        else
        {
            var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            if (!emailAttr.IsValid(email))
            {
                ModelState.AddModelError("Email", "Invalid email address.");
            }
            else
            {
                // Check duplicate email
                var duplicate = await _context.Users.AnyAsync(u => u.UserId != id && u.Email == email);
                if (duplicate)
                {
                    ModelState.AddModelError("Email", "This email is already in use by another account.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            // Preserve attempted values for display
            user.Email = email;
            user.IsActive = isActive;
            return View(user);
        }

        // Apply updates
        var previousIsActive = user.IsActive ?? false;
        user.Email = email;
        user.IsActive = isActive;
        _context.Update(user);

        // Sync linked employees' IsActive to match user
        var linkedEmployees = await _context.Employees.Where(e => e.UserId == user.UserId).ToListAsync();
        if (linkedEmployees.Any())
        {
            foreach (var emp in linkedEmployees)
            {
                emp.IsActive = isActive;
            }
            _context.Employees.UpdateRange(linkedEmployees);
        }

        await _context.SaveChangesAsync();

        // Audit: User Updated (email / active)
        try
        {
            var oldVals = $"Email={user.Email};IsActive={previousIsActive}";
            var newVals = $"Email={email};IsActive={user.IsActive}";
            var audit = new AuditLog
            {
                UserId = HttpContext.Session.GetInt32("UserID"),
                TableName = "Users",
                RecordId = user.UserId,
                ActionType = "User Updated",
                OldValues = oldVals,
                NewValues = newVals,
                ActionDate = DateTime.Now,
                Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Do not block user update on audit failure
        }

        // Batch 2: EMPLOYEE ACCOUNT ACTIVATED / DEACTIVATED notifications
        try
        {
            var newIsActive = user.IsActive ?? false;
            if (!previousIsActive && newIsActive)
            {
                // Activated
                var title = "Employee Account Activated";
                var message = $"Employee account {user.Username} has been activated.";
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                var ownerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Owner");
                if (adminRole != null)
                    await _notificationService.CreateForRoleAsync(adminRole.RoleId, "Employees", title, message);
                if (ownerRole != null)
                    await _notificationService.CreateForRoleAsync(ownerRole.RoleId, "Employees", title, message);
            }
            else if (previousIsActive && !(user.IsActive ?? false))
            {
                // Deactivated
                var title = "Employee Account Deactivated";
                var message = $"Employee account {user.Username} has been deactivated.";
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                var ownerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Owner");
                if (adminRole != null)
                    await _notificationService.CreateForRoleAsync(adminRole.RoleId, "Employees", title, message);
                if (ownerRole != null)
                    await _notificationService.CreateForRoleAsync(ownerRole.RoleId, "Employees", title, message);
            }
        }
        catch
        {
            // Do not block user update on notification failures (best-effort)
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: USERS/Deactivate/5
    public async Task<IActionResult> Deactivate(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users
            .Include(u => u.Employees)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(m => m.UserId == id);
        if (user == null)
        {
            return NotFound();
        }

        return View("Deactivate", user);
    }

    // POST: USERS/DeactivateConfirmed/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateConfirmed(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Soft-deactivate the user
        user.IsActive = false;
        _context.Users.Update(user);

        // Also deactivate any linked Employee records to keep user/employee status consistent
        var linkedEmployees = await _context.Employees.Where(e => e.UserId == user.UserId).ToListAsync();
        if (linkedEmployees.Any())
        {
            foreach (var emp in linkedEmployees)
            {
                emp.IsActive = false;
            }
            _context.Employees.UpdateRange(linkedEmployees);
        }

        await _context.SaveChangesAsync();
        TempData["Message"] = "User account deactivated.";

        // Batch 2: EMPLOYEE ACCOUNT DEACTIVATED notification (Admin and Owner)
        try
        {
            var title = "Employee Account Deactivated";
            var message = $"Employee account {user.Username} has been deactivated.";
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            var ownerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Owner");
            if (adminRole != null)
                await _notificationService.CreateForRoleAsync(adminRole.RoleId, "Employees", title, message);
            if (ownerRole != null)
                await _notificationService.CreateForRoleAsync(ownerRole.RoleId, "Employees", title, message);
        }
        catch
        {
            // swallow notification errors
        }

        return RedirectToAction(nameof(Index));
    }

    private bool UserExists(int? id)
    {
        return _context.Users.Any(e => e.UserId == id);
    }
}

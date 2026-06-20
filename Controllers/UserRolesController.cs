
using AutoRepairERD.Filters;
using AutoRepairERD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
[RoleAuthorize("Admin")]

[RoleAuthorize("Admin","Owner")]
public class UserRolesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.NotificationService _notificationService;

    public UserRolesController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    // GET: USERROLES
    public async Task<IActionResult> Index(string username = "", string fullname = "", int? roleId = null)
    {
        var query = _context.UserRoles
            .Include(ur => ur.User)
                .ThenInclude(u => u.Employees)
            .Include(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(username))
            query = query.Where(ur => ur.User.Username.Contains(username));

        if (!string.IsNullOrWhiteSpace(fullname))
            query = query.Where(ur => (ur.User.FullName ?? "").Contains(fullname));

        if (roleId.HasValue)
            query = query.Where(ur => ur.RoleId == roleId.Value);

        var list = await query.OrderBy(ur => ur.User.Username).ToListAsync();

        // Prepare role filter list
        var roles = await _context.Roles.OrderBy(r => r.RoleName)
            .Select(r => new { r.RoleId, r.RoleName }).ToListAsync();
        ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName", roleId);
        ViewBag.Count = list.Count;
        ViewBag.SearchUsername = username;
        ViewBag.SearchFullName = fullname;

        return View(list);
    }

    // GET: USERROLES/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var userrole = await _context.UserRoles
            .Include(ur => ur.User)
                .ThenInclude(u => u.Employees)
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(m => m.UserRoleId == id);

        if (userrole == null)
            return NotFound();

        return View(userrole);
    }

    // GET: USERROLES/Create
    public IActionResult Create()
    {
        PopulateUsersAndRoles();
        return View();
    }

    // POST: USERROLES/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int? UserId, int? RoleId)
    {
        // Validate incoming ids
        if (!UserId.HasValue || UserId.Value <= 0)
            ModelState.AddModelError("UserId", "User is required.");
        if (!RoleId.HasValue || RoleId.Value <= 0)
            ModelState.AddModelError("RoleId", "Role is required.");

        // verify user and role exist
        var userExists = UserId.HasValue && _context.Users.Any(u => u.UserId == UserId.Value);
        var roleExists = RoleId.HasValue && _context.Roles.Any(r => r.RoleId == RoleId.Value);
        if (!userExists)
            ModelState.AddModelError("UserId", "Selected user does not exist.");
        if (!roleExists)
            ModelState.AddModelError("RoleId", "Selected role does not exist.");

        // prevent duplicate assignment
        if (UserId.HasValue && RoleId.HasValue && _context.UserRoles.Any(ur => ur.UserId == UserId.Value && ur.RoleId == RoleId.Value))
        {
            ModelState.AddModelError(string.Empty, "This user already has the selected role assigned.");
        }

        if (ModelState.IsValid)
        {
            var userrole = new UserRole { UserId = UserId.Value, RoleId = RoleId.Value };
            _context.UserRoles.Add(userrole);
            await _context.SaveChangesAsync();
            TempData["Toast"] = "Role assigned to user.";
            TempData["ToastType"] = "success";
            // Batch 2: ROLE ASSIGNED notification (to Admin)
            try
            {
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                if (adminRole != null)
                {
                    var user = await _context.Users.FindAsync(UserId.Value);
                    var role = await _context.Roles.FindAsync(RoleId.Value);
                    if (user != null && role != null)
                    {
                        var title = "Role Assigned";
                        var message = $"{role.RoleName} role assigned to {user.FullName ?? user.Username}.";
                        await _notificationService.CreateForRoleAsync(adminRole.RoleId, "System", title, message);
                    }
                }
            }
            catch
            {
                // swallow
            }
            return RedirectToAction(nameof(Index));
        }

        PopulateUsersAndRoles(UserId, RoleId);
        // Recreate a minimal model to return to view so validation messages can show
        var vm = new UserRole { UserId = UserId ?? 0, RoleId = RoleId ?? 0 };
        return View(vm);
    }

    // GET: USERROLES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();
        var userrole = await _context.UserRoles
            .Include(ur => ur.User)
                .ThenInclude(u => u.Employees)
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.UserRoleId == id);
        if (userrole == null)
            return NotFound();

        PopulateUsersAndRoles(userrole.UserId, userrole.RoleId);
        return View(userrole);
    }

    // POST: USERROLES/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, int? UserId, int? RoleId)
    {
        var existing = await _context.UserRoles.FindAsync(id);
        if (existing == null) return NotFound();

        if (!UserId.HasValue || UserId.Value <= 0)
            ModelState.AddModelError("UserId", "User is required.");
        if (!RoleId.HasValue || RoleId.Value <= 0)
            ModelState.AddModelError("RoleId", "Role is required.");

        var userExists = UserId.HasValue && _context.Users.Any(u => u.UserId == UserId.Value);
        var roleExists = RoleId.HasValue && _context.Roles.Any(r => r.RoleId == RoleId.Value);
        if (!userExists)
            ModelState.AddModelError("UserId", "Selected user does not exist.");
        if (!roleExists)
            ModelState.AddModelError("RoleId", "Selected role does not exist.");

        // prevent duplicates excluding current
        if (UserId.HasValue && RoleId.HasValue && _context.UserRoles.Any(ur => ur.UserId == UserId.Value && ur.RoleId == RoleId.Value && ur.UserRoleId != id))
        {
            ModelState.AddModelError(string.Empty, "This user already has the selected role assigned.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                existing.UserId = UserId.Value;
                existing.RoleId = RoleId.Value;

                _context.Update(existing);
                await _context.SaveChangesAsync();
                TempData["Toast"] = "Assignment updated.";
                TempData["ToastType"] = "success";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserRoleExists(id))
                    return NotFound();
                else
                    throw;
            }
        }

        PopulateUsersAndRoles(UserId, RoleId);
        var vm = new UserRole { UserRoleId = id, UserId = UserId ?? 0, RoleId = RoleId ?? 0 };
        return View(vm);
    }

    // GET: USERROLES/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var userrole = await _context.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(m => m.UserRoleId == id);

        if (userrole == null)
            return NotFound();

        return View(userrole);
    }

    // POST: USERROLES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var userrole = await _context.UserRoles.FindAsync(id);
        if (userrole != null)
        {
            // Capture details for notification before deletion
            var user = await _context.Users.FindAsync(userrole.UserId);
            var role = await _context.Roles.FindAsync(userrole.RoleId);

            _context.UserRoles.Remove(userrole);
            await _context.SaveChangesAsync();
            TempData["Toast"] = "Assignment removed.";
            TempData["ToastType"] = "success";

            // Batch 2: ROLE REMOVED notification (to Admin)
            try
            {
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                if (adminRole != null && user != null && role != null)
                {
                    var title = "Role Removed";
                    var message = $"{role.RoleName} role removed from {user.FullName ?? user.Username}.";
                    await _notificationService.CreateForRoleAsync(adminRole.RoleId, "System", title, message);
                }
            }
            catch
            {
                // swallow
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private bool UserRoleExists(int? id)
    {
        return _context.UserRoles.Any(e => e.UserRoleId == id);
    }

    private void PopulateUsersAndRoles(int? selectedUserId = null, int? selectedRoleId = null)
    {
        var users = _context.Users
            .AsNoTracking()
            .GroupBy(u => new { u.UserId, u.Username, u.FullName })
            .Select(g => new { UserId = g.Key.UserId, Display = (g.Key.Username ?? "") + (string.IsNullOrEmpty(g.Key.FullName) ? "" : " - " + g.Key.FullName) })
            .OrderBy(u => u.Display)
            .ToList();
        // Roles: exclude Owner from assignment. Admin role only assignable by Owner.
        var rolesQuery = _context.Roles.AsQueryable();
        rolesQuery = rolesQuery.Where(r => r.RoleName != "Owner");
        var sessionRole = HttpContext?.Session.GetString("RoleName");
        if (!string.Equals(sessionRole, "Owner", StringComparison.OrdinalIgnoreCase))
        {
            // Non-owner cannot assign Admin role
            rolesQuery = rolesQuery.Where(r => r.RoleName != "Admin");
        }

        var roles = rolesQuery.AsNoTracking().OrderBy(r => r.RoleName)
            .Select(r => new { r.RoleId, r.RoleName })
            .ToList();

        ViewBag.Users = new SelectList(users, "UserId", "Display", selectedUserId);
        ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName", selectedRoleId);
    }
}


using AutoRepairERD.Filters;
using AutoRepairERD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
[RoleAuthorize("Admin")]

[RoleAuthorize("Admin","Owner")]
public class RolesController : Controller
{
    private readonly ApplicationDbContext _context;

    public RolesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: ROLES
    public async Task<IActionResult> Index(string search = "", string filter = "all")
    {
        // Seed defaults if no roles
        if (!await _context.Roles.AnyAsync())
        {
            var defaults = new[] { "Owner", "Admin", "Service Advisor", "Mechanic", "Inventory Manager", "Receptionist" };
            foreach (var r in defaults)
            {
                _context.Roles.Add(new Role { RoleName = r, Description = r + " role" });
            }
            await _context.SaveChangesAsync();
        }

        var rolesQuery = _context.Roles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            rolesQuery = rolesQuery.Where(r => r.RoleName.Contains(search));

        ViewBag.Search = search;
        ViewBag.Filter = filter;

        var list = await rolesQuery
            .Select(r => new RoleListItem
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                Description = r.Description,
                AssignedCount = _context.UserRoles.Count(ur => ur.RoleId == r.RoleId)
            })
            .OrderBy(r => r.RoleName)
            .ToListAsync();

        return View(list);
    }

    // GET: ROLES/Details/5
    public async Task<IActionResult> Details(int?  id)
    {
        if (id  == null)
        {
            return NotFound();
        }

        var role = await _context.Roles
            .FirstOrDefaultAsync(m => m.RoleId == id);
        if (role == null)
        {
            return NotFound();
        }

        return View(role);
    }

    // GET: ROLES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ROLES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("RoleId,RoleName,Description,UserRoles")] Role role)
    {
        if (string.IsNullOrWhiteSpace(role.RoleName))
            ModelState.AddModelError("RoleName", "Role Name is required.");

        if (_context.Roles.Any(r => r.RoleName == role.RoleName))
            ModelState.AddModelError("RoleName", "Role name already exists.");

        if (ModelState.IsValid)
        {
            _context.Add(role);
            await _context.SaveChangesAsync();
            TempData["Toast"] = "Role created.";
            TempData["ToastType"] = "success";
            return RedirectToAction(nameof(Index));
        }

        return View(role);
    }

    // GET: ROLES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var role = await _context.Roles.FindAsync(id);
        if (role == null)
        {
            return NotFound();
        }
        return View(role);
    }

    // POST: ROLES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("RoleId,RoleName,Description,UserRoles")] Role role)
    {
        if (id != role.RoleId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _context.Roles.FindAsync(id);
                if (existing == null) return NotFound();

                existing.RoleName = role.RoleName;
                existing.Description = role.Description;

                _context.Update(existing);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoleExists(role.RoleId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(role);
    }

    // GET: ROLES/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var role = await _context.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(m => m.RoleId == id);
        if (role == null)
        {
            return NotFound();
        }

        ViewBag.AssignedCount = role.UserRoles?.Count() ?? 0;
        return View(role);
    }

    // POST: ROLES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var role = await _context.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.RoleId == id);

        if (role == null)
            return NotFound();

        if (role.UserRoles != null && role.UserRoles.Any())
        {
            TempData["Toast"] = "This role is assigned to users and cannot be deleted.";
            TempData["ToastType"] = "danger";
            return RedirectToAction(nameof(Delete), new { id });
        }

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();

        TempData["Toast"] = "Role deleted.";
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Index));
    }

    private bool RoleExists(int? id)
    {
        return _context.Roles.Any(e => e.RoleId == id);
    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

public class UserRolesController : Controller
{
    private readonly ApplicationDbContext _context;

    public UserRolesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: USERROLES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.UserRoles.ToListAsync());
    }

    // GET: USERROLES/Details/5
    public async Task<IActionResult> Details(int? userroleid)
    {
        if (userroleid == null)
        {
            return NotFound();
        }

        var userrole = await _context.UserRoles
            .FirstOrDefaultAsync(m => m.UserRoleId == userroleid);
        if (userrole == null)
        {
            return NotFound();
        }

        return View(userrole);
    }

    // GET: USERROLES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: USERROLES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("UserRoleId,UserId,RoleId,Role,User")] UserRole userrole)
    {
        if (ModelState.IsValid)
        {
            _context.Add(userrole);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(userrole);
    }

    // GET: USERROLES/Edit/5
    public async Task<IActionResult> Edit(int? userroleid)
    {
        if (userroleid == null)
        {
            return NotFound();
        }

        var userrole = await _context.UserRoles.FindAsync(userroleid);
        if (userrole == null)
        {
            return NotFound();
        }
        return View(userrole);
    }

    // POST: USERROLES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? userroleid, [Bind("UserRoleId,UserId,RoleId,Role,User")] UserRole userrole)
    {
        if (userroleid != userrole.UserRoleId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(userrole);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserRoleExists(userrole.UserRoleId))
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
        return View(userrole);
    }

    // GET: USERROLES/Delete/5
    public async Task<IActionResult> Delete(int? userroleid)
    {
        if (userroleid == null)
        {
            return NotFound();
        }

        var userrole = await _context.UserRoles
            .FirstOrDefaultAsync(m => m.UserRoleId == userroleid);
        if (userrole == null)
        {
            return NotFound();
        }

        return View(userrole);
    }

    // POST: USERROLES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? userroleid)
    {
        var userrole = await _context.UserRoles.FindAsync(userroleid);
        if (userrole != null)
        {
            _context.UserRoles.Remove(userrole);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool UserRoleExists(int? userroleid)
    {
        return _context.UserRoles.Any(e => e.UserRoleId == userroleid);
    }
}

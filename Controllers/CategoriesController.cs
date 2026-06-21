
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
[SessionAuthorize]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: CATEGORYS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Categories.ToListAsync());
    }

    // GET: CATEGORYS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(m => m.CategoryId == id);
        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    // GET: CATEGORYS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: CATEGORYS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CategoryId,CategoryName,Description,Parts")] Category category)
    {
        if (ModelState.IsValid)
        {
            _context.Add(category);
            await _context.SaveChangesAsync();
            // Audit: Category Created (best-effort)
            try
            {
                var audit = new AuditLog
                {
                    UserId = HttpContext.Session.GetInt32("UserID"),
                    TableName = "Categories",
                    RecordId = category.CategoryId,
                    ActionType = "Category Created",
                    OldValues = null,
                    NewValues = $"CategoryName={category.CategoryName};Description={category.Description}",
                    ActionDate = DateTime.Now,
                    Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.AuditLogs.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch { }
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // GET: CATEGORYS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    // POST: CATEGORYS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("CategoryId,CategoryName,Description,Parts")] Category category)
    {
        if (id != category.CategoryId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == category.CategoryId);
                _context.Update(category);
                await _context.SaveChangesAsync();
                // Audit: Category Updated (best-effort)
                try
                {
                    var audit = new AuditLog
                    {
                        UserId = HttpContext.Session.GetInt32("UserID"),
                        TableName = "Categories",
                        RecordId = category.CategoryId,
                        ActionType = "Category Updated",
                        OldValues = existing != null ? $"CategoryName={existing.CategoryName};Description={existing.Description}" : null,
                        NewValues = $"CategoryName={category.CategoryName};Description={category.Description}",
                        ActionDate = DateTime.Now,
                        Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    };
                    _context.AuditLogs.Add(audit);
                    await _context.SaveChangesAsync();
                }
                catch { }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(category.CategoryId))
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
        return View(category);
    }

    // GET: CATEGORYS/Delete/5
    public async Task<IActionResult> Delete(int?id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(m => m.CategoryId == id);
        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    // POST: CATEGORYS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
        }

        await _context.SaveChangesAsync();
        // Audit: Category Deleted (best-effort)
        try
        {
            var audit = new AuditLog
            {
                UserId = HttpContext.Session.GetInt32("UserID"),
                TableName = "Categories",
                RecordId = category.CategoryId,
                ActionType = "Category Deleted",
                OldValues = $"CategoryName={category.CategoryName};Description={category.Description}",
                NewValues = null,
                ActionDate = DateTime.Now,
                Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }
        catch { }
        return RedirectToAction(nameof(Index));
    }

    private bool CategoryExists(int? id)
    {
        return _context.Categories.Any(e => e.CategoryId == id);
    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
[SessionAuthorize]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.AuditService _auditService;

    public CategoriesController(ApplicationDbContext context, AutoRepairERD.Services.AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
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

            // Audit log
            await _auditService.LogCreateAsync("Categories", category.CategoryId, category.CategoryName);

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
                _context.Update(category);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogUpdateAsync("Categories", category.CategoryId, null, category.CategoryName);
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
            var categoryName = category.CategoryName;
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogDeleteAsync("Categories", (int)id, categoryName);
        }
        else
        {
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool CategoryExists(int? id)
    {
        return _context.Categories.Any(e => e.CategoryId == id);
    }
}

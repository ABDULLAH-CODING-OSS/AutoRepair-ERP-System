
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;

[SessionAuthorize]
public class PartsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.NotificationService _notificationService;

    public PartsController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    // GET: PARTS
    public async Task<IActionResult> Index()
    {
        return View(await _context.Parts
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .ToListAsync());
    }

    // GET: PARTS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var part = await _context.Parts
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(m => m.PartId == id);
        if (part == null)
        {
            return NotFound();
        }

        return View(part);
    }

    // GET: PARTS/Create
    //public IActionResult Create()
    //{
    //    return View();
    //}
    public IActionResult Create()
    {
        ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
        ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "CompanyName");
        return View();
    }

    // POST: PARTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CategoryId,SupplierId,Sku,PartName,Description,CostPrice,SalePrice,CurrentStock,ReorderLevel,Unit,RackLocation,IsActive")] Part part)
    {
        ModelState.Remove("Category");
        ModelState.Remove("Supplier");
        ModelState.Remove("JobPartItems");
        ModelState.Remove("LowStockAlerts");
        ModelState.Remove("PurchaseOrderItems");
        ModelState.Remove("StockTransactions");
        if (ModelState.IsValid)
        {
            _context.Add(part);
            try
            {
                await _context.SaveChangesAsync();
                // Batch 3: NEW PART CREATED - notify Inventory Manager and Admin
                try
                {
                    var invRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Inventory Manager");
                    var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                    var title = "New Inventory Item";
                    var message = $"New inventory item {part.PartName} has been added.";
                    if (invRole != null)
                        await _notificationService.CreateForRoleAsync(invRole.RoleId, "Inventory", title, message);
                    if (adminRole != null)
                        await _notificationService.CreateForRoleAsync(adminRole.RoleId, "Inventory", title, message);
                }
                catch { }
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                // Friendly error for unique constraint or duplicate key failures
                var msg = "Unable to save changes. A part with the same identifier or SKU may already exist.";
                if (dbEx.InnerException?.Message != null && dbEx.InnerException.Message.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    msg = "A part with the same identifier or SKU already exists.";
                }
                ModelState.AddModelError(string.Empty, msg);
            }
        }

        // repopulate selects when returning view on validation errors or DB errors
        ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", part.CategoryId);
        ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "CompanyName", part.SupplierId);
        return View(part);
    }

    // GET: PARTS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var part = await _context.Parts.FindAsync(id);
        if (part == null)
        {
            return NotFound();
        }
        ViewData["CategoryId"] =
    new SelectList(_context.Categories,
                   "CategoryId",
                   "CategoryName",
                   part.CategoryId);

        ViewData["SupplierId"] =
            new SelectList(_context.Suppliers,
                           "SupplierId",
                           "CompanyName",
                           part.SupplierId);
        return View(part);
    }

    // POST: PARTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("PartId,CategoryId,SupplierId,Sku,PartName,Description,CostPrice,SalePrice,CurrentStock,ReorderLevel,Unit,RackLocation,IsActive,Category,JobPartItems,LowStockAlerts,PurchaseOrderItems,StockTransactions,Supplier")] Part part)
    {
        if (id != part.PartId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(part);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PartExists(part.PartId))
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
        ViewData["CategoryId"] =
    new SelectList(_context.Categories,
                   "CategoryId",
                   "CategoryName",
                   part.CategoryId);

        ViewData["SupplierId"] =
            new SelectList(_context.Suppliers,
                           "SupplierId",
                           "CompanyName",
                           part.SupplierId);
        return View(part);
    }

    // GET: PARTS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var part = await _context.Parts
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(m => m.PartId == id);
        if (part == null)
        {
            return NotFound();
        }

        return View(part);
    }

    // POST: PARTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var part = await _context.Parts.FindAsync(id);
        if (part != null)
        {
            _context.Parts.Remove(part);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool PartExists(int? id)
    {
        return _context.Parts.Any(e => e.PartId == id);
    }
}

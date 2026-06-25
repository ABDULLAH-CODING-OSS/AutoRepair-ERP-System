
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
    private readonly AutoRepairERD.Services.AuditService _auditService;

    public PartsController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notificationService, AutoRepairERD.Services.AuditService auditService)
    {
        _context = context;
        _notificationService = notificationService;
        _auditService = auditService;
    }

    // GET: PARTS
    public async Task<IActionResult> Index(string q)
    {
        var query = _context.Parts
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p => (p.PartName ?? "").Contains(q) || (p.Sku ?? "").Contains(q) || (p.Supplier != null && (p.Supplier.SupplierName ?? "").Contains(q)) || (p.Category != null && (p.Category.CategoryName ?? "").Contains(q)));
            ViewBag.SearchQuery = q;
        }

        return View(await query.ToListAsync());
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
        var cats = new SelectList(_context.Categories, "CategoryId", "CategoryName");
        var sups = new SelectList(_context.Suppliers, "SupplierId", "CompanyName");
        ViewData["CategoryId"] = cats;
        ViewData["SupplierId"] = sups;
        ViewBag.Categories = cats;
        ViewBag.Suppliers = sups;
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
        // Require Category and Supplier to be selected
        if (!part.CategoryId.HasValue)
        {
            ModelState.AddModelError("CategoryId", "Category is required.");
        }
        if (!part.SupplierId.HasValue)
        {
            ModelState.AddModelError("SupplierId", "Supplier is required.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(part);
            try
            {
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogCreateAsync("Parts", part.PartId, part.PartName);

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
        var cats = new SelectList(_context.Categories, "CategoryId", "CategoryName", part.CategoryId);
        var sups = new SelectList(_context.Suppliers, "SupplierId", "CompanyName", part.SupplierId);
        ViewData["CategoryId"] = cats;
        ViewData["SupplierId"] = sups;
        ViewBag.Categories = cats;
        ViewBag.Suppliers = sups;
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
        var cats = new SelectList(_context.Categories, "CategoryId", "CategoryName", part.CategoryId);
        var sups = new SelectList(_context.Suppliers, "SupplierId", "CompanyName", part.SupplierId);
        ViewData["CategoryId"] = cats;
        ViewData["SupplierId"] = sups;
        ViewBag.Categories = cats;
        ViewBag.Suppliers = sups;
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

        // Require Category and Supplier to be selected
        if (!part.CategoryId.HasValue)
        {
            ModelState.AddModelError("CategoryId", "Category is required.");
        }
        if (!part.SupplierId.HasValue)
        {
            ModelState.AddModelError("SupplierId", "Supplier is required.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(part);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogUpdateAsync("Parts", part.PartId, null, part.PartName);
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
        var cats = new SelectList(_context.Categories, "CategoryId", "CategoryName", part.CategoryId);
        var sups = new SelectList(_context.Suppliers, "SupplierId", "CompanyName", part.SupplierId);
        ViewData["CategoryId"] = cats;
        ViewData["SupplierId"] = sups;
        ViewBag.Categories = cats;
        ViewBag.Suppliers = sups;
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
        var part = await _context.Parts
            .Include(p => p.JobPartItems)
            .Include(p => p.PurchaseOrderItems)
            .Include(p => p.StockTransactions)
            .Include(p => p.LowStockAlerts)
            .FirstOrDefaultAsync(p => p.PartId == id);
        if (part == null)
        {
            return NotFound();
        }

        // Prevent deletion if related records exist
        var hasJobs = part.JobPartItems != null && part.JobPartItems.Any();
        var hasPoItems = part.PurchaseOrderItems != null && part.PurchaseOrderItems.Any();
        var hasStockTx = part.StockTransactions != null && part.StockTransactions.Any();
        var hasAlerts = part.LowStockAlerts != null && part.LowStockAlerts.Any();

        if (hasJobs || hasPoItems || hasStockTx || hasAlerts)
        {
            // Friendly error message explaining why delete failed
            ModelState.AddModelError(string.Empty, "Cannot delete this part because it is referenced by existing records (jobs, purchase orders, stock transactions or alerts). Deactivate it instead.");
            return View(part);
        }

        try
        {
            var partName = part.PartName;
            _context.Parts.Remove(part);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogDeleteAsync("Parts", (int)id, partName);

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Unable to delete part due to database constraints. Deactivate the part or remove related records first.");
            return View(part);
        }
    }

    private bool PartExists(int? id)
    {
        return _context.Parts.Any(e => e.PartId == id);
    }
}

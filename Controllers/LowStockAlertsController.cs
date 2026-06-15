
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Services;
using AutoRepairERD.Filters;

[RoleAuthorize("Owner","Admin","Inventory Manager")]
public class LowStockAlertsController : Controller
{
    private readonly ApplicationDbContext _context;

    public LowStockAlertsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: LOWSTOCKALERTS
    public async Task<IActionResult> Index()
    {
        // Ensure alerts are synchronized before showing
        LowStockAlertManager.SyncAll(_context);
        await _context.SaveChangesAsync();

        var alerts = await _context.LowStockAlerts
            .Include(a => a.Part)
            .ThenInclude(p => p.Category)
            .OrderByDescending(a => a.AlertDate)
            .ToListAsync();

        return View(alerts);
    }

    // GET: LOWSTOCKALERTS/Details/5
    public async Task<IActionResult> Details(int? alertid)
    {
        if (alertid == null)
        {
            return NotFound();
        }

        var lowstockalert = await _context.LowStockAlerts
            .Include(a => a.Part)
            .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(m => m.AlertId == alertid);
        if (lowstockalert == null)
        {
            return NotFound();
        }

        return View(lowstockalert);
    }

    // GET: LOWSTOCKALERTS/Create
    // Manual create disabled: alerts are generated automatically from parts
    public IActionResult Create()
    {
        return Forbid();
    }

    // POST: LOWSTOCKALERTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([Bind("AlertId,PartId,CurrentQuantity,ReorderLevel,AlertDate,Status,Part")] LowStockAlert lowstockalert)
    {
        // Disallow manual creation
        return Forbid();
    }

    // GET: LOWSTOCKALERTS/Edit/5
    public async Task<IActionResult> Edit(int? alertid)
    {
        if (alertid == null)
        {
            return NotFound();
        }

        var lowstockalert = await _context.LowStockAlerts
            .Include(a => a.Part)
            .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(a => a.AlertId == alertid);
        if (lowstockalert == null)
        {
            return NotFound();
        }
        // Refresh snapshot values from live part to show accurate current stock in the edit form
        if (lowstockalert.Part != null)
        {
            lowstockalert.CurrentQuantity = lowstockalert.Part.CurrentStock;
            lowstockalert.ReorderLevel = lowstockalert.Part.ReorderLevel;
        }
        return View(lowstockalert);
    }

    // POST: LOWSTOCKALERTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? alertid, [Bind("AlertId,Status")] LowStockAlert lowstockalert)
    {
        if (alertid != lowstockalert.AlertId)
        {
            return NotFound();
        }
        // Only allow updating Status and Notes (we store Notes in Status column if not present in model)
        var existing = await _context.LowStockAlerts.FindAsync(alertid);
        if (existing == null) return NotFound();

        // Protective business rules:
        // - If attempting to set Active from Resolved, disallow (cannot reopen resolved alert via UI)
        if (existing.Status == "Resolved" && lowstockalert.Status == "Active")
        {
            TempData["Error"] = "Cannot mark a resolved alert as Active. Add a stock movement to trigger a new alert instead.";
            // reload details for view and show message
            var loaded = await _context.LowStockAlerts
                .Include(a => a.Part)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(a => a.AlertId == alertid);
            if (loaded != null)
            {
                // refresh live snapshot
                loaded.CurrentQuantity = loaded.Part?.CurrentStock;
                loaded.ReorderLevel = loaded.Part?.ReorderLevel;
            }
            return View(loaded);
        }

        // Allow only Status update; preserve other fields
        existing.Status = lowstockalert.Status;
        existing.AlertDate = DateTime.Now;

        try
        {
            _context.Update(existing);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!LowStockAlertExists(existing.AlertId)) return NotFound();
            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: LOWSTOCKALERTS/Delete/5
    public async Task<IActionResult> Delete(int? alertid)
    {
        if (alertid == null)
        {
            return NotFound();
        }

        var lowstockalert = await _context.LowStockAlerts
            .Include(a => a.Part)
            .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(m => m.AlertId == alertid);
        if (lowstockalert == null)
        {
            return NotFound();
        }

        return View(lowstockalert);
    }

    // POST: LOWSTOCKALERTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? alertid)
    {
        var lowstockalert = await _context.LowStockAlerts.FindAsync(alertid);
        if (lowstockalert != null)
        {
            _context.LowStockAlerts.Remove(lowstockalert);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool LowStockAlertExists(int? alertid)
    {
        return _context.LowStockAlerts.Any(e => e.AlertId == alertid);
    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

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
        return View(await _context.LowStockAlerts.ToListAsync());
    }

    // GET: LOWSTOCKALERTS/Details/5
    public async Task<IActionResult> Details(int? alertid)
    {
        if (alertid == null)
        {
            return NotFound();
        }

        var lowstockalert = await _context.LowStockAlerts
            .FirstOrDefaultAsync(m => m.AlertId == alertid);
        if (lowstockalert == null)
        {
            return NotFound();
        }

        return View(lowstockalert);
    }

    // GET: LOWSTOCKALERTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: LOWSTOCKALERTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AlertId,PartId,CurrentQuantity,ReorderLevel,AlertDate,Status,Part")] LowStockAlert lowstockalert)
    {
        if (ModelState.IsValid)
        {
            _context.Add(lowstockalert);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(lowstockalert);
    }

    // GET: LOWSTOCKALERTS/Edit/5
    public async Task<IActionResult> Edit(int? alertid)
    {
        if (alertid == null)
        {
            return NotFound();
        }

        var lowstockalert = await _context.LowStockAlerts.FindAsync(alertid);
        if (lowstockalert == null)
        {
            return NotFound();
        }
        return View(lowstockalert);
    }

    // POST: LOWSTOCKALERTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? alertid, [Bind("AlertId,PartId,CurrentQuantity,ReorderLevel,AlertDate,Status,Part")] LowStockAlert lowstockalert)
    {
        if (alertid != lowstockalert.AlertId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(lowstockalert);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LowStockAlertExists(lowstockalert.AlertId))
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
        return View(lowstockalert);
    }

    // GET: LOWSTOCKALERTS/Delete/5
    public async Task<IActionResult> Delete(int? alertid)
    {
        if (alertid == null)
        {
            return NotFound();
        }

        var lowstockalert = await _context.LowStockAlerts
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

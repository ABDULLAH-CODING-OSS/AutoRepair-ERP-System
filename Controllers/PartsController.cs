
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

public class PartsController : Controller
{
    private readonly ApplicationDbContext _context;

    public PartsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: PARTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Parts.ToListAsync());
    }

    // GET: PARTS/Details/5
    public async Task<IActionResult> Details(int? partid)
    {
        if (partid == null)
        {
            return NotFound();
        }

        var part = await _context.Parts
            .FirstOrDefaultAsync(m => m.PartId == partid);
        if (part == null)
        {
            return NotFound();
        }

        return View(part);
    }

    // GET: PARTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: PARTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PartId,CategoryId,SupplierId,Sku,PartName,Description,CostPrice,SalePrice,CurrentStock,ReorderLevel,Unit,RackLocation,IsActive,Category,JobPartItems,LowStockAlerts,PurchaseOrderItems,StockTransactions,Supplier")] Part part)
    {
        if (ModelState.IsValid)
        {
            _context.Add(part);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(part);
    }

    // GET: PARTS/Edit/5
    public async Task<IActionResult> Edit(int? partid)
    {
        if (partid == null)
        {
            return NotFound();
        }

        var part = await _context.Parts.FindAsync(partid);
        if (part == null)
        {
            return NotFound();
        }
        return View(part);
    }

    // POST: PARTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? partid, [Bind("PartId,CategoryId,SupplierId,Sku,PartName,Description,CostPrice,SalePrice,CurrentStock,ReorderLevel,Unit,RackLocation,IsActive,Category,JobPartItems,LowStockAlerts,PurchaseOrderItems,StockTransactions,Supplier")] Part part)
    {
        if (partid != part.PartId)
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
        return View(part);
    }

    // GET: PARTS/Delete/5
    public async Task<IActionResult> Delete(int? partid)
    {
        if (partid == null)
        {
            return NotFound();
        }

        var part = await _context.Parts
            .FirstOrDefaultAsync(m => m.PartId == partid);
        if (part == null)
        {
            return NotFound();
        }

        return View(part);
    }

    // POST: PARTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? partid)
    {
        var part = await _context.Parts.FindAsync(partid);
        if (part != null)
        {
            _context.Parts.Remove(part);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool PartExists(int? partid)
    {
        return _context.Parts.Any(e => e.PartId == partid);
    }
}

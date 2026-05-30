
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

public class PurchaseOrderItemsController : Controller
{
    private readonly ApplicationDbContext _context;

    public PurchaseOrderItemsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: PURCHASEORDERITEMS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.PurchaseOrderItems.ToListAsync());
    }

    // GET: PURCHASEORDERITEMS/Details/5
    public async Task<IActionResult> Details(int? poitemid)
    {
        if (poitemid == null)
        {
            return NotFound();
        }

        var purchaseorderitem = await _context.PurchaseOrderItems
            .FirstOrDefaultAsync(m => m.PoitemId == poitemid);
        if (purchaseorderitem == null)
        {
            return NotFound();
        }

        return View(purchaseorderitem);
    }

    // GET: PURCHASEORDERITEMS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: PURCHASEORDERITEMS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PoitemId,PurchaseOrderId,PartId,Quantity,UnitCost,TotalCost,Part,PurchaseOrder")] PurchaseOrderItem purchaseorderitem)
    {
        if (ModelState.IsValid)
        {
            _context.Add(purchaseorderitem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(purchaseorderitem);
    }

    // GET: PURCHASEORDERITEMS/Edit/5
    public async Task<IActionResult> Edit(int? poitemid)
    {
        if (poitemid == null)
        {
            return NotFound();
        }

        var purchaseorderitem = await _context.PurchaseOrderItems.FindAsync(poitemid);
        if (purchaseorderitem == null)
        {
            return NotFound();
        }
        return View(purchaseorderitem);
    }

    // POST: PURCHASEORDERITEMS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? poitemid, [Bind("PoitemId,PurchaseOrderId,PartId,Quantity,UnitCost,TotalCost,Part,PurchaseOrder")] PurchaseOrderItem purchaseorderitem)
    {
        if (poitemid != purchaseorderitem.PoitemId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(purchaseorderitem);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PurchaseOrderItemExists(purchaseorderitem.PoitemId))
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
        return View(purchaseorderitem);
    }

    // GET: PURCHASEORDERITEMS/Delete/5
    public async Task<IActionResult> Delete(int? poitemid)
    {
        if (poitemid == null)
        {
            return NotFound();
        }

        var purchaseorderitem = await _context.PurchaseOrderItems
            .FirstOrDefaultAsync(m => m.PoitemId == poitemid);
        if (purchaseorderitem == null)
        {
            return NotFound();
        }

        return View(purchaseorderitem);
    }

    // POST: PURCHASEORDERITEMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? poitemid)
    {
        var purchaseorderitem = await _context.PurchaseOrderItems.FindAsync(poitemid);
        if (purchaseorderitem != null)
        {
            _context.PurchaseOrderItems.Remove(purchaseorderitem);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool PurchaseOrderItemExists(int? poitemid)
    {
        return _context.PurchaseOrderItems.Any(e => e.PoitemId == poitemid);
    }
}

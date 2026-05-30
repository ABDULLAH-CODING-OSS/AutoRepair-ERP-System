
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

public class PurchaseOrdersController : Controller
{
    private readonly ApplicationDbContext _context;

    public PurchaseOrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: PURCHASEORDERS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.PurchaseOrders.ToListAsync());
    }

    // GET: PURCHASEORDERS/Details/5
    public async Task<IActionResult> Details(int? purchaseorderid)
    {
        if (purchaseorderid == null)
        {
            return NotFound();
        }

        var purchaseorder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(m => m.PurchaseOrderId == purchaseorderid);
        if (purchaseorder == null)
        {
            return NotFound();
        }

        return View(purchaseorder);
    }

    // GET: PURCHASEORDERS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: PURCHASEORDERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PurchaseOrderId,SupplierId,CreatedByUserId,OrderDate,ExpectedDeliveryDate,Status,TotalAmount,CreatedByUser,PurchaseOrderItems,Supplier")] PurchaseOrder purchaseorder)
    {
        if (ModelState.IsValid)
        {
            _context.Add(purchaseorder);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(purchaseorder);
    }

    // GET: PURCHASEORDERS/Edit/5
    public async Task<IActionResult> Edit(int? purchaseorderid)
    {
        if (purchaseorderid == null)
        {
            return NotFound();
        }

        var purchaseorder = await _context.PurchaseOrders.FindAsync(purchaseorderid);
        if (purchaseorder == null)
        {
            return NotFound();
        }
        return View(purchaseorder);
    }

    // POST: PURCHASEORDERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? purchaseorderid, [Bind("PurchaseOrderId,SupplierId,CreatedByUserId,OrderDate,ExpectedDeliveryDate,Status,TotalAmount,CreatedByUser,PurchaseOrderItems,Supplier")] PurchaseOrder purchaseorder)
    {
        if (purchaseorderid != purchaseorder.PurchaseOrderId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(purchaseorder);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PurchaseOrderExists(purchaseorder.PurchaseOrderId))
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
        return View(purchaseorder);
    }

    // GET: PURCHASEORDERS/Delete/5
    public async Task<IActionResult> Delete(int? purchaseorderid)
    {
        if (purchaseorderid == null)
        {
            return NotFound();
        }

        var purchaseorder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(m => m.PurchaseOrderId == purchaseorderid);
        if (purchaseorder == null)
        {
            return NotFound();
        }

        return View(purchaseorder);
    }

    // POST: PURCHASEORDERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? purchaseorderid)
    {
        var purchaseorder = await _context.PurchaseOrders.FindAsync(purchaseorderid);
        if (purchaseorder != null)
        {
            _context.PurchaseOrders.Remove(purchaseorder);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool PurchaseOrderExists(int? purchaseorderid)
    {
        return _context.PurchaseOrders.Any(e => e.PurchaseOrderId == purchaseorderid);
    }
}

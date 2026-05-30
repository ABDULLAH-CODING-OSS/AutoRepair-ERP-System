
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

public class SuppliersController : Controller
{
    private readonly ApplicationDbContext _context;

    public SuppliersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: SUPPLIERS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Suppliers.ToListAsync());
    }

    // GET: SUPPLIERS/Details/5
    public async Task<IActionResult> Details(int? supplierid)
    {
        if (supplierid == null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(m => m.SupplierId == supplierid);
        if (supplier == null)
        {
            return NotFound();
        }

        return View(supplier);
    }

    // GET: SUPPLIERS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: SUPPLIERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SupplierId,CompanyName,ContactPerson,Phone,Email,Address,Parts,PurchaseOrders")] Supplier supplier)
    {
        if (ModelState.IsValid)
        {
            _context.Add(supplier);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(supplier);
    }

    // GET: SUPPLIERS/Edit/5
    public async Task<IActionResult> Edit(int? supplierid)
    {
        if (supplierid == null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers.FindAsync(supplierid);
        if (supplier == null)
        {
            return NotFound();
        }
        return View(supplier);
    }

    // POST: SUPPLIERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? supplierid, [Bind("SupplierId,CompanyName,ContactPerson,Phone,Email,Address,Parts,PurchaseOrders")] Supplier supplier)
    {
        if (supplierid != supplier.SupplierId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(supplier);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SupplierExists(supplier.SupplierId))
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
        return View(supplier);
    }

    // GET: SUPPLIERS/Delete/5
    public async Task<IActionResult> Delete(int? supplierid)
    {
        if (supplierid == null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(m => m.SupplierId == supplierid);
        if (supplier == null)
        {
            return NotFound();
        }

        return View(supplier);
    }

    // POST: SUPPLIERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? supplierid)
    {
        var supplier = await _context.Suppliers.FindAsync(supplierid);
        if (supplier != null)
        {
            _context.Suppliers.Remove(supplier);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool SupplierExists(int? supplierid)
    {
        return _context.Suppliers.Any(e => e.SupplierId == supplierid);
    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;

[SessionAuthorize]
public class SuppliersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.NotificationService _notificationService;
    private readonly AutoRepairERD.Services.AuditService _auditService;

    public SuppliersController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notificationService, AutoRepairERD.Services.AuditService auditService)
    {
        _context = context;
        _notificationService = notificationService;
        _auditService = auditService;
    }

    // GET: SUPPLIERS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Suppliers.ToListAsync());
    }

    // GET: SUPPLIERS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(m => m.SupplierId == id);
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
            // Email required + format + uniqueness check
            if (string.IsNullOrWhiteSpace(supplier.Email))
            {
                ModelState.AddModelError("Email", "Email is required.");
                return View(supplier);
            }
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(supplier.Email))
            {
                ModelState.AddModelError("Email", "Invalid email format.");
                return View(supplier);
            }
            if (await _context.Suppliers.AnyAsync(s => s.Email == supplier.Email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(supplier);
            }

            _context.Add(supplier);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogCreateAsync("Suppliers", supplier.SupplierId, supplier.CompanyName);

            // Batch 3: NEW SUPPLIER CREATED - notify Inventory Manager and Admin
            try
            {
                var invRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Inventory Manager");
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                var title = "New Supplier";
                var message = $"New supplier {supplier.CompanyName} has been added.";
                if (invRole != null)
                    await _notificationService.CreateForRoleAsync(invRole.RoleId, "Inventory", title, message);
                if (adminRole != null)
                    await _notificationService.CreateForRoleAsync(adminRole.RoleId, "Inventory", title, message);
            }
            catch { }
            return RedirectToAction(nameof(Index));
        }
        return View(supplier);
    }

    // GET: SUPPLIERS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers.FindAsync(id);
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
    public async Task<IActionResult> Edit(int? id, [Bind("SupplierId,CompanyName,ContactPerson,Phone,Email,Address,Parts,PurchaseOrders")] Supplier supplier)
    {
        if (id != supplier.SupplierId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Email required + format + uniqueness check excluding current supplier
            if (string.IsNullOrWhiteSpace(supplier.Email))
            {
                ModelState.AddModelError("Email", "Email is required.");
                return View(supplier);
            }
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(supplier.Email))
            {
                ModelState.AddModelError("Email", "Invalid email format.");
                return View(supplier);
            }
            if (await _context.Suppliers.AnyAsync(s => s.Email == supplier.Email && s.SupplierId != supplier.SupplierId))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(supplier);
            }

try
            {
                _context.Update(supplier);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogUpdateAsync("Suppliers", supplier.SupplierId, null, supplier.CompanyName);
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
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(m => m.SupplierId == id);
        if (supplier == null)
        {
            return NotFound();
        }

        return View(supplier);
    }

    // POST: SUPPLIERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier != null)
        {
            var supplierName = supplier.CompanyName;
            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogDeleteAsync("Suppliers", (int)id, supplierName);
        }
        else
        {
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool SupplierExists(int? id)
    {
        return _context.Suppliers.Any(e => e.SupplierId == id);
    }
}

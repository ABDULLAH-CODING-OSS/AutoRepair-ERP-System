
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

    public SuppliersController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
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
            _context.Add(supplier);
            await _context.SaveChangesAsync();
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
            // Audit: Supplier Created (best-effort)
            try
            {
                var audit = new AuditLog
                {
                    UserId = HttpContext.Session.GetInt32("UserID"),
                    TableName = "Suppliers",
                    RecordId = supplier.SupplierId,
                    ActionType = "Supplier Created",
                    OldValues = null,
                    NewValues = $"Supplier={supplier.CompanyName};Contact={supplier.ContactPerson};Phone={supplier.Phone}",
                    ActionDate = DateTime.Now,
                    Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.AuditLogs.Add(audit);
                await _context.SaveChangesAsync();
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
            try
            {
                var existing = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.SupplierId == supplier.SupplierId);
                _context.Update(supplier);
                await _context.SaveChangesAsync();
                // Audit: Supplier Updated (best-effort)
                try
                {
                    var audit = new AuditLog
                    {
                        UserId = HttpContext.Session.GetInt32("UserID"),
                        TableName = "Suppliers",
                        RecordId = supplier.SupplierId,
                        ActionType = "Supplier Updated",
                        OldValues = existing != null ? $"Supplier={existing.CompanyName};Contact={existing.ContactPerson};Phone={existing.Phone}" : null,
                        NewValues = $"Supplier={supplier.CompanyName};Contact={supplier.ContactPerson};Phone={supplier.Phone}",
                        ActionDate = DateTime.Now,
                        Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    };
                    _context.AuditLogs.Add(audit);
                    await _context.SaveChangesAsync();
                }
                catch { }
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
            _context.Suppliers.Remove(supplier);
        }

        await _context.SaveChangesAsync();
        // Audit: Supplier Deleted (best-effort)
        try
        {
            var audit = new AuditLog
            {
                UserId = HttpContext.Session.GetInt32("UserID"),
                TableName = "Suppliers",
                RecordId = supplier.SupplierId,
                ActionType = "Supplier Deleted",
                OldValues = $"Supplier={supplier.CompanyName};Contact={supplier.ContactPerson};Phone={supplier.Phone}",
                NewValues = null,
                ActionDate = DateTime.Now,
                Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }
        catch { }

        return RedirectToAction(nameof(Index));
    }

    private bool SupplierExists(int? id)
    {
        return _context.Suppliers.Any(e => e.SupplierId == id);
    }
}

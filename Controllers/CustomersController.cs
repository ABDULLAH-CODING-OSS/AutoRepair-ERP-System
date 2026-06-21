
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;

[SessionAuthorize]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.NotificationService _notificationService;

    public CustomersController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    // GET: CUSTOMERS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Customers.ToListAsync());
    }

    // GET: CUSTOMERS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(m => m.CustomerId == id);
        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // GET: CUSTOMERS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: CUSTOMERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
     [Bind("FirstName,LastName,Phone,Email,Address,City")]
    Customer customer)
    {
        if (ModelState.IsValid)
        {
            customer.CreatedByUserId =
                HttpContext.Session.GetInt32("UserID");

            customer.CreatedAt = DateTime.Now;

            _context.Add(customer);
            await _context.SaveChangesAsync();

            // Batch 3: NEW CUSTOMER CREATED - notify Admin
            try
            {
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                if (adminRole != null)
                {
                    var title = "New Customer";
                    var name = customer.FirstName + (string.IsNullOrEmpty(customer.LastName) ? "" : " " + customer.LastName);
                    var message = $"New customer {name} has been registered.";
                    await _notificationService.CreateForRoleAsync(adminRole.RoleId, "Customer", title, message);
                }
            }
            catch { }

            // Audit: Customer Created (best-effort)
            try
            {
                var name = customer.FirstName + (string.IsNullOrEmpty(customer.LastName) ? "" : " " + customer.LastName);
                var audit = new AuditLog
                {
                    UserId = HttpContext.Session.GetInt32("UserID"),
                    TableName = "Customers",
                    RecordId = customer.CustomerId,
                    ActionType = "Customer Created",
                    OldValues = null,
                    NewValues = $"Customer={name};Phone={customer.Phone};Email={customer.Email}",
                    ActionDate = DateTime.Now,
                    Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.AuditLogs.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch { }

            return RedirectToAction(nameof(Index));
        }

        return View(customer);
    }

    // GET: CUSTOMERS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }
        return View(customer);
    }

    // POST: CUSTOMERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("CustomerId,CreatedByUserId,FirstName,LastName,Phone,Email,Address,City,CreatedAt,CreatedByUser,JobOrders,Vehicles")] Customer customer)
    {
        if (id != customer.CustomerId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
                _context.Update(customer);
                await _context.SaveChangesAsync();
                // Audit: Customer Updated (best-effort)
                try
                {
                    var oldName = existing != null ? existing.FirstName + (string.IsNullOrEmpty(existing.LastName)?"":" "+existing.LastName) : null;
                    var newName = customer.FirstName + (string.IsNullOrEmpty(customer.LastName)?"":" "+customer.LastName);
                    var audit = new AuditLog
                    {
                        UserId = HttpContext.Session.GetInt32("UserID"),
                        TableName = "Customers",
                        RecordId = customer.CustomerId,
                        ActionType = "Customer Updated",
                        OldValues = existing != null ? $"Customer={oldName};Phone={existing.Phone};Email={existing.Email}" : null,
                        NewValues = $"Customer={newName};Phone={customer.Phone};Email={customer.Email}",
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
                if (!CustomerExists(customer.CustomerId))
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
        return View(customer);
    }

    // GET: CUSTOMERS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(m => m.CustomerId == id);
        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // POST: CUSTOMERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
        }

        await _context.SaveChangesAsync();
        // Audit: Customer Deleted (best-effort)
        try
        {
            var name = customer.FirstName + (string.IsNullOrEmpty(customer.LastName) ? "" : " " + customer.LastName);
            var audit = new AuditLog
            {
                UserId = HttpContext.Session.GetInt32("UserID"),
                TableName = "Customers",
                RecordId = customer.CustomerId,
                ActionType = "Customer Deleted",
                OldValues = $"Customer={name};Phone={customer.Phone};Email={customer.Email}",
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

    private bool CustomerExists(int? id)
    {
        return _context.Customers.Any(e => e.CustomerId == id);
    }
}

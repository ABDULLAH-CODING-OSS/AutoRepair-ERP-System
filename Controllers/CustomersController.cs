
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;

[SessionAuthorize]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.NotificationService _notificationService;
    private readonly AutoRepairERD.Services.AuditService _auditService;

    public CustomersController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notificationService, AutoRepairERD.Services.AuditService auditService)
    {
        _context = context;
        _notificationService = notificationService;
        _auditService = auditService;
    }

    // GET: CUSTOMERS
    public async Task<IActionResult> Index(string q)
    {
        var query = _context.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(c => (c.FirstName ?? "").Contains(q) || (c.LastName ?? "").Contains(q) || (c.Phone ?? "").Contains(q) || (c.Email ?? "").Contains(q));
            ViewBag.SearchQuery = q;
        }
        var list = await query.ToListAsync();
        return View(list);
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
            // Server-side validation: required + format + uniqueness
            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                ModelState.AddModelError("Email", "Email is required.");
                return View(customer);
            }
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(customer.Email))
            {
                ModelState.AddModelError("Email", "Invalid email format.");
                return View(customer);
            }
            if (await _context.Customers.AnyAsync(c => c.Email == customer.Email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(customer);
            }
            if (!string.IsNullOrWhiteSpace(customer.Phone) && await _context.Customers.AnyAsync(c => c.Phone == customer.Phone))
            {
                ModelState.AddModelError("Phone", "Phone number already exists.");
                return View(customer);
            }

            customer.CreatedByUserId =
                HttpContext.Session.GetInt32("UserID");

            customer.CreatedAt = DateTime.Now;

            _context.Add(customer);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogCreateAsync("Customers", customer.CustomerId, $"{customer.FirstName} {customer.LastName}");

            // Notify relevant roles: Admin, Owner, Service Advisor
            try
            {
                var roleNames = new[] { "Admin", "Owner", "Service Advisor" };
                foreach (var rn in roleNames)
                {
                    var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == rn);
                    if (role != null)
                    {
                        var title = "New Customer";
                        var name = customer.FirstName + (string.IsNullOrEmpty(customer.LastName) ? "" : " " + customer.LastName);
                        var message = $"New customer {name} has been registered.";
                        await _notificationService.CreateForRoleAsync(role.RoleId, "Customer", title, message);
                    }
                }
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
            // Server-side validation for email + uniqueness checks excluding current record
            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                ModelState.AddModelError("Email", "Email is required.");
                return View(customer);
            }
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(customer.Email))
            {
                ModelState.AddModelError("Email", "Invalid email format.");
                return View(customer);
            }
            if (await _context.Customers.AnyAsync(c => c.Email == customer.Email && c.CustomerId != customer.CustomerId))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(customer);
            }
            if (!string.IsNullOrWhiteSpace(customer.Phone) && await _context.Customers.AnyAsync(c => c.Phone == customer.Phone && c.CustomerId != customer.CustomerId))
            {
                ModelState.AddModelError("Phone", "Phone number already exists.");
                return View(customer);
            }

            try
            {
                _context.Update(customer);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogUpdateAsync("Customers", customer.CustomerId, null, $"{customer.FirstName} {customer.LastName}");
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
        var customer = await _context.Customers
            .Include(c => c.Vehicles)
            .Include(c => c.JobOrders)
            .FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer == null)
        {
            return NotFound();
        }

        // Prevent deletion if related records exist
        var hasVehicles = customer.Vehicles != null && customer.Vehicles.Any();
        var hasJobs = customer.JobOrders != null && customer.JobOrders.Any();
        if (hasVehicles || hasJobs)
        {
            ModelState.AddModelError(string.Empty, "Cannot delete this customer because related vehicles or job orders exist. Please remove related records first or deactivate the customer.");
            return View(customer);
        }

        try
        {
            var customerName = $"{customer.FirstName} {customer.LastName}";
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogDeleteAsync("Customers", (int)id, customerName);

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Unable to delete customer due to database constraints. Please remove related records first.");
            return View(customer);
        }
    }

    private bool CustomerExists(int? id)
    {
        return _context.Customers.Any(e => e.CustomerId == id);
    }
}

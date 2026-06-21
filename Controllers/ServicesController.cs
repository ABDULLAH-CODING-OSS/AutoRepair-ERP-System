
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
[SessionAuthorize]

public class ServicesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ServicesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: SERVICES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Services.ToListAsync());
    }

    // GET: SERVICES/Details/5
    public async Task<IActionResult> Details(int?  id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(m => m.ServiceId == id);
        if (service == null)
        {
            return NotFound();
        }

        return View(service);
    }

    // GET: SERVICES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: SERVICES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ServiceId,ServiceName,Description,StandardHours,FixedPrice,IsActive,JobServiceItems")] Service service)
    {
        if (ModelState.IsValid)
        {
            _context.Add(service);
            await _context.SaveChangesAsync();
            // Audit: Service Created (best-effort)
            try
            {
                var audit = new AuditLog
                {
                    UserId = HttpContext.Session.GetInt32("UserID"),
                    TableName = "Services",
                    RecordId = service.ServiceId,
                    ActionType = "Service Created",
                    OldValues = null,
                    NewValues = $"ServiceName={service.ServiceName};FixedPrice={service.FixedPrice}",
                    ActionDate = DateTime.Now,
                    Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.AuditLogs.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch { }
            return RedirectToAction(nameof(Index));
        }
        return View(service);
    }

    // GET: SERVICES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id  == null)
        {
            return NotFound();
        }

        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            return NotFound();
        }
        return View(service);
    }

    // POST: SERVICES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ServiceId,ServiceName,Description,StandardHours,FixedPrice,IsActive,JobServiceItems")] Service service)
    {
        if (id != service.ServiceId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.ServiceId == service.ServiceId);
                _context.Update(service);
                await _context.SaveChangesAsync();
                // Audit: Service Updated (best-effort)
                try
                {
                    var audit = new AuditLog
                    {
                        UserId = HttpContext.Session.GetInt32("UserID"),
                        TableName = "Services",
                        RecordId = service.ServiceId,
                        ActionType = "Service Updated",
                        OldValues = existing != null ? $"ServiceName={existing.ServiceName};FixedPrice={existing.FixedPrice}" : null,
                        NewValues = $"ServiceName={service.ServiceName};FixedPrice={service.FixedPrice}",
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
                if (!ServiceExists(service.ServiceId))
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
        return View(service);
    }

    // GET: SERVICES/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(m => m.ServiceId == id);
        if (service == null)
        {
            return NotFound();
        }

        return View(service);
    }

    // POST: SERVICES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service != null)
        {
            _context.Services.Remove(service);
        }

        await _context.SaveChangesAsync();
        // Audit: Service Deleted (best-effort)
        try
        {
            var audit = new AuditLog
            {
                UserId = HttpContext.Session.GetInt32("UserID"),
                TableName = "Services",
                RecordId = service.ServiceId,
                ActionType = "Service Deleted",
                OldValues = $"ServiceName={service.ServiceName};FixedPrice={service.FixedPrice}",
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

    private bool ServiceExists(int? id)
    {
        return _context.Services.Any(e => e.ServiceId == id );
    }
}

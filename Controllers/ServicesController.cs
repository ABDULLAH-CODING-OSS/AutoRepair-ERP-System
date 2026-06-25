
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
[SessionAuthorize]

public class ServicesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.AuditService _auditService;

    public ServicesController(ApplicationDbContext context, AutoRepairERD.Services.AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET: SERVICES
    public async Task<IActionResult> Index(string q)
    {
        var query = _context.Services.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(s => (s.ServiceName ?? "").Contains(q) || (s.Description ?? "").Contains(q));
            ViewBag.SearchQuery = q;
        }
        return View(await query.ToListAsync());
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

            // Audit log
            await _auditService.LogCreateAsync("Services", service.ServiceId, service.ServiceName);

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
                _context.Update(service);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogUpdateAsync("Services", service.ServiceId, null, service.ServiceName);
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
            var serviceName = service.ServiceName;
            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogDeleteAsync("Services", (int)id, serviceName);
        }
        else
        {
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool ServiceExists(int? id)
    {
        return _context.Services.Any(e => e.ServiceId == id );
    }
}

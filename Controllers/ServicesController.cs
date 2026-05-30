
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

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
    public async Task<IActionResult> Details(int? serviceid)
    {
        if (serviceid == null)
        {
            return NotFound();
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(m => m.ServiceId == serviceid);
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
            return RedirectToAction(nameof(Index));
        }
        return View(service);
    }

    // GET: SERVICES/Edit/5
    public async Task<IActionResult> Edit(int? serviceid)
    {
        if (serviceid == null)
        {
            return NotFound();
        }

        var service = await _context.Services.FindAsync(serviceid);
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
    public async Task<IActionResult> Edit(int? serviceid, [Bind("ServiceId,ServiceName,Description,StandardHours,FixedPrice,IsActive,JobServiceItems")] Service service)
    {
        if (serviceid != service.ServiceId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(service);
                await _context.SaveChangesAsync();
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
    public async Task<IActionResult> Delete(int? serviceid)
    {
        if (serviceid == null)
        {
            return NotFound();
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(m => m.ServiceId == serviceid);
        if (service == null)
        {
            return NotFound();
        }

        return View(service);
    }

    // POST: SERVICES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? serviceid)
    {
        var service = await _context.Services.FindAsync(serviceid);
        if (service != null)
        {
            _context.Services.Remove(service);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ServiceExists(int? serviceid)
    {
        return _context.Services.Any(e => e.ServiceId == serviceid);
    }
}

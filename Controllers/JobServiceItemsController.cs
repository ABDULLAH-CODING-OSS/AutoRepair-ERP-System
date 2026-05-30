
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

public class JobServiceItemsController : Controller
{
    private readonly ApplicationDbContext _context;

    public JobServiceItemsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: JOBSERVICEITEMS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.JobServiceItems.ToListAsync());
    }

    // GET: JOBSERVICEITEMS/Details/5
    public async Task<IActionResult> Details(int? jobserviceitemid)
    {
        if (jobserviceitemid == null)
        {
            return NotFound();
        }

        var jobserviceitem = await _context.JobServiceItems
            .FirstOrDefaultAsync(m => m.JobServiceItemId == jobserviceitemid);
        if (jobserviceitem == null)
        {
            return NotFound();
        }

        return View(jobserviceitem);
    }

    // GET: JOBSERVICEITEMS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: JOBSERVICEITEMS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("JobServiceItemId,JobOrderId,ServiceId,MechanicId,HoursWorked,HourlyRate,ServicePrice,Notes,JobOrder,Mechanic,Service")] JobServiceItem jobserviceitem)
    {
        if (ModelState.IsValid)
        {
            _context.Add(jobserviceitem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(jobserviceitem);
    }

    // GET: JOBSERVICEITEMS/Edit/5
    public async Task<IActionResult> Edit(int? jobserviceitemid)
    {
        if (jobserviceitemid == null)
        {
            return NotFound();
        }

        var jobserviceitem = await _context.JobServiceItems.FindAsync(jobserviceitemid);
        if (jobserviceitem == null)
        {
            return NotFound();
        }
        return View(jobserviceitem);
    }

    // POST: JOBSERVICEITEMS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? jobserviceitemid, [Bind("JobServiceItemId,JobOrderId,ServiceId,MechanicId,HoursWorked,HourlyRate,ServicePrice,Notes,JobOrder,Mechanic,Service")] JobServiceItem jobserviceitem)
    {
        if (jobserviceitemid != jobserviceitem.JobServiceItemId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(jobserviceitem);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JobServiceItemExists(jobserviceitem.JobServiceItemId))
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
        return View(jobserviceitem);
    }

    // GET: JOBSERVICEITEMS/Delete/5
    public async Task<IActionResult> Delete(int? jobserviceitemid)
    {
        if (jobserviceitemid == null)
        {
            return NotFound();
        }

        var jobserviceitem = await _context.JobServiceItems
            .FirstOrDefaultAsync(m => m.JobServiceItemId == jobserviceitemid);
        if (jobserviceitem == null)
        {
            return NotFound();
        }

        return View(jobserviceitem);
    }

    // POST: JOBSERVICEITEMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? jobserviceitemid)
    {
        var jobserviceitem = await _context.JobServiceItems.FindAsync(jobserviceitemid);
        if (jobserviceitem != null)
        {
            _context.JobServiceItems.Remove(jobserviceitem);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool JobServiceItemExists(int? jobserviceitemid)
    {
        return _context.JobServiceItems.Any(e => e.JobServiceItemId == jobserviceitemid);
    }
}

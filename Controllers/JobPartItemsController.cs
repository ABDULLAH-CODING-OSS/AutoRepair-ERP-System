
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

public class JobPartItemsController : Controller
{
    private readonly ApplicationDbContext _context;

    public JobPartItemsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: JOBPARTITEMS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.JobPartItems.ToListAsync());
    }

    // GET: JOBPARTITEMS/Details/5
    public async Task<IActionResult> Details(int? jobpartitemid)
    {
        if (jobpartitemid == null)
        {
            return NotFound();
        }

        var jobpartitem = await _context.JobPartItems
            .FirstOrDefaultAsync(m => m.JobPartItemId == jobpartitemid);
        if (jobpartitem == null)
        {
            return NotFound();
        }

        return View(jobpartitem);
    }

    // GET: JOBPARTITEMS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: JOBPARTITEMS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("JobPartItemId,JobOrderId,PartId,Quantity,UnitPrice,TotalPrice,JobOrder,Part")] JobPartItem jobpartitem)
    {
        if (ModelState.IsValid)
        {
            _context.Add(jobpartitem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(jobpartitem);
    }

    // GET: JOBPARTITEMS/Edit/5
    public async Task<IActionResult> Edit(int? jobpartitemid)
    {
        if (jobpartitemid == null)
        {
            return NotFound();
        }

        var jobpartitem = await _context.JobPartItems.FindAsync(jobpartitemid);
        if (jobpartitem == null)
        {
            return NotFound();
        }
        return View(jobpartitem);
    }

    // POST: JOBPARTITEMS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? jobpartitemid, [Bind("JobPartItemId,JobOrderId,PartId,Quantity,UnitPrice,TotalPrice,JobOrder,Part")] JobPartItem jobpartitem)
    {
        if (jobpartitemid != jobpartitem.JobPartItemId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(jobpartitem);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JobPartItemExists(jobpartitem.JobPartItemId))
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
        return View(jobpartitem);
    }

    // GET: JOBPARTITEMS/Delete/5
    public async Task<IActionResult> Delete(int? jobpartitemid)
    {
        if (jobpartitemid == null)
        {
            return NotFound();
        }

        var jobpartitem = await _context.JobPartItems
            .FirstOrDefaultAsync(m => m.JobPartItemId == jobpartitemid);
        if (jobpartitem == null)
        {
            return NotFound();
        }

        return View(jobpartitem);
    }

    // POST: JOBPARTITEMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? jobpartitemid)
    {
        var jobpartitem = await _context.JobPartItems.FindAsync(jobpartitemid);
        if (jobpartitem != null)
        {
            _context.JobPartItems.Remove(jobpartitem);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool JobPartItemExists(int? jobpartitemid)
    {
        return _context.JobPartItems.Any(e => e.JobPartItemId == jobpartitemid);
    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

public class JobOrdersController : Controller
{
    private readonly ApplicationDbContext _context;

    public JobOrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: JOBORDERS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.JobOrders.ToListAsync());
    }

    // GET: JOBORDERS/Details/5
    public async Task<IActionResult> Details(int? joborderid)
    {
        if (joborderid == null)
        {
            return NotFound();
        }

        var joborder = await _context.JobOrders
            .FirstOrDefaultAsync(m => m.JobOrderId == joborderid);
        if (joborder == null)
        {
            return NotFound();
        }

        return View(joborder);
    }

    // GET: JOBORDERS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: JOBORDERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("JobOrderId,CustomerId,VehicleId,AdvisorId,MechanicId,CreatedByUserId,JobNumber,ComplaintDescription,DiagnosisNotes,EstimatedCompletionDate,StartDate,CompletionDate,Status,EstimatedCost,FinalCost,CreatedAt,Advisor,CreatedByUser,Customer,Invoices,JobPartItems,JobServiceItems,Mechanic,Vehicle")] JobOrder joborder)
    {
        if (ModelState.IsValid)
        {
            _context.Add(joborder);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(joborder);
    }

    // GET: JOBORDERS/Edit/5
    public async Task<IActionResult> Edit(int? joborderid)
    {
        if (joborderid == null)
        {
            return NotFound();
        }

        var joborder = await _context.JobOrders.FindAsync(joborderid);
        if (joborder == null)
        {
            return NotFound();
        }
        return View(joborder);
    }

    // POST: JOBORDERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? joborderid, [Bind("JobOrderId,CustomerId,VehicleId,AdvisorId,MechanicId,CreatedByUserId,JobNumber,ComplaintDescription,DiagnosisNotes,EstimatedCompletionDate,StartDate,CompletionDate,Status,EstimatedCost,FinalCost,CreatedAt,Advisor,CreatedByUser,Customer,Invoices,JobPartItems,JobServiceItems,Mechanic,Vehicle")] JobOrder joborder)
    {
        if (joborderid != joborder.JobOrderId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(joborder);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JobOrderExists(joborder.JobOrderId))
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
        return View(joborder);
    }

    // GET: JOBORDERS/Delete/5
    public async Task<IActionResult> Delete(int? joborderid)
    {
        if (joborderid == null)
        {
            return NotFound();
        }

        var joborder = await _context.JobOrders
            .FirstOrDefaultAsync(m => m.JobOrderId == joborderid);
        if (joborder == null)
        {
            return NotFound();
        }

        return View(joborder);
    }

    // POST: JOBORDERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? joborderid)
    {
        var joborder = await _context.JobOrders.FindAsync(joborderid);
        if (joborder != null)
        {
            _context.JobOrders.Remove(joborder);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool JobOrderExists(int? joborderid)
    {
        return _context.JobOrders.Any(e => e.JobOrderId == joborderid);
    }
}

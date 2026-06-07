
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
[SessionAuthorize]
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
        var items = await _context.JobServiceItems
            .Include(j => j.JobOrder)
            .Include(j => j.Service)
            .Include(j => j.Mechanic)
            .ToListAsync();

        return View(items);
    }

    // GET: JOBSERVICEITEMS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var jobserviceitem = await _context.JobServiceItems
            .Include(j => j.JobOrder)
            .Include(j => j.Service)
            .Include(j => j.Mechanic)
            .FirstOrDefaultAsync(m => m.JobServiceItemId == id);
        if (jobserviceitem == null)
        {
            return NotFound();
        }

        return View(jobserviceitem);
    }

    // GET: JOBSERVICEITEMS/Create
    //public IActionResult Create()
    //{
    //    return View();
    //}
    public IActionResult Create()
    {
        ViewBag.JobOrders = new SelectList(
            _context.JobOrders.Where(j => j.Status != "Completed" && j.Status != "Cancelled"),
            "JobOrderId",
            "JobNumber");

        ViewBag.Services = new SelectList(
            _context.Services,
            "ServiceId",
            "ServiceName");

        ViewBag.Mechanics = new SelectList(
            _context.Employees.Where(e => e.Designation == "Mechanic"),
            "EmployeeId",
            "FirstName");

        return View();
    }

    // POST: JOBSERVICEITEMS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    //public async Task<IActionResult> Create([Bind("JobServiceItemId,JobOrderId,ServiceId,MechanicId,HoursWorked,HourlyRate,ServicePrice,Notes,JobOrder,Mechanic,Service")] JobServiceItem jobserviceitem)
    public async Task<IActionResult> Create([Bind("JobOrderId,ServiceId,MechanicId,HoursWorked,Notes")] JobServiceItem jobserviceitem)
    {
        ModelState.Remove("JobOrder");
        ModelState.Remove("Mechanic");
        ModelState.Remove("Service");
        if (ModelState.IsValid)
        {
            // populate hourly rate from Service and calculate service price
            var service = await _context.Services.FindAsync(jobserviceitem.ServiceId);
            if (service != null)
            {
                // Use service fixed price as hourly rate when HourlyRate field not present on Service
                jobserviceitem.HourlyRate = service.FixedPrice;
            }

            jobserviceitem.ServicePrice = (jobserviceitem.HoursWorked ?? 0) * (jobserviceitem.HourlyRate ?? 0);

            // determine if this is the first service item for the job
            var existingCount = _context.JobServiceItems.Count(j => j.JobOrderId == jobserviceitem.JobOrderId);

            _context.Add(jobserviceitem);
            await _context.SaveChangesAsync();

            // if first service added and job is pending, set to In Progress
            if (existingCount == 0)
            {
                var job = await _context.JobOrders.FindAsync(jobserviceitem.JobOrderId);
                if (job != null && job.Status == "Pending")
                {
                    job.Status = "In Progress";
                    _context.Update(job);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }
        ViewBag.JobOrders = new SelectList(
            _context.JobOrders.Where(j => j.Status != "Completed" && j.Status != "Cancelled"),
            "JobOrderId",
            "JobNumber",
            jobserviceitem.JobOrderId);

        ViewBag.Services = new SelectList(
            _context.Services,
            "ServiceId",
            "ServiceName",
            jobserviceitem.ServiceId);

        ViewBag.Mechanics = new SelectList(
            _context.Employees.Where(e => e.Designation == "Mechanic"),
            "EmployeeId",
            "FirstName",
            jobserviceitem.MechanicId);

        return View(jobserviceitem);
    }

    // GET: JOBSERVICEITEMS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var jobserviceitem = await _context.JobServiceItems
            .Include(j => j.Mechanic)
            .Include(j => j.JobOrder)
            .Include(j => j.Service)
            .FirstOrDefaultAsync(j => j.JobServiceItemId == id);
        if (jobserviceitem == null)
        {
            return NotFound();
        }
        ViewBag.JobOrders = new SelectList(
    _context.JobOrders.Where(j => j.Status != "Completed" && j.Status != "Cancelled"),
    "JobOrderId",
    "JobNumber",
    jobserviceitem.JobOrderId);

        ViewBag.Services = new SelectList(
            _context.Services,
            "ServiceId",
            "ServiceName",
            jobserviceitem.ServiceId);

        ViewBag.Mechanics = new SelectList(
            _context.Employees.Where(e => e.Designation == "Mechanic"),
            "EmployeeId",
            "FirstName",
            jobserviceitem.MechanicId);

        return View(jobserviceitem);
    }

    // POST: JOBSERVICEITEMS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("JobServiceItemId,JobOrderId,ServiceId,MechanicId,HoursWorked,Notes")] JobServiceItem jobserviceitem)
 {

        if (id != jobserviceitem.JobServiceItemId)
        {
            return NotFound();
        }
        ModelState.Remove("JobOrder");
        ModelState.Remove("Mechanic");
        ModelState.Remove("Service");

        if (ModelState.IsValid)
        {
            try
            {
                // ensure hourly rate and service price reflect selected Service and HoursWorked
                var service = await _context.Services.FindAsync(jobserviceitem.ServiceId);
                if (service != null)
                {
                    jobserviceitem.HourlyRate = service.FixedPrice;
                }
                jobserviceitem.ServicePrice = (jobserviceitem.HoursWorked ?? 0) * (jobserviceitem.HourlyRate ?? 0);

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
        ViewBag.JobOrders = new SelectList(
    _context.JobOrders,
    "JobOrderId",
    "JobNumber",
    jobserviceitem.JobOrderId);

        ViewBag.Services = new SelectList(
            _context.Services,
            "ServiceId",
            "ServiceName",
            jobserviceitem.ServiceId);

        ViewBag.Mechanics = new SelectList(
            _context.Employees.Where(e => e.Designation == "Mechanic"),
            "EmployeeId",
            "FirstName",
            jobserviceitem.MechanicId);
        return View(jobserviceitem);
    }

    // GET: JOBSERVICEITEMS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var jobserviceitem = await _context.JobServiceItems
            .Include(j => j.JobOrder)
            .Include(j => j.Service)
            .Include(j => j.Mechanic)
            .FirstOrDefaultAsync(m => m.JobServiceItemId == id);
        if (jobserviceitem == null)
        {
            return NotFound();
        }

        return View(jobserviceitem);
    }

    // POST: JOBSERVICEITEMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var jobserviceitem = await _context.JobServiceItems.FindAsync(id);
        if (jobserviceitem != null)
        {
            _context.JobServiceItems.Remove(jobserviceitem);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool JobServiceItemExists(int? id)
    {
        return _context.JobServiceItems.Any(e => e.JobServiceItemId == id);
    }
}

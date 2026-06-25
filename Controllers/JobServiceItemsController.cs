
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

        // If the default selection (first returned job) already has a mechanic, mark mechanic readonly
        var defaultJob = _context.JobOrders.Where(j => j.Status != "Completed" && j.Status != "Cancelled").OrderBy(j => j.CreatedAt).FirstOrDefault();
        if (defaultJob != null && defaultJob.MechanicId.HasValue)
        {
            ViewBag.ReadonlyMechanic = true;
            var mech = _context.Employees.Find(defaultJob.MechanicId.Value);
            ViewBag.ReadonlyMechanicText = mech != null ? mech.FirstName + " " + mech.LastName : "Assigned";
            ViewBag.DefaultMechanicId = defaultJob.MechanicId;
        }

        ViewBag.Services = new SelectList(
            _context.Services,
            "ServiceId",
            "ServiceName");

        ViewBag.Mechanics = new SelectList(
            _context.Employees.Where(e => e.Designation == "Mechanic" && e.IsActive == true).OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new { e.EmployeeId, Text = e.FirstName + " " + e.LastName + (string.IsNullOrEmpty(e.Phone) ? "" : " (" + e.Phone + ")") }),
            "EmployeeId",
            "Text");

        // If a job was preselected via querystring, detect its mechanic and set readonly flag
        var jobIdStr = HttpContext.Request.Query["jobId"].FirstOrDefault();
        if (int.TryParse(jobIdStr, out var jobId))
        {
            var job = _context.JobOrders.Include(j => j.Mechanic).FirstOrDefault(j => j.JobOrderId == jobId);
            if (job != null && job.MechanicId.HasValue)
            {
                ViewBag.ReadonlyMechanic = true;
                ViewBag.ReadonlyMechanicText = job.Mechanic != null ? job.Mechanic.FirstName + " " + job.Mechanic.LastName : "Assigned";
                ViewBag.DefaultMechanicId = job.MechanicId;
            }
        }

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

            // If the related job has a mechanic assigned, respect it and override the selection
            var relatedJob = await _context.JobOrders.FindAsync(jobserviceitem.JobOrderId);
            if (relatedJob != null && relatedJob.MechanicId.HasValue)
            {
                jobserviceitem.MechanicId = relatedJob.MechanicId;
            }

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
            _context.Employees.Where(e => e.Designation == "Mechanic" && e.IsActive == true).OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new { e.EmployeeId, Text = e.FirstName + " " + e.LastName + (string.IsNullOrEmpty(e.Phone) ? "" : " (" + e.Phone + ")") }),
            "EmployeeId",
            "Text",
            jobserviceitem.MechanicId);
        // If the item already has mechanic or its parent job has mechanic, mark view to render mechanic as readonly
        ViewBag.ReadonlyMechanic = jobserviceitem.MechanicId.HasValue || (jobserviceitem.JobOrder != null && jobserviceitem.JobOrder.MechanicId.HasValue);
        if (ViewBag.ReadonlyMechanic == true)
        {
            var job = jobserviceitem.JobOrder ?? _context.JobOrders.Include(j=>j.Mechanic).FirstOrDefault(j=>j.JobOrderId==jobserviceitem.JobOrderId);
            ViewBag.ReadonlyMechanicText = job?.Mechanic != null ? job.Mechanic.FirstName + " " + job.Mechanic.LastName : "Assigned";
        }

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

        // Check if job is completed or invoiced - prevent editing
        if (jobserviceitem.JobOrder != null && 
            (jobserviceitem.JobOrder.Status == "Completed" || jobserviceitem.JobOrder.Status == "Invoiced"))
        {
            TempData["Toast"] = "Cannot edit service items from completed or invoiced jobs.";
            TempData["ToastType"] = "danger";
            return RedirectToAction("Details", "JobOrders", new { id = jobserviceitem.JobOrderId });
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
            _context.Employees.Where(e => e.Designation == "Mechanic" && e.IsActive == true).OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new { e.EmployeeId, Text = e.FirstName + " " + e.LastName + (string.IsNullOrEmpty(e.Phone) ? "" : " (" + e.Phone + ")") }),
            "EmployeeId",
            "Text",
            jobserviceitem.MechanicId);

        // Ensure we always check the parent job for assigned mechanic and set readonly flag accordingly
        var relatedJob = await _context.JobOrders.Include(j => j.Mechanic).FirstOrDefaultAsync(j => j.JobOrderId == jobserviceitem.JobOrderId);
        ViewBag.ReadonlyMechanic = jobserviceitem.MechanicId.HasValue || (relatedJob != null && relatedJob.MechanicId.HasValue);
        if (ViewBag.ReadonlyMechanic == true)
        {
            ViewBag.ReadonlyMechanicText = relatedJob?.Mechanic != null ? relatedJob.Mechanic.FirstName + " " + relatedJob.Mechanic.LastName : "Assigned";
            // ensure displayed and persisted value uses the job's mechanic when assigned
            if (relatedJob != null && relatedJob.MechanicId.HasValue)
            {
                jobserviceitem.MechanicId = relatedJob.MechanicId;
            }
        }

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
                // if related job has mechanic assigned, do not allow overriding
                var relatedJob = await _context.JobOrders.FindAsync(jobserviceitem.JobOrderId);
                if (relatedJob != null && relatedJob.MechanicId.HasValue)
                {
                    jobserviceitem.MechanicId = relatedJob.MechanicId;
                }

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
            _context.Employees.Where(e => e.Designation == "Mechanic" && e.IsActive == true).OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new { e.EmployeeId, Text = e.FirstName + " " + e.LastName + (string.IsNullOrEmpty(e.Phone) ? "" : " (" + e.Phone + ")") }),
            "EmployeeId",
            "Text",
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
        var jobserviceitem = await _context.JobServiceItems
            .Include(j => j.JobOrder)
            .FirstOrDefaultAsync(m => m.JobServiceItemId == id);

        if (jobserviceitem != null)
        {
            // Check if job is completed or invoiced - prevent deletion
            if (jobserviceitem.JobOrder != null && 
                (jobserviceitem.JobOrder.Status == "Completed" || jobserviceitem.JobOrder.Status == "Invoiced"))
            {
                TempData["Toast"] = "Cannot delete service items from completed or invoiced jobs.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Details", "JobOrders", new { id = jobserviceitem.JobOrderId });
            }

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

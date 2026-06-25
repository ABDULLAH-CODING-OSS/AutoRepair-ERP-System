
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;   
[RoleAuthorize("Admin","Owner","Service Advisor","Inventory Manager","Receptionist","Mechanic")]
public class JobOrdersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.NotificationService _notifications;
    private readonly AutoRepairERD.Services.AuditService _auditService;

    public JobOrdersController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notifications, AutoRepairERD.Services.AuditService auditService)
    {
        _context = context;
        _notifications = notifications;
        _auditService = auditService;
    }

    // GET: JOBORDERS/GetJobMechanic?jobId=123
    [HttpGet]
    public JsonResult GetJobMechanic(int jobId)
    {
        var job = _context.JobOrders
            .Include(j => j.Mechanic)
            .FirstOrDefault(j => j.JobOrderId == jobId);

        if (job == null)
        {
            return Json(new { mechanicId = (int?)null, mechanicName = "" });
        }

        return Json(new
        {
            mechanicId = job.MechanicId,
            mechanicName = job.Mechanic != null ? job.Mechanic.FirstName + " " + job.Mechanic.LastName : ""
        });
    }

    // GET: JOBORDERS
    public async Task<IActionResult> Index()    
    {
        // Mechanics should not access the full Job Orders index
        var role = HttpContext.Session.GetString("RoleName") ?? "";
        if (role == "Mechanic")
        {
            return RedirectToAction(nameof(MyAssignedJobs));
        }
        var jobs = await _context.JobOrders
            .Include(j => j.Customer)
            .Include(j => j.Vehicle)
            .Include(j => j.Advisor)
            .Include(j => j.Mechanic)
            .ToListAsync();

        return View(jobs);
    }

    // GET: JOBORDERS/MyAssignedJobs
    // Shows only jobs assigned to the currently logged-in mechanic
    public async Task<IActionResult> MyAssignedJobs(string q, string status)
    {
        var userId = HttpContext.Session.GetInt32("UserID");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        // Find employee linked to this user
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.UserId == userId.Value);

        if (employee == null)
        {
            return RedirectToAction("AccessDenied", "Home");
        }

        var query = _context.JobOrders
            .Where(j => j.MechanicId == employee.EmployeeId)
            .Include(j => j.Customer)
            .Include(j => j.Vehicle)
            .Include(j => j.Advisor)
            .Include(j => j.Mechanic)
            .AsQueryable();

        // Search by job number, customer name, vehicle registration
        if (!string.IsNullOrWhiteSpace(q))
        {
            var qlow = q.Trim().ToLower();
            query = query.Where(j => (j.JobNumber != null && j.JobNumber.ToLower().Contains(qlow))
                || (j.Customer != null && (j.Customer.FirstName + " " + j.Customer.LastName).ToLower().Contains(qlow))
                || (j.Vehicle != null && j.Vehicle.LicensePlate != null && j.Vehicle.LicensePlate.ToLower().Contains(qlow)));
        }

        // Filter by status
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(j => j.Status == status);
        }

        ViewBag.SearchQuery = q;
        ViewBag.FilterStatus = status;

        var jobs = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();

        // Build view models extracting priority token from DiagnosisNotes
        var vmList = jobs.Select(j => new AutoRepairERD.Models.ViewModels.AssignedJobViewModel
        {
            Job = j,
            Priority = ExtractPriority(j.DiagnosisNotes)
        }).ToList();

        return View(vmList);
    }

    private string ExtractPriority(string notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;
        // looks for [Priority:High] token
        var start = notes.IndexOf("[Priority:", StringComparison.OrdinalIgnoreCase);
        if (start < 0) return null;
        var end = notes.IndexOf(']', start);
        if (end <= start) return null;
        var token = notes.Substring(start + 10, end - (start + 10));
        return token;
    }

    // GET: JOBORDERS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id== null)
        {
            return NotFound();
        }

        var joborder = await _context.JobOrders
            .Include(j => j.Customer)
            .Include(j => j.Vehicle)
            .Include(j => j.Advisor)
            .Include(j => j.Mechanic)
            .Include(j => j.JobServiceItems).ThenInclude(js => js.Service)
            .Include(j => j.JobPartItems).ThenInclude(jp => jp.Part)
            .FirstOrDefaultAsync(m => m.JobOrderId == id);
        if (joborder == null)
        {
            return NotFound();
        }

        // If current user is a mechanic, ensure they can only view their assigned jobs
        var role = HttpContext.Session.GetString("RoleName") ?? "";
        if (role == "Mechanic")
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId.Value);
            if (employee == null || joborder.MechanicId != employee.EmployeeId)
            {
                return RedirectToAction("AccessDenied", "Home");
            }
        }

        return View(joborder);
    }

    // GET: JOBORDERS/Create
    //public IActionResult Create()
    //{
    //    return View();
    //}
    [RoleAuthorize("Admin","Owner","Service Advisor")]
    public IActionResult Create()
    {
        ViewBag.Customers = _context.Customers
            .Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = c.FirstName + " " + c.LastName + " (" + c.Phone + ")"
            })
            .ToList();
        ViewBag.Vehicles = new List<SelectListItem>();
        ViewBag.Advisors = _context.Employees
            .Where(e => e.Designation == "Service Advisor")
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = e.FirstName + " " + e.LastName
            })
            .ToList();

        // Preselect Service Advisor as the currently logged-in employee if applicable (any employee)
        var userId = HttpContext.Session.GetInt32("UserID");
        if (userId.HasValue)
        {
            var emp = _context.Employees.FirstOrDefault(e => e.UserId == userId.Value);
            if (emp != null)
            {
                ViewBag.DefaultAdvisorId = emp.EmployeeId;
            }
        }

        ViewBag.Mechanics = _context.Employees
            .Where(e => e.Designation == "Mechanic" && e.IsActive == true)
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = e.FirstName + " " + e.LastName + (string.IsNullOrEmpty(e.Phone) ? "" : " (" + e.Phone + ")")
            })
            .ToList();

        // No mechanic context needed on Create (job not assigned yet)
        return View();
    }
    [HttpGet]
    public JsonResult GetVehiclesByCustomer(int customerId)
    {
        var vehicles = _context.Vehicles
            .Where(v => v.CustomerId == customerId)
            .Select(v => new
            {
                vehicleId = v.VehicleId,
                text = v.Make + " " + v.Model + " - " + v.LicensePlate
            })
            .ToList();

        return Json(vehicles);
    }
    // POST: JOBORDERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RoleAuthorize("Admin","Owner","Service Advisor")]
    public async Task<IActionResult> Create(
     [Bind("CustomerId,VehicleId,AdvisorId,MechanicId,ComplaintDescription,DiagnosisNotes,EstimatedCompletionDate,EstimatedCost")]
    JobOrder joborder)
    {
        ModelState.Remove("Customer");
        ModelState.Remove("Vehicle");
        ModelState.Remove("Advisor");
        ModelState.Remove("Mechanic");
        ModelState.Remove("CreatedByUser");
        ModelState.Remove("Invoices");
        ModelState.Remove("JobPartItems");
        ModelState.Remove("JobServiceItems");
        ModelState.Remove("JobNumber");
        ModelState.Remove("Status");
        var selectedVehicle = await _context.Vehicles
    .FirstOrDefaultAsync(v => v.VehicleId == joborder.VehicleId);

        if (selectedVehicle == null)
        {
            ModelState.AddModelError(
                "VehicleId",
                "Please select a valid vehicle.");
        }
        else if (selectedVehicle.CustomerId != joborder.CustomerId)
        {
            ModelState.AddModelError(
                "VehicleId",
                "Selected vehicle does not belong to the selected customer.");
        }
        if (!ModelState.IsValid)
        {
            foreach (var item in ModelState)
            {
                foreach (var error in item.Value.Errors)
                {
                    ViewBag.ErrorMessage +=
                        $"{item.Key}: {error.ErrorMessage}<br/>";
                }
            }
        }
        if (ModelState.IsValid)
        {
            joborder.CreatedByUserId =
                HttpContext.Session.GetInt32("UserID");

            joborder.CreatedAt = DateTime.Now;

            joborder.JobNumber =
                "JOB" + DateTime.Now.ToString("yyyyMMddHHmmss");
            joborder.Status = "Pending";
            // If advisor not set but current user is a Service Advisor, auto-assign
            if (!joborder.AdvisorId.HasValue)
            {
                var currentUserId = HttpContext.Session.GetInt32("UserID");
                if (currentUserId.HasValue)
                {
                    var currEmp = _context.Employees.FirstOrDefault(e => e.UserId == currentUserId.Value && e.Designation == "Service Advisor");
                    if (currEmp != null)
                    {
                        joborder.AdvisorId = currEmp.EmployeeId;
                    }
                }
            }

            // Capture priority from form (no schema change) by appending to DiagnosisNotes as a minimal workaround
            var priority = HttpContext.Request.Form["Priority"].FirstOrDefault();
            if (!string.IsNullOrEmpty(priority))
            {
                joborder.DiagnosisNotes = (joborder.DiagnosisNotes ?? "") + "\n[Priority:" + priority + "]";
            }
            _context.Add(joborder);

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogCreateAsync("JobOrders", joborder.JobOrderId, joborder.JobNumber);

            // Notification: Job Order Created
            try
            {
                // If an advisor was explicitly assigned, notify that advisor only. Otherwise notify Service Advisor role members.
                if (joborder.AdvisorId.HasValue)
                {
                    var adv = _context.Employees.FirstOrDefault(e => e.EmployeeId == joborder.AdvisorId.Value);
                    if (adv != null && adv.UserId.HasValue)
                    {
                        await _notifications.CreateForUserAsync(adv.UserId.Value, "JobCreated", "New Job Order", $"Job {joborder.JobNumber} created and assigned to you.", HttpContext.Session.GetInt32("UserID"));
                    }
                }
                else
                {
                    var saRoleId = _context.Roles.Where(r => r.RoleName == "Service Advisor").Select(r => r.RoleId).FirstOrDefault();
                    if (saRoleId != 0)
                    {
                        await _notifications.CreateForRoleAsync(saRoleId, "JobCreated", "New Job Order", $"Job {joborder.JobNumber} created.", HttpContext.Session.GetInt32("UserID"));
                    }
                }

                // Also notify management (Admin and Owner)
                var adminId = _context.Roles.Where(r => r.RoleName == "Admin").Select(r => r.RoleId).FirstOrDefault();
                var ownerId = _context.Roles.Where(r => r.RoleName == "Owner").Select(r => r.RoleId).FirstOrDefault();
                if (adminId != 0) await _notifications.CreateForRoleAsync(adminId, "JobCreated", "New Job Order", $"Job {joborder.JobNumber} created.", HttpContext.Session.GetInt32("UserID"));
                if (ownerId != 0) await _notifications.CreateForRoleAsync(ownerId, "JobCreated", "New Job Order", $"Job {joborder.JobNumber} created.", HttpContext.Session.GetInt32("UserID"));

                // If mechanic assigned at creation, notify that mechanic user
                if (joborder.MechanicId.HasValue)
                {
                    var mech = _context.Employees.FirstOrDefault(e => e.EmployeeId == joborder.MechanicId.Value);
                    if (mech != null && mech.UserId.HasValue)
                    {
                        await _notifications.CreateForUserAsync(mech.UserId.Value, "JobAssigned", "Job assigned", $"You have been assigned job {joborder.JobNumber}.", HttpContext.Session.GetInt32("UserID"));
                    }
                }
            }
            catch { }

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Customers = _context.Customers
            .Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = c.FirstName + " " + c.LastName + " (" + c.Phone + ")"
            })
            .ToList();

        //ViewBag.Vehicles = _context.Vehicles
        //    .Select(v => new SelectListItem
        //    {
        //        Value = v.VehicleId.ToString(),
        //        Text = v.Make + " " + v.Model + " - " + v.LicensePlate
        //    })
        //    .ToList();
        ViewBag.Vehicles = _context.Vehicles
    .Where(v => v.CustomerId == joborder.CustomerId)
    .Select(v => new SelectListItem
    {
        Value = v.VehicleId.ToString(),
        Text = v.Make + " " + v.Model + " - " + v.LicensePlate
    })
    .ToList();

        ViewBag.Advisors = _context.Employees
            .Where(e => e.Designation == "Service Advisor")
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = e.FirstName + " " + e.LastName
            })
            .ToList();

        ViewBag.Mechanics = _context.Employees
            .Where(e => e.Designation == "Mechanic" && e.IsActive == true)
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = e.FirstName + " " + e.LastName + (string.IsNullOrEmpty(e.Phone) ? "" : " (" + e.Phone + ")")
            })
            .ToList();

        // If mechanic, ensure only assigned mechanic can edit and present limited UI
        var role = HttpContext.Session.GetString("RoleName") ?? "";
        if (role == "Mechanic")
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId.Value);
            if (employee == null || joborder.MechanicId != employee.EmployeeId)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            ViewBag.IsMechanic = true;
        }

        return View(joborder);
    }

    // GET: JOBORDERS/Edit/5
    //public async Task<IActionResult> Edit(int? id)
    //{
    //    if (id == null)
    //    {
    //        return NotFound();
    //    }

    //    var joborder = await _context.JobOrders.FindAsync(id);
    //    if (joborder == null)
    //    {
    //        return NotFound();
    //    }
    //    return View(joborder);
    //}
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var joborder = await _context.JobOrders
            .Include(j => j.Customer)
            .Include(j => j.Vehicle)
            .FirstOrDefaultAsync(j => j.JobOrderId == id);

        if (joborder == null)
        {
            return NotFound();
        }

        ViewBag.Customers = _context.Customers
            .Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = c.FirstName + " " + c.LastName
            })
            .ToList();

        ViewBag.Vehicles = _context.Vehicles
            .Select(v => new SelectListItem
            {
                Value = v.VehicleId.ToString(),
                Text = v.Make + " " + v.Model
            })
            .ToList();

        ViewBag.Advisors = _context.Employees
            .Where(e => e.Designation == "Service Advisor")
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = e.FirstName + " " + e.LastName
            })
            .ToList();

        ViewBag.Mechanics = _context.Employees
            .Where(e => e.Designation == "Mechanic" && e.IsActive == true)
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = e.FirstName + " " + e.LastName + (string.IsNullOrEmpty(e.Phone) ? "" : " (" + e.Phone + ")")
            })
            .ToList();

        return View(joborder);
    }

    // POST: JOBORDERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> Edit(int? id, [Bind("JobOrderId,CustomerId,VehicleId,AdvisorId,MechanicId,CreatedByUserId,JobNumber,ComplaintDescription,DiagnosisNotes,EstimatedCompletionDate,StartDate,CompletionDate,Status,EstimatedCost,FinalCost,CreatedAt,Advisor,CreatedByUser,Customer,Invoices,JobPartItems,JobServiceItems,Mechanic,Vehicle")] JobOrder joborder)
    //{
    //    if (id != joborder.JobOrderId)
    //    {
    //        return NotFound();
    //    }
    //    ModelState.Remove("Advisor");
    //    ModelState.Remove("Customer");
    //    ModelState.Remove("CreatedByUser");
    //    ModelState.Remove("Invoices");
    //    ModelState.Remove("JobPartItems");
    //    ModelState.Remove("JobServiceItems");
    //    ModelState.Remove("Mechanic");
    //    ModelState.Remove("Vehicle");
    //    ModelState.Remove("CreatedByUserId");
    //    ModelState.Remove("JobNumber");
    //    ModelState.Remove("CreatedAt");
    //    if (!ModelState.IsValid)
    //    {
    //        foreach (var item in ModelState)
    //        {
    //            foreach (var error in item.Value.Errors)
    //            {
    //                Console.WriteLine($"{item.Key}: {error.ErrorMessage}");
    //            }
    //        }
    //    }
    //    if (ModelState.IsValid)
    //    {
    //        try
    //        {
    //            _context.Update(joborder);
    //            await _context.SaveChangesAsync();
    //        }
    //        catch (DbUpdateConcurrencyException)
    //        {
    //            if (id != joborder.JobOrderId)
    //            {
    //                return NotFound();
    //            }

    //            var existingJob = await _context.JobOrders
    //                .AsNoTracking()
    //                .FirstOrDefaultAsync(j => j.JobOrderId == id);

    //            if (existingJob == null)
    //            {
    //                return NotFound();
    //            }

    //            joborder.JobNumber = existingJob.JobNumber;
    //            joborder.CreatedAt = existingJob.CreatedAt;
    //            joborder.CreatedByUserId = existingJob.CreatedByUserId;

    //            ModelState.Remove("Advisor");
    //            ModelState.Remove("Customer");
    //            ModelState.Remove("CreatedByUser");
    //            ModelState.Remove("Invoices");
    //            ModelState.Remove("JobPartItems");
    //            ModelState.Remove("JobServiceItems");
    //            ModelState.Remove("Mechanic");
    //            ModelState.Remove("Vehicle");
    //            ModelState.Remove("CreatedByUserId");
    //            ModelState.Remove("JobNumber");
    //            ModelState.Remove("CreatedAt");
    //            else
    //            {
    //                throw;
    //            }
    //        }
    //        return RedirectToAction(nameof(Index));
    //    }
    //    return View(joborder);
    //}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
    int? id,
    [Bind("JobOrderId,CustomerId,VehicleId,AdvisorId,MechanicId,CreatedByUserId,JobNumber,ComplaintDescription,DiagnosisNotes,EstimatedCompletionDate,StartDate,CompletionDate,Status,EstimatedCost,FinalCost,CreatedAt,Advisor,CreatedByUser,Customer,Invoices,JobPartItems,JobServiceItems,Mechanic,Vehicle")]
    JobOrder joborder)
    {
        if (id != joborder.JobOrderId)
        {
            return NotFound();
        }

        var existingJob = await _context.JobOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.JobOrderId == id);
        if (existingJob == null)
        {
            return NotFound();
        }

        if (existingJob.Status == "Completed" || existingJob.Status == "Cancelled")
        {
            TempData["Error"] =
                "Completed or cancelled jobs cannot be modified.";

            return RedirectToAction(nameof(Index));
        }

        if (existingJob == null)
        {
            return NotFound();
        }

        // Preserve values not shown on Edit screen
        joborder.JobNumber = existingJob.JobNumber;
        joborder.CreatedAt = existingJob.CreatedAt;
        joborder.CreatedByUserId = existingJob.CreatedByUserId;
        // Preserve FinalCost - only updated by Invoice process
        joborder.FinalCost = existingJob.FinalCost;

        ModelState.Remove("Advisor");
        ModelState.Remove("Customer");
        ModelState.Remove("CreatedByUser");
        ModelState.Remove("Invoices");
        ModelState.Remove("JobPartItems");
        ModelState.Remove("JobServiceItems");
        ModelState.Remove("Mechanic");
        ModelState.Remove("Vehicle");
        ModelState.Remove("CreatedByUserId");
        ModelState.Remove("JobNumber");
        ModelState.Remove("CreatedAt");

        // Validation: if both dates provided, CompletionDate must be >= StartDate
        if (joborder.StartDate.HasValue && joborder.CompletionDate.HasValue)
        {
            if (joborder.CompletionDate.Value < joborder.StartDate.Value)
            {
                ModelState.AddModelError("CompletionDate", "Completion Date must be the same as or later than Start Date.");
            }
        }

        if (!ModelState.IsValid)
        {
            foreach (var item in ModelState)
            {
                foreach (var error in item.Value.Errors)
                {
                    Console.WriteLine($"{item.Key}: {error.ErrorMessage}");
                }
            }
        }

        var role = HttpContext.Session.GetString("RoleName") ?? "";

        if (role == "Mechanic")
        {
            // Mechanics can only update status (and optionally notes). Validate ownership and enforce allowed transitions.
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId.Value);
            if (employee == null)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var existing = await _context.JobOrders.AsNoTracking().FirstOrDefaultAsync(j => j.JobOrderId == id);
            if (existing == null || existing.MechanicId != employee.EmployeeId)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            // Enforce allowed transitions: Pending -> In Progress -> Completed
            var oldStatus = existing.Status ?? "";
            var newStatus = joborder.Status ?? "";

            bool validTransition = false;
            if (oldStatus == "Pending" && newStatus == "In Progress") validTransition = true;
            else if (oldStatus == "In Progress" && newStatus == "Completed") validTransition = true;
            else if (oldStatus == newStatus) validTransition = true; // allow idempotent

            if (!validTransition)
            {
                ModelState.AddModelError("Status", "Invalid status transition.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.IsMechanic = true;
                return View(joborder);
            }

            // Preserve protected fields from existing record
            joborder.CustomerId = existing.CustomerId;
            joborder.VehicleId = existing.VehicleId;
            joborder.AdvisorId = existing.AdvisorId;
            joborder.MechanicId = existing.MechanicId;
            joborder.CreatedByUserId = existing.CreatedByUserId;
            joborder.JobNumber = existing.JobNumber;
            joborder.CreatedAt = existing.CreatedAt;
            joborder.FinalCost = existing.FinalCost;

            // If status changed to Completed, set CompletionDate if not set
            if (oldStatus != "Completed" && newStatus == "Completed")
            {
                joborder.CompletionDate = DateTime.Now;
            }

            try
            {
                var oldMechanicId = existingJob.MechanicId;

                _context.Update(joborder);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogUpdateAsync("JobOrders", joborder.JobOrderId, null, $"{joborder.JobNumber}, Status: {newStatus}");

                // Notify on assignment change
                try
                {
                    if (oldMechanicId != joborder.MechanicId && joborder.MechanicId.HasValue)
                    {
                        var mech = _context.Employees.FirstOrDefault(e => e.EmployeeId == joborder.MechanicId.Value);
                        if (mech != null && mech.UserId.HasValue)
                        {
                            await _notifications.CreateForUserAsync(mech.UserId.Value, "JobAssigned", "Job assigned", $"You have been assigned job {joborder.JobNumber}.", HttpContext.Session.GetInt32("UserID"));
                        }
                    }
                    // Notify on completion
                    if (((existingJob.Status ?? "") != "Completed") && joborder.Status == "Completed")
                    {
                        // Notify advisor user if present
                        var advisor = _context.Employees.FirstOrDefault(e => e.EmployeeId == joborder.AdvisorId);
                        if (advisor != null && advisor.UserId.HasValue)
                        {
                            await _notifications.CreateForUserAsync(advisor.UserId.Value, "JobCompleted", "Job completed", $"Job {joborder.JobNumber} has been completed.", HttpContext.Session.GetInt32("UserID"));
                        }

                        // Notify management roles: Admin and Owner
                        var adminRole = _context.Roles.FirstOrDefault(r => r.RoleName == "Admin");
                        var ownerRole = _context.Roles.FirstOrDefault(r => r.RoleName == "Owner");
                        if (adminRole != null) await _notifications.CreateForRoleAsync(adminRole.RoleId, "JobCompleted", "Job completed", $"Job {joborder.JobNumber} has been completed.", HttpContext.Session.GetInt32("UserID"));
                        if (ownerRole != null) await _notifications.CreateForRoleAsync(ownerRole.RoleId, "JobCompleted", "Job completed", $"Job {joborder.JobNumber} has been completed.", HttpContext.Session.GetInt32("UserID"));
                    }
                }
                catch { }
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

            return RedirectToAction(nameof(MyAssignedJobs));
        }

        if (ModelState.IsValid)
        {
            try
            {
                // If status changed to Completed and CompletionDate not set, set it automatically
                if (existingJob.Status != "Completed" && joborder.Status == "Completed")
                {
                    joborder.CompletionDate = DateTime.Now;
                }
                // If status changed to Cancelled from a non-cancelled state, return parts to stock
                if (existingJob.Status != "Cancelled" && joborder.Status == "Cancelled")
                {
                    var parts = _context.JobPartItems
                        .Where(p => p.JobOrderId == joborder.JobOrderId)
                        .ToList();

                    foreach (var jp in parts)
                    {
                        var part = await _context.Parts.FindAsync(jp.PartId);
                        var qty = jp.Quantity ?? 0;
                        if (part != null && qty > 0)
                        {
                            var previous = part.CurrentStock;
                            part.CurrentStock += qty;

                            var tx = new StockTransaction
                            {
                                PartId = part.PartId,
                                TransactionType = "IN",
                                Quantity = qty,
                                PreviousStock = previous,
                                NewStock = part.CurrentStock,
                                ReferenceNumber = "JOB-CANCEL-" + joborder.JobOrderId,
                                Remarks = "Job cancelled - parts returned",
                                TransactionDate = DateTime.Now
                            };

                            _context.StockTransactions.Add(tx);
                            _context.Parts.Update(part);
                        }
                    }
                }
                _context.Update(joborder);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogCustomActionAsync("JobOrders", joborder.JobOrderId, "Cancelled", $"{joborder.JobNumber}");
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

        ViewBag.Customers = _context.Customers
            .Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = c.FirstName + " " + c.LastName
            })
            .ToList();

        ViewBag.Vehicles = _context.Vehicles
            .Select(v => new SelectListItem
            {
                Value = v.VehicleId.ToString(),
                Text = v.Make + " " + v.Model
            })
            .ToList();

        ViewBag.Advisors = _context.Employees
            .Where(e => e.Designation == "Service Advisor")
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = e.FirstName + " " + e.LastName
            })
            .ToList();

        ViewBag.Mechanics = _context.Employees
            .Where(e => e.Designation == "Mechanic" && e.IsActive == true)
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = e.FirstName + " " + e.LastName + (string.IsNullOrEmpty(e.Phone) ? "" : " (" + e.Phone + ")")
            })
            .ToList();

        return View(joborder);
    }

    // GET: JOBORDERS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var joborder = await _context.JobOrders
            .Include(j => j.Customer)
            .Include(j => j.Vehicle)
            .Include(j => j.Advisor)
            .Include(j => j.Mechanic)
            .FirstOrDefaultAsync(m => m.JobOrderId == id);
        if (joborder == null)
        {
            return NotFound();
        }

        return View(joborder);
    }

    // POST: JOBORDERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [RoleAuthorize("Admin","Owner","Service Advisor")]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var joborder = await _context.JobOrders.FindAsync(id);
        if (joborder == null)
        {
            return RedirectToAction(nameof(Index));
        }
        if (joborder.Status == "Completed")
        {
            TempData["Error"] = "Completed jobs cannot be deleted.";
            return RedirectToAction(nameof(Delete), new { id });
        }
        if (joborder != null)
        {
            // restore part stocks for parts associated with this job
            var parts = _context.JobPartItems.Where(p => p.JobOrderId == joborder.JobOrderId).ToList();
            foreach (var jp in parts)
            {
                var part = await _context.Parts.FindAsync(jp.PartId);
                var qty = jp.Quantity ?? 0;
                if (part != null && qty > 0)
                {
                    var previous = part.CurrentStock;
                    part.CurrentStock += qty;

                    var tx = new StockTransaction
                    {
                        PartId = part.PartId,
                        TransactionType = "IN",
                        Quantity = qty,
                        PreviousStock = previous,
                        NewStock = part.CurrentStock,
                        ReferenceNumber = "JOB-DEL-" + joborder.JobOrderId,
                        Remarks = "Job deleted - parts returned",
                        TransactionDate = DateTime.Now
                    };

                    _context.StockTransactions.Add(tx);
                    _context.Parts.Update(part);
                }
            }

            _context.JobOrders.Remove(joborder);
        }

        try
        {
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogDeleteAsync("JobOrders", (int)id, joborder?.JobNumber ?? "Unknown");

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            // likely due to FK constraints - show friendly message
            TempData["Error"] = "Cannot delete this Job Order because related records exist (invoices, parts, or services).";
            return RedirectToAction(nameof(Delete), new { id });
        }
    }

    private bool JobOrderExists(int? id)
    {
        return _context.JobOrders.Any(e => e.JobOrderId == id);
    }
}

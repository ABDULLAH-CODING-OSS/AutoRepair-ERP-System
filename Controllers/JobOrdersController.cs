
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;   
[SessionAuthorize]

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
        var jobs = await _context.JobOrders
            .Include(j => j.Customer)
            .Include(j => j.Vehicle)
            .Include(j => j.Advisor)
            .Include(j => j.Mechanic)
            .ToListAsync();

        return View(jobs);
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
            .FirstOrDefaultAsync(m => m.JobOrderId == id);
        if (joborder == null)
        {
            return NotFound();
        }

        return View(joborder);
    }

    // GET: JOBORDERS/Create
    //public IActionResult Create()
    //{
    //    return View();
    //}
    public IActionResult Create()
    {
        ViewBag.Customers = _context.Customers
            .Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = c.FirstName + " " + c.LastName + " (" + c.Phone + ")"
            })
            .ToList();

        ViewBag.Vehicles = _context.Vehicles
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
            .Where(e => e.Designation == "Mechanic")
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = e.FirstName + " " + e.LastName
            })
            .ToList();

        return View();
    }
    // POST: JOBORDERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
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
            _context.Add(joborder);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Customers = _context.Customers
            .Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = c.FirstName + " " + c.LastName + " (" + c.Phone + ")"
            })
            .ToList();

        ViewBag.Vehicles = _context.Vehicles
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
            .Where(e => e.Designation == "Mechanic")
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = e.FirstName + " " + e.LastName
            })
            .ToList();

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
            .Where(e => e.Designation == "Mechanic")
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = e.FirstName + " " + e.LastName
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
        if (existingJob.Status == "Completed")
        {
            TempData["Error"] =
                "Completed jobs cannot be modified.";

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
            .Where(e => e.Designation == "Mechanic")
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = e.FirstName + " " + e.LastName
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

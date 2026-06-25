
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
[RoleAuthorize("Admin","Owner","Service Advisor")]
public class InvoicesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.NotificationService _notifications;
    private readonly AutoRepairERD.Services.AuditService _auditService;

    public InvoicesController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notifications, AutoRepairERD.Services.AuditService auditService)
    {
        _context = context;
        _notifications = notifications;
        _auditService = auditService;
    }

    // GET: INVOICES
    public async Task<IActionResult> Index(string q)
    {
        var query = _context.Invoices
            .Include(i => i.JobOrder)
                .ThenInclude(j => j.Customer)
            .Include(i => i.JobOrder)
                .ThenInclude(j => j.Vehicle)
            .Include(i => i.Payments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(i => (i.InvoiceNumber ?? "").Contains(q) || (i.JobOrder != null && ((i.JobOrder.JobNumber ?? "").Contains(q) || (i.JobOrder.Customer != null && ((i.JobOrder.Customer.FirstName ?? "").Contains(q) || (i.JobOrder.Customer.LastName ?? "").Contains(q))))) || (i.PaymentStatus ?? "").Contains(q));
            ViewBag.SearchQuery = q;
        }

        var list = await query.ToListAsync();
        return View(list);
    }

    // GET: INVOICES/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var invoice = await _context.Invoices
            .Include(i => i.JobOrder)
                .ThenInclude(j => j.Customer)
            .Include(i => i.JobOrder)
                .ThenInclude(j => j.Vehicle)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(m => m.InvoiceId == id);
        if (invoice == null)
        {
            return NotFound();
        }

        // compute breakdowns
        var partsTotal = _context.JobPartItems
            .Where(j => j.JobOrderId == invoice.JobOrderId)
            .Sum(j => j.TotalPrice ?? 0);

        var servicesTotal = _context.JobServiceItems
            .Where(j => j.JobOrderId == invoice.JobOrderId)
            .Sum(j => j.ServicePrice ?? 0);

        ViewBag.PartsTotal = partsTotal;
        ViewBag.ServicesTotal = servicesTotal;

        return View(invoice);
    }

    // GET: INVOICES/Create
    public async Task<IActionResult> Create()
    {
        // Only show completed job orders that don't have invoices yet
        var completedUninvoicedJobs = await _context.JobOrders
            .Where(j => j.Status == "Completed" && !_context.Invoices.Any(i => i.JobOrderId == j.JobOrderId))
            .OrderByDescending(j => j.JobOrderId)
            .ToListAsync();

        ViewBag.JobOrders = new SelectList(
            completedUninvoicedJobs,
            "JobOrderId",
            "JobNumber");

        return View();
    }

    // POST: INVOICES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("JobOrderId,TaxAmount,DiscountAmount")] Invoice invoice)
    {
        ModelState.Remove("CreatedByUser");
        ModelState.Remove("JobOrder");
        ModelState.Remove("Payments");
        ModelState.Remove("InvoiceNumber");
        ModelState.Remove("CreatedByUserId");
        ModelState.Remove("InvoiceDate");

        // Validate that the selected job is completed and not already invoiced
        var job = await _context.JobOrders.FindAsync(invoice.JobOrderId);
        if (job == null)
        {
            ModelState.AddModelError("JobOrderId", "Selected job order not found.");
        }
        else if (job.Status != "Completed")
        {
            ModelState.AddModelError("JobOrderId", "Only completed job orders can be invoiced.");
        }
        else if (await _context.Invoices.AnyAsync(i => i.JobOrderId == invoice.JobOrderId))
        {
            ModelState.AddModelError("JobOrderId", "An invoice already exists for this job order.");
        }

        if (ModelState.IsValid)
        {
            invoice.InvoiceNumber =
    "INV" + DateTime.Now.ToString("yyyyMMddHHmmss");

            invoice.InvoiceDate = DateTime.Now;

            invoice.CreatedByUserId =
                HttpContext.Session.GetInt32("UserID");

            invoice.InvoiceStatus = "Generated";

            invoice.PaymentStatus = "Unpaid";
            var partsTotal = _context.JobPartItems
    .Where(j => j.JobOrderId == invoice.JobOrderId)
    .Sum(j => j.TotalPrice ?? 0);

            var servicesTotal = _context.JobServiceItems
                .Where(j => j.JobOrderId == invoice.JobOrderId)
                .Sum(j => j.ServicePrice ?? 0);

            invoice.SubTotal = partsTotal + servicesTotal;

            invoice.GrandTotal =
    (invoice.SubTotal ?? 0)
    + (invoice.TaxAmount ?? 0)
    - (invoice.DiscountAmount ?? 0);
            var existingInvoice = await _context.Invoices
    .FirstOrDefaultAsync(i => i.JobOrderId == invoice.JobOrderId);

            if (existingInvoice != null)
            {
                ModelState.AddModelError("", "Invoice already exists for this Job.");

                var completedUninvoicedJobs = await _context.JobOrders
                    .Where(j => j.Status == "Completed" && !_context.Invoices.Any(i => i.JobOrderId == j.JobOrderId))
                    .OrderByDescending(j => j.JobOrderId)
                    .ToListAsync();

                ViewBag.JobOrders = new SelectList(
                    completedUninvoicedJobs,
                    "JobOrderId",
                    "JobNumber",
                    invoice.JobOrderId);

                return View(invoice);
            }
            _context.Add(invoice);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogCreateAsync("Invoices", invoice.InvoiceId, $"Invoice {invoice.InvoiceNumber}");

            // update JobOrder.FinalCost to reflect invoice grand total
            var jobOrder = await _context.JobOrders.FindAsync(invoice.JobOrderId);
            if (jobOrder != null)
            {
                jobOrder.FinalCost = invoice.GrandTotal;
                _context.Update(jobOrder);
                await _context.SaveChangesAsync();
            }

            // Notification: Invoice Generated
            try
            {
                var ownerRole = _context.Roles.FirstOrDefault(r => r.RoleName == "Owner");
                var adminRole = _context.Roles.FirstOrDefault(r => r.RoleName == "Admin");
                if (ownerRole != null)
                {
                    await _notifications.CreateForRoleAsync(ownerRole.RoleId, "InvoiceGenerated", "Invoice generated", $"Invoice {invoice.InvoiceNumber} generated for job {invoice.JobOrderId}.", HttpContext.Session.GetInt32("UserID"));
                }
                if (adminRole != null)
                {
                    await _notifications.CreateForRoleAsync(adminRole.RoleId, "InvoiceGenerated", "Invoice generated", $"Invoice {invoice.InvoiceNumber} generated for job {invoice.JobOrderId}.", HttpContext.Session.GetInt32("UserID"));
                }

                // High value check: configurable threshold - use 100000 as default
                var threshold = 100000m;
                if ((decimal)(invoice.GrandTotal) >= threshold)
                {
                    var title = "High value invoice";
                    var message = $"High value invoice {invoice.InvoiceNumber} generated. Amount: {invoice.GrandTotal:N0}.";
                    if (ownerRole != null)
                    {
                        await _notifications.CreateForRoleAsync(ownerRole.RoleId, "Invoices", title, message, HttpContext.Session.GetInt32("UserID"));
                    }
                    if (adminRole != null)
                    {
                        await _notifications.CreateForRoleAsync(adminRole.RoleId, "Invoices", title, message, HttpContext.Session.GetInt32("UserID"));
                    }
                }
            }
            catch { }
            return RedirectToAction(nameof(Index));
        }
        ViewBag.JobOrders = new SelectList(
     _context.JobOrders,
     "JobOrderId",
     "JobNumber",
     invoice.JobOrderId);

        return View(invoice);
    }

    // GET: INVOICES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null)
        {
            return NotFound();
        }
        ViewBag.JobOrders = new SelectList(
    _context.JobOrders,
    "JobOrderId",
    "JobNumber",
    invoice.JobOrderId);

        return View(invoice);
       
    }

    // POST: INVOICES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> Edit(int? id, [Bind("InvoiceId,JobOrderId,TaxAmount,DiscountAmount,InvoiceStatus,PaymentStatus")] Invoice invoice)
    //{

    //    if (id != invoice.InvoiceId)
    //    {
    //        return NotFound();
    //    }
    //    var existingInvoice = await _context.Invoices
    //.AsNoTracking()
    //.FirstOrDefaultAsync(i => i.InvoiceId == id);

    //    if (existingInvoice == null)
    //    {
    //        return NotFound();
    //    }

    //    invoice.InvoiceNumber = existingInvoice.InvoiceNumber;
    //    invoice.InvoiceDate = existingInvoice.InvoiceDate;
    //    invoice.CreatedByUserId = existingInvoice.CreatedByUserId;

    //    ModelState.Remove("CreatedByUser");
    //    ModelState.Remove("JobOrder");
    //    ModelState.Remove("Payments");
    //    if (ModelState.IsValid)
    //    {
    //        try
    //        {
    //            _context.Update(invoice);
    //            await _context.SaveChangesAsync();
    //        }
    //        catch (DbUpdateConcurrencyException)
    //        {
    //            if (!InvoiceExists(invoice.InvoiceId))
    //            {
    //                return NotFound();
    //            }
    //            else
    //            {
    //                throw;
    //            }
    //        }
    //        return RedirectToAction(nameof(Index));
    //    }
    //    return View(invoice);
    //}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
    int? id,
    [Bind("InvoiceId,JobOrderId,TaxAmount,DiscountAmount,InvoiceStatus,PaymentStatus")]
    Invoice invoice)
    {
        if (id != invoice.InvoiceId)
        {
            return NotFound();
        }

        var existingInvoice = await _context.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InvoiceId == id);

        if (existingInvoice == null)
        {
            return NotFound();
        }

        // Preserve values not shown on Edit screen
        invoice.InvoiceNumber = existingInvoice.InvoiceNumber;
        invoice.InvoiceDate = existingInvoice.InvoiceDate;
        invoice.CreatedByUserId = existingInvoice.CreatedByUserId;
        invoice.SubTotal = existingInvoice.SubTotal;
        invoice.GrandTotal = existingInvoice.GrandTotal;

        ModelState.Remove("CreatedByUser");
        ModelState.Remove("JobOrder");
        ModelState.Remove("Payments");
        ModelState.Remove("InvoiceNumber");
        ModelState.Remove("InvoiceDate");
        ModelState.Remove("CreatedByUserId");
        ModelState.Remove("SubTotal");
        ModelState.Remove("GrandTotal");
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
                var partsTotal = _context.JobPartItems
                    .Where(j => j.JobOrderId == invoice.JobOrderId)
                    .Sum(j => j.TotalPrice ?? 0);

                var servicesTotal = _context.JobServiceItems
                    .Where(j => j.JobOrderId == invoice.JobOrderId)
                    .Sum(j => j.ServicePrice ?? 0);

                invoice.SubTotal = partsTotal + servicesTotal;

                invoice.GrandTotal =
                    (invoice.SubTotal ?? 0)
                    + (invoice.TaxAmount ?? 0)
                    - (invoice.DiscountAmount ?? 0);

                // Recompute payment status based on existing payments so status and remaining balance are accurate after edits
                var totalPaidRaw = _context.Payments.Where(p => p.InvoiceId == invoice.InvoiceId).Sum(p => (decimal?)p.AmountPaid) ?? 0m;
                var totalPaid = Decimal.Round(totalPaidRaw, 2, MidpointRounding.AwayFromZero);
                var grand = Decimal.Round(invoice.GrandTotal, 2, MidpointRounding.AwayFromZero);
                if (totalPaid <= 0m) invoice.PaymentStatus = "Unpaid";
                else if (totalPaid < grand) invoice.PaymentStatus = "Partially Paid";
                else invoice.PaymentStatus = "Paid";

                _context.Update(invoice);

                await _context.SaveChangesAsync();


                // Audit log
                await _auditService.LogUpdateAsync("Invoices", invoice.InvoiceId, null, $"Invoice {invoice.InvoiceNumber}");

                // update JobOrder.FinalCost after invoice edit
                var job = await _context.JobOrders.FindAsync(invoice.JobOrderId);
                if (job != null)
                {
                    job.FinalCost = invoice.GrandTotal;
                    _context.Update(job);
                    await _context.SaveChangesAsync();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InvoiceExists(invoice.InvoiceId))
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
            invoice.JobOrderId);

        return View(invoice);
    }

    // GET: INVOICES/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(m => m.InvoiceId == id);
        if (invoice == null)
        {
            return NotFound();
        }

        return View(invoice);
    }

    // POST: INVOICES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice != null)
        {
            // Check if invoice is paid - don't allow deletion
            if (invoice.PaymentStatus == "Paid")
            {
                TempData["Toast"] = "Paid invoices cannot be deleted.";
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var invoiceNum = invoice.InvoiceNumber;
            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogDeleteAsync("Invoices", (int)id, $"Invoice {invoiceNum}");
        }
        else
        {
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool InvoiceExists(int? id)
    {
        return _context.Invoices.Any(e => e.InvoiceId == id);
    }
}

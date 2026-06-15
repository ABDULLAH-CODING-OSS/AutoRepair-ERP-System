
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

    public InvoicesController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    // GET: INVOICES
    public async Task<IActionResult> Index()    
    {
        // include related job and customer/vehicle for display
        var list = await _context.Invoices
            .Include(i => i.JobOrder)
                .ThenInclude(j => j.Customer)
            .Include(i => i.JobOrder)
                .ThenInclude(j => j.Vehicle)
            .ToListAsync();

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
    //public IActionResult Create()
    //{
    //    return View();
    //}
    public IActionResult Create()
    {
        ViewBag.JobOrders = new SelectList(
            _context.JobOrders,
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

                ViewBag.JobOrders = new SelectList(
                    _context.JobOrders,
                    "JobOrderId",
                    "JobNumber",
                    invoice.JobOrderId);

                return View(invoice);
            }
            _context.Add(invoice);
            await _context.SaveChangesAsync();
            // update JobOrder.FinalCost to reflect invoice grand total
            var job = await _context.JobOrders.FindAsync(invoice.JobOrderId);
            if (job != null)
            {
                job.FinalCost = invoice.GrandTotal;
                _context.Update(job);
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
                    if (ownerRole != null)
                    {
                        await _notifications.CreateForRoleAsync(ownerRole.RoleId, "HighValueInvoice", "High value invoice", $"Invoice {invoice.InvoiceNumber} amount {invoice.GrandTotal:C} exceeds threshold.", HttpContext.Session.GetInt32("UserID"));
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

                _context.Update(invoice);

                await _context.SaveChangesAsync();
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
            _context.Invoices.Remove(invoice);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool InvoiceExists(int? id)
    {
        return _context.Invoices.Any(e => e.InvoiceId == id);
    }
}

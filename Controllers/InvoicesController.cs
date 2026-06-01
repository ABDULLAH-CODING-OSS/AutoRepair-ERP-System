
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
[SessionAuthorize]
public class InvoicesController : Controller
{
    private readonly ApplicationDbContext _context;

    public InvoicesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: INVOICES
    public async Task<IActionResult> Index()    
    {
        //return View(await _context.Invoices.ToListAsync());
        return View(await _context.Invoices
    .Include(i => i.JobOrder)
    .ToListAsync());
    }

    // GET: INVOICES/Details/5
    public async Task<IActionResult> Details(int? id)
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

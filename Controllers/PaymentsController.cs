
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
[SessionAuthorize]


public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public PaymentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: PAYMENTS
    //public async Task<IActionResult> Index()    
    //{
    //    return View(await _context.Payments.ToListAsync());
    //}
    public async Task<IActionResult> Index()
    {
        return View(await _context.Payments
            .Include(p => p.Invoice)
            .ToListAsync());
    }

    // GET: PAYMENTS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(m => m.PaymentId == id);
        if (payment == null)
        {
            return NotFound();
        }

        return View(payment);
    }

    // GET: PAYMENTS/Create
    //public IActionResult Create()
    //{
    //    return View();
    //}
    public IActionResult Create()
    {
        //ViewBag.Invoices = new SelectList(
        //    _context.Invoices,
        //    "InvoiceId",
        //    "InvoiceNumber");

        //return View();
        ViewBag.Invoices = new SelectList(
    _context.Invoices
        .Where(i => i.PaymentStatus != "Paid"),
    "InvoiceId",
    "InvoiceNumber");
        return View();
    }

    // POST: PAYMENTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("InvoiceId,AmountPaid,PaymentMethod,TransactionReference,Notes")] Payment payment)
    {
        ModelState.Remove("Invoice");
        ModelState.Remove("PaymentDate");
        if (payment.AmountPaid <= 0)
        {
            ModelState.AddModelError("", "Payment amount must be greater than zero.");
        }
        if (ModelState.IsValid)
        {
            payment.PaymentDate = DateTime.Now;
            var invoice = await _context.Invoices
    .Include(i => i.Payments)
    .FirstOrDefaultAsync(i => i.InvoiceId == payment.InvoiceId);
            if (invoice == null)
            {
                return NotFound();
            }
            if (invoice.PaymentStatus == "Paid")
            {
                ModelState.AddModelError("", "This invoice is already fully paid.");

                ViewBag.Invoices = new SelectList(
                    _context.Invoices
                        .Where(i => i.PaymentStatus != "Paid"),
                    "InvoiceId",
                    "InvoiceNumber",
                    payment.InvoiceId);

                return View(payment);
            }

            if (invoice == null)
            {
                return NotFound();
            }

            var totalPaid = invoice.Payments.Sum(p => p.AmountPaid);

            if (totalPaid + payment.AmountPaid > invoice.GrandTotal)
            {
                ModelState.AddModelError("", "Payment exceeds invoice balance.");

                ViewBag.Invoices = new SelectList(
                    _context.Invoices,
                    "InvoiceId",
                    "InvoiceNumber",
                    payment.InvoiceId);

                return View(payment);
            }
            _context.Add(payment);
            await _context.SaveChangesAsync();
            totalPaid += payment.AmountPaid;

            if (totalPaid <= 0)
            {
                invoice.PaymentStatus = "Unpaid";
            }
            else if (totalPaid < invoice.GrandTotal)
            {
                invoice.PaymentStatus = "Partially Paid";
            }
            else
            {
                invoice.PaymentStatus = "Paid";
            }

            _context.Update(invoice);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(payment);
    }

    // GET: PAYMENTS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
        {
            return NotFound();
        }
        ViewBag.Invoices = new SelectList(
    _context.Invoices,
    "InvoiceId",
    "InvoiceNumber",
    payment.InvoiceId);

        return View(payment);
    }

    // POST: PAYMENTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("PaymentId,InvoiceId,AmountPaid,PaymentMethod,TransactionReference,Notes")] Payment payment)
    {
        if (id != payment.PaymentId)
        {
            return NotFound();
        }
        var existingPayment = await _context.Payments
    .AsNoTracking()
    .FirstOrDefaultAsync(p => p.PaymentId == id);

        if (existingPayment == null)
        {
            return NotFound();
        }

        payment.PaymentDate = existingPayment.PaymentDate;

        ModelState.Remove("Invoice");
        ModelState.Remove("PaymentDate");

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(payment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaymentExists(payment.PaymentId))
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
        ViewBag.Invoices = new SelectList(
    _context.Invoices,
    "InvoiceId",
    "InvoiceNumber",
    payment.InvoiceId);

        return View(payment);
    }

    // GET: PAYMENTS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(m => m.PaymentId == id);
        if (payment == null)
        {
            return NotFound();
        }

        return View(payment);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var payment = await _context.Payments.FindAsync(id);

        if (payment == null)
        {
            return NotFound();
        }

        int invoiceId = payment.InvoiceId;

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();

        var invoice = await _context.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        if (invoice != null)
        {
            decimal totalPaid = invoice.Payments.Sum(p => p.AmountPaid);

            if (totalPaid <= 0)
            {
                invoice.PaymentStatus = "Unpaid";
            }
            else if (totalPaid < invoice.GrandTotal)
            {
                invoice.PaymentStatus = "Partially Paid";
            }
            else
            {
                invoice.PaymentStatus = "Paid";
            }

            _context.Update(invoice);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
    // POST: PAYMENTS/Delete/5
    //[HttpPost, ActionName("Delete")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> DeleteConfirmed(int? id)
    //{
    //    var payment = await _context.Payments.FindAsync(id);
    //    if (payment != null)
    //    {
    //        _context.Payments.Remove(payment);
    //    }

    //    await _context.SaveChangesAsync();
    //    return RedirectToAction(nameof(Index));
    //}

    private bool PaymentExists(int? id)
    {
        return _context.Payments.Any(e => e.PaymentId == id);
    }

}

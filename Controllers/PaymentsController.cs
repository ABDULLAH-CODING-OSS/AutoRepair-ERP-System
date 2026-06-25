
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
[RoleAuthorize("Admin","Owner","Service Advisor")]


public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.NotificationService _notifications;
    private readonly AutoRepairERD.Services.AuditService _auditService;

    public PaymentsController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notifications, AutoRepairERD.Services.AuditService auditService)
    {
        _context = context;
        _notifications = notifications;
        _auditService = auditService;
    }

    // GET: PAYMENTS
    //public async Task<IActionResult> Index()    
    //{
    //    return View(await _context.Payments.ToListAsync());
    //}
    public async Task<IActionResult> Index(string q)
    {
        var query = _context.Payments
            .Include(p => p.Invoice)
            .ThenInclude(i => i.JobOrder)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p => (p.TransactionReference ?? "").Contains(q) || (p.PaymentMethod ?? "").Contains(q) || (p.Invoice != null && (p.Invoice.InvoiceNumber ?? "").Contains(q)));
            ViewBag.SearchQuery = q;
        }

        return View(await query.ToListAsync());
    }

    // GET: PAYMENTS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var payment = await _context.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.JobOrder)
                    .ThenInclude(j => j.Customer)
            .Include(p => p.Invoice)
                .ThenInclude(i => i.JobOrder)
                    .ThenInclude(j => j.Vehicle)
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
    public IActionResult Create(int? invoiceId)
    {
        ViewBag.Invoices = new SelectList(
            _context.Invoices
                .Where(i => i.PaymentStatus != "Paid"),
            "InvoiceId",
            "InvoiceNumber",
            invoiceId);

        var model = new Payment();
        if (invoiceId.HasValue)
        {
            model.InvoiceId = invoiceId.Value;
        }
        return View(model);
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
            // Round amount to 2 dp and persist the exact value the user entered
            payment.AmountPaid = Decimal.Round(payment.AmountPaid, 2, MidpointRounding.AwayFromZero);
            payment.PaymentDate = DateTime.Now;

            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceId == payment.InvoiceId);
            if (invoice == null) return NotFound();

            if (invoice.PaymentStatus == "Paid")
            {
                ModelState.AddModelError(string.Empty, "This invoice is already fully paid.");
                ViewBag.Invoices = new SelectList(_context.Invoices.Where(i => i.PaymentStatus != "Paid"), "InvoiceId", "InvoiceNumber", payment.InvoiceId);
                return View(payment);
            }

            // Persist payment (rounded) first, then reload invoice totals from DB to use stored values exactly.
            _context.Add(payment);
            await _context.SaveChangesAsync();

            // Audit log - Payment recorded
            await _auditService.LogCreateAsync("Payments", payment.PaymentId, $"Payment {payment.AmountPaid:C} for Invoice {payment.InvoiceId}");

            invoice = await _context.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.InvoiceId == payment.InvoiceId);
            if (invoice == null) return NotFound();

            var totalPaidRaw = invoice.Payments.Sum(p => p.AmountPaid);
            var totalPaid = Decimal.Round(totalPaidRaw, 2, MidpointRounding.AwayFromZero);
            var grand = Decimal.Round(invoice.GrandTotal, 2, MidpointRounding.AwayFromZero);

            if (totalPaid <= 0m) invoice.PaymentStatus = "Unpaid";
            else if (totalPaid < grand) invoice.PaymentStatus = "Partially Paid";
            else invoice.PaymentStatus = "Paid";

            _context.Update(invoice);
            await _context.SaveChangesAsync();

            // Audit log - Invoice status updated
            await _auditService.LogCustomActionAsync("Invoices", invoice.InvoiceId, "Payment Received", $"Status: {invoice.PaymentStatus}, Amount Paid: {totalPaid:C}");

            // Notification: Payment Received
            try
            {
                // Notify owner/admin role
                var ownerRole = _context.Roles.FirstOrDefault(r => r.RoleName == "Owner");
                var adminRole = _context.Roles.FirstOrDefault(r => r.RoleName == "Admin");
                if (ownerRole != null) await _notifications.CreateForRoleAsync(ownerRole.RoleId, "PaymentReceived", "Payment received", $"Payment of {payment.AmountPaid:C} received for Invoice {invoice.InvoiceNumber}.", HttpContext.Session.GetInt32("UserID"));
                if (adminRole != null) await _notifications.CreateForRoleAsync(adminRole.RoleId, "PaymentReceived", "Payment received", $"Payment of {payment.AmountPaid:C} received for Invoice {invoice.InvoiceNumber}.", HttpContext.Session.GetInt32("UserID"));

                // Notify invoice creator if exists
                if (invoice.CreatedByUserId.HasValue)
                {
                    await _notifications.CreateForUserAsync(invoice.CreatedByUserId.Value, "PaymentReceived", "Payment received", $"Payment of {payment.AmountPaid:C} received for Invoice {invoice.InvoiceNumber}.", HttpContext.Session.GetInt32("UserID"));
                }
            }
            catch { }

            return RedirectToAction(nameof(Index));
        }
        ViewBag.Invoices = new SelectList(
            _context.Invoices
                .Where(i => i.PaymentStatus != "Paid"),
            "InvoiceId",
            "InvoiceNumber",
            payment.InvoiceId);
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
        var paymentAmount = payment.AmountPaid;

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();

        // Audit log - Payment deleted
        await _auditService.LogDeleteAsync("Payments", (int)id, $"Payment {paymentAmount:C} for Invoice {invoiceId}");

        var invoice = await _context.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        if (invoice != null)
        {
            var totalPaidRaw = invoice.Payments.Sum(p => p.AmountPaid);
            var totalPaid = Decimal.Round(totalPaidRaw, 2, MidpointRounding.AwayFromZero);
            var grand = Decimal.Round(invoice.GrandTotal, 2, MidpointRounding.AwayFromZero);

            if (totalPaid <= 0m)
            {
                invoice.PaymentStatus = "Unpaid";
            }
            else if (totalPaid < grand)
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


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public PaymentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: PAYMENTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Payments.ToListAsync());
    }

    // GET: PAYMENTS/Details/5
    public async Task<IActionResult> Details(int? paymentid)
    {
        if (paymentid == null)
        {
            return NotFound();
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(m => m.PaymentId == paymentid);
        if (payment == null)
        {
            return NotFound();
        }

        return View(payment);
    }

    // GET: PAYMENTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: PAYMENTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PaymentId,InvoiceId,PaymentDate,AmountPaid,PaymentMethod,TransactionReference,Notes,Invoice")] Payment payment)
    {
        if (ModelState.IsValid)
        {
            _context.Add(payment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(payment);
    }

    // GET: PAYMENTS/Edit/5
    public async Task<IActionResult> Edit(int? paymentid)
    {
        if (paymentid == null)
        {
            return NotFound();
        }

        var payment = await _context.Payments.FindAsync(paymentid);
        if (payment == null)
        {
            return NotFound();
        }
        return View(payment);
    }

    // POST: PAYMENTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? paymentid, [Bind("PaymentId,InvoiceId,PaymentDate,AmountPaid,PaymentMethod,TransactionReference,Notes,Invoice")] Payment payment)
    {
        if (paymentid != payment.PaymentId)
        {
            return NotFound();
        }

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
        return View(payment);
    }

    // GET: PAYMENTS/Delete/5
    public async Task<IActionResult> Delete(int? paymentid)
    {
        if (paymentid == null)
        {
            return NotFound();
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(m => m.PaymentId == paymentid);
        if (payment == null)
        {
            return NotFound();
        }

        return View(payment);
    }

    // POST: PAYMENTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? paymentid)
    {
        var payment = await _context.Payments.FindAsync(paymentid);
        if (payment != null)
        {
            _context.Payments.Remove(payment);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool PaymentExists(int? paymentid)
    {
        return _context.Payments.Any(e => e.PaymentId == paymentid);
    }
}

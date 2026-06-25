
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;

[RoleAuthorize("Admin","Owner")]
public class SalaryAdjustmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SalaryAdjustmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: SALARYADJUSTMENTS
    public async Task<IActionResult> Index(int? payrollId, string q)
    {
        var query = _context.SalaryAdjustments
            .Include(sa => sa.Payroll)
                .ThenInclude(p => p.Employee)
            .AsQueryable();

        if (payrollId.HasValue)
        {
            query = query.Where(sa => sa.PayrollId == payrollId.Value);
            ViewBag.PayrollId = payrollId.Value;
            var payroll = await _context.Payrolls.Include(p=>p.Employee).FirstOrDefaultAsync(p=>p.PayrollId == payrollId.Value);
            if (payroll != null) ViewBag.PayrollLabel = payroll.PayrollNumber + " — " + (payroll.Employee != null ? (payroll.Employee.FirstName + " " + (payroll.Employee.LastName ?? "")) : "");
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(sa => (sa.AdjustmentType ?? "").Contains(q) || (sa.Reason ?? "").Contains(q));
            ViewBag.SearchQuery = q;
        }

        var list = await query.ToListAsync();
        return View(list);
    }

    // GET: SALARYADJUSTMENTS/Details/5
    public async Task<IActionResult> Details(int? adjustmentid)
    {
        if (adjustmentid == null)
        {
            return NotFound();
        }

        var salaryadjustment = await _context.SalaryAdjustments
            .Include(sa => sa.Payroll)
            .FirstOrDefaultAsync(m => m.AdjustmentId == adjustmentid);
        if (salaryadjustment == null)
        {
            return NotFound();
        }

        return View(salaryadjustment);
    }

    // GET: SALARYADJUSTMENTS/Create
    public async Task<IActionResult> Create(int? payrollId)
    {
        var model = new SalaryAdjustment();
        if (payrollId.HasValue) model.PayrollId = payrollId.Value;

        // Pass adjustment types to view
        ViewBag.AdjustmentTypes = AutoRepairERD.Helpers.AdjustmentTypeHelper.GetAdjustmentTypes();
        ViewBag.PayrollId = payrollId;

        // Pass unpaid payrolls for dropdown selection
        var unpaidPayrolls = await _context.Payrolls
            .Include(p => p.Employee)
            .Where(p => p.PayrollStatus != "Paid")
            .OrderByDescending(p => p.PayrollId)
            .ToListAsync();
        ViewBag.Payrolls = unpaidPayrolls;

        return View(model);
    }

    // POST: SALARYADJUSTMENTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
    [Bind("AdjustmentId,PayrollId,AdjustmentType,Amount,Reason")]
    SalaryAdjustment salaryadjustment)
    {
        if (ModelState.IsValid)
        {
            _context.Add(salaryadjustment);
            await _context.SaveChangesAsync();
            if (salaryadjustment.PayrollId != 0)
            {
                return RedirectToAction("Details", "Payrolls", new { payrollid = salaryadjustment.PayrollId });
            }
            return RedirectToAction(nameof(Index));
        }
        // If validation fails, return to form with errors
        ViewBag.AdjustmentTypes = AutoRepairERD.Helpers.AdjustmentTypeHelper.GetAdjustmentTypes();
        ViewBag.PayrollId = salaryadjustment.PayrollId;

        // Pass unpaid payrolls for dropdown selection
        var unpaidPayrolls = await _context.Payrolls
            .Include(p => p.Employee)
            .Where(p => p.PayrollStatus != "Paid")
            .OrderByDescending(p => p.PayrollId)
            .ToListAsync();
        ViewBag.Payrolls = unpaidPayrolls;

        return View(salaryadjustment);
    }

    // GET: SALARYADJUSTMENTS/Edit/5
    public async Task<IActionResult> Edit(int? adjustmentid)
    {
        if (adjustmentid == null)
        {
            return NotFound();
        }

        var salaryadjustment = await _context.SalaryAdjustments.FindAsync(adjustmentid);
        if (salaryadjustment == null)
        {
            return NotFound();
        }

        // Pass adjustment types to view
        ViewBag.AdjustmentTypes = AutoRepairERD.Helpers.AdjustmentTypeHelper.GetAdjustmentTypes();
        ViewBag.PayrollId = salaryadjustment.PayrollId;

        return View(salaryadjustment);
    }

    // POST: SALARYADJUSTMENTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? adjustmentid, [Bind("AdjustmentId,PayrollId,AdjustmentType,Amount,Reason")] SalaryAdjustment salaryadjustment)
    {
        if (adjustmentid != salaryadjustment.AdjustmentId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(salaryadjustment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SalaryAdjustmentExists(salaryadjustment.AdjustmentId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            // Redirect back to Payroll Details if coming from payroll context
            if (salaryadjustment.PayrollId != 0)
            {
                return RedirectToAction("Details", "Payrolls", new { payrollid = salaryadjustment.PayrollId });
            }
            return RedirectToAction(nameof(Index));
        }
        // If validation fails, return to form with errors
        ViewBag.AdjustmentTypes = AutoRepairERD.Helpers.AdjustmentTypeHelper.GetAdjustmentTypes();
        ViewBag.PayrollId = salaryadjustment.PayrollId;
        return View(salaryadjustment);
    }

    // GET: SALARYADJUSTMENTS/Delete/5
    public async Task<IActionResult> Delete(int? adjustmentid)
    {
        if (adjustmentid == null)
        {
            return NotFound();
        }

        var salaryadjustment = await _context.SalaryAdjustments
            .Include(sa => sa.Payroll)
            .FirstOrDefaultAsync(m => m.AdjustmentId == adjustmentid);
        if (salaryadjustment == null)
        {
            return NotFound();
        }

        return View(salaryadjustment);
    }

    // POST: SALARYADJUSTMENTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? adjustmentid)
    {
        var salaryadjustment = await _context.SalaryAdjustments.FindAsync(adjustmentid);
        int payrollId = 0;

        if (salaryadjustment != null)
        {
            payrollId = salaryadjustment.PayrollId;
            _context.SalaryAdjustments.Remove(salaryadjustment);
        }

        await _context.SaveChangesAsync();

        // Redirect back to Payroll Details if coming from payroll context
        if (payrollId != 0)
        {
            return RedirectToAction("Details", "Payrolls", new { payrollid = payrollId });
        }
        return RedirectToAction(nameof(Index));
    }

    private bool SalaryAdjustmentExists(int? adjustmentid)
    {
        return _context.SalaryAdjustments.Any(e => e.AdjustmentId == adjustmentid);
    }
}

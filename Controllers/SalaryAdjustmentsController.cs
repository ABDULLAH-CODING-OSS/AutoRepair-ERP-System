
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
    public async Task<IActionResult> Index()    
    {
        return View(await _context.SalaryAdjustments.ToListAsync());
    }

    // GET: SALARYADJUSTMENTS/Details/5
    public async Task<IActionResult> Details(int? adjustmentid)
    {
        if (adjustmentid == null)
        {
            return NotFound();
        }

        var salaryadjustment = await _context.SalaryAdjustments
            .FirstOrDefaultAsync(m => m.AdjustmentId == adjustmentid);
        if (salaryadjustment == null)
        {
            return NotFound();
        }

        return View(salaryadjustment);
    }

    // GET: SALARYADJUSTMENTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: SALARYADJUSTMENTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AdjustmentId,PayrollId,AdjustmentType,Amount,Reason,Payroll")] SalaryAdjustment salaryadjustment)
    {
        if (ModelState.IsValid)
        {
            _context.Add(salaryadjustment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
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
        return View(salaryadjustment);
    }

    // POST: SALARYADJUSTMENTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? adjustmentid, [Bind("AdjustmentId,PayrollId,AdjustmentType,Amount,Reason,Payroll")] SalaryAdjustment salaryadjustment)
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
            return RedirectToAction(nameof(Index));
        }
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
        if (salaryadjustment != null)
        {
            _context.SalaryAdjustments.Remove(salaryadjustment);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool SalaryAdjustmentExists(int? adjustmentid)
    {
        return _context.SalaryAdjustments.Any(e => e.AdjustmentId == adjustmentid);
    }
}

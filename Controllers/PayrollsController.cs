
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
[RoleAuthorize("Admin","Owner")]
public class PayrollsController : Controller
{
    private readonly ApplicationDbContext _context;

    public PayrollsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: PAYROLLS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Payrolls.ToListAsync());
    }

    // GET: PAYROLLS/Details/5
    public async Task<IActionResult> Details(int? payrollid)
    {
        if (payrollid == null)
        {
            return NotFound();
        }

        var payroll = await _context.Payrolls
            .FirstOrDefaultAsync(m => m.PayrollId == payrollid);
        if (payroll == null)
        {
            return NotFound();
        }

        return View(payroll);
    }

    // GET: PAYROLLS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: PAYROLLS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PayrollId,EmployeeId,PayrollMonth,PayrollYear,TotalWorkingDays,TotalPresentDays,OvertimeHours,BonusAmount,DeductionAmount,GrossSalary,NetSalary,PaymentDate,PayrollStatus,Employee,SalaryAdjustments")] Payroll payroll)
    {
        if (ModelState.IsValid)
        {
            _context.Add(payroll);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(payroll);
    }

    // GET: PAYROLLS/Edit/5
    public async Task<IActionResult> Edit(int? payrollid)
    {
        if (payrollid == null)
        {
            return NotFound();
        }

        var payroll = await _context.Payrolls.FindAsync(payrollid);
        if (payroll == null)
        {
            return NotFound();
        }
        return View(payroll);
    }

    // POST: PAYROLLS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? payrollid, [Bind("PayrollId,EmployeeId,PayrollMonth,PayrollYear,TotalWorkingDays,TotalPresentDays,OvertimeHours,BonusAmount,DeductionAmount,GrossSalary,NetSalary,PaymentDate,PayrollStatus,Employee,SalaryAdjustments")] Payroll payroll)
    {
        if (payrollid != payroll.PayrollId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(payroll);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PayrollExists(payroll.PayrollId))
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
        return View(payroll);
    }

    // GET: PAYROLLS/Delete/5
    public async Task<IActionResult> Delete(int? payrollid)
    {
        if (payrollid == null)
        {
            return NotFound();
        }

        var payroll = await _context.Payrolls
            .FirstOrDefaultAsync(m => m.PayrollId == payrollid);
        if (payroll == null)
        {
            return NotFound();
        }

        return View(payroll);
    }

    // POST: PAYROLLS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? payrollid)
    {
        var payroll = await _context.Payrolls.FindAsync(payrollid);
        if (payroll != null)
        {
            _context.Payrolls.Remove(payroll);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool PayrollExists(int? payrollid)
    {
        return _context.Payrolls.Any(e => e.PayrollId == payrollid);
    }
}

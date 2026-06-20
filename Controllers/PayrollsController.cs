
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using AutoRepairERD.Services;
[RoleAuthorize("Admin","Owner")]
public class PayrollsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PayrollCalculationService _payrollCalc;
    private readonly NotificationService _notifications;

    public PayrollsController(ApplicationDbContext context, PayrollCalculationService payrollCalc, NotificationService notifications)
    {
        _context = context;
        _payrollCalc = payrollCalc;
        _notifications = notifications;
    }

    // GET: PAYROLLS
    public async Task<IActionResult> Index()    
    {
        var list = await _context.Payrolls
            .Include(p => p.Employee)
            .OrderByDescending(p => p.PayrollYear).ThenByDescending(p => p.PayrollMonth)
            .ToListAsync();
        return View(list);
    }

    // GET: PAYROLLS/Details/5
    public async Task<IActionResult> Details(int? payrollid)
    {
        if (payrollid == null)
        {
            return NotFound();
        }

        var payroll = await _context.Payrolls
            .Include(p => p.Employee).ThenInclude(e => e.User)
            .Include(p => p.SalaryAdjustments)
            .FirstOrDefaultAsync(m => m.PayrollId == payrollid);
        if (payroll == null)
        {
            return NotFound();
        }

        return View(payroll);
    }

    // POST: PAYROLLS/Recalculate/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recalculate(int? payrollid, int? manualPresentDays)
    {
        if (payrollid == null) return NotFound();
        var payroll = await _context.Payrolls.FindAsync(payrollid);
        if (payroll == null) return NotFound();
        if (payroll.PayrollStatus == "Paid")
        {
            TempData["Toast"] = "Paid payrolls cannot be recalculated.";
            TempData["ToastType"] = "warning";
            return RedirectToAction(nameof(Edit), new { payrollid = payrollid });
        }

        var result = await _payrollCalc.RecalculatePayrollAsync(payroll, manualPresentDays);
        if (!result.Success)
        {
            TempData["Toast"] = result.Error ?? "Recalculation failed.";
            TempData["ToastType"] = "danger";
            return RedirectToAction(nameof(Edit), new { payrollid = payrollid });
        }

        // If user provided manualPresentDays and it differs significantly from computed attendance, warn
        if (manualPresentDays.HasValue && result.ComputedPresentDays.HasValue)
        {
            var computed = result.ComputedPresentDays.Value;
            var manual = Math.Clamp(manualPresentDays.Value, 0, payroll.TotalWorkingDays ?? computed);
            // Difference threshold: more than 2 days or more than 20% of working days
            var diff = Math.Abs(manual - computed);
            var threshold = Math.Max(2, (int)Math.Ceiling((payroll.TotalWorkingDays ?? computed) * 0.2));
            if (diff > threshold)
            {
                TempData["Toast"] = "Warning: manual present days differ significantly from attendance records.";
                TempData["ToastType"] = "warning";
            }
        }

        try
        {
            _context.Update(payroll);
            await _context.SaveChangesAsync();

            // Audit
            var audit = new AuditLog
            {
                UserId = HttpContext.Session.GetInt32("UserID"),
                TableName = "Payrolls",
                RecordId = payroll.PayrollId,
                ActionType = "Payroll Recalculated",
                OldValues = null,
                NewValues = $"Net={payroll.NetSalary};Gross={payroll.GrossSalary}",
                ActionDate = DateTime.Now,
                Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // swallow audit exceptions
        }

        // Notify Employee, Admin, Owner about recalculation
        try
        {
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            var ownerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Owner");
            var employee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.EmployeeId == payroll.EmployeeId);
            var name = employee?.FirstName + (string.IsNullOrEmpty(employee?.LastName) ? "" : " " + employee?.LastName);
            var title = "Payroll recalculated";
            var messageForEmployee = $"Your payroll for {System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(payroll.PayrollMonth ?? 0)} {payroll.PayrollYear} has been recalculated.";
            var message = $"Payroll recalculated for {name ?? "Unknown"} - {System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(payroll.PayrollMonth ?? 0)}/{payroll.PayrollYear}.";
            if (adminRole != null)
                await _notifications.CreateForRoleAsync(adminRole.RoleId, "Payroll", title, message, HttpContext.Session.GetInt32("UserID"));
            if (ownerRole != null)
                await _notifications.CreateForRoleAsync(ownerRole.RoleId, "Payroll", title, message, HttpContext.Session.GetInt32("UserID"));
            try
            {
                if (employee?.User != null && employee.User.UserId != 0)
                {
                    await _notifications.CreateForUserAsync(employee.User.UserId, "Payroll", title, messageForEmployee, HttpContext.Session.GetInt32("UserID"));
                }
            }
            catch { }
        }
        catch { }

        TempData["Toast"] = "Payroll recalculated.";
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Details), new { payrollid = payrollid });
    }

    // POST: PAYROLLS/MarkPaid/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(int? payrollid)
    {
        if (payrollid == null) return NotFound();
        var payroll = await _context.Payrolls.FindAsync(payrollid);
        if (payroll == null) return NotFound();

        if (payroll.PayrollStatus == "Paid")
        {
            TempData["Toast"] = "Payroll is already marked as Paid.";
            TempData["ToastType"] = "info";
            return RedirectToAction(nameof(Details), new { payrollid = payrollid });
        }

        // Only allow marking Paid from non-cancelled states
        if (payroll.PayrollStatus == "Cancelled")
        {
            TempData["Toast"] = "Cannot mark a cancelled payroll as Paid.";
            TempData["ToastType"] = "danger";
            return RedirectToAction(nameof(Details), new { payrollid = payrollid });
        }

        payroll.PayrollStatus = "Paid";
        // Ensure PaymentDate is recorded and persisted
        payroll.PaymentDate = DateOnly.FromDateTime(DateTime.Now);
        _context.Update(payroll);
        await _context.SaveChangesAsync();

        try
        {
            var audit = new AuditLog
            {
                UserId = HttpContext.Session.GetInt32("UserID"),
                TableName = "Payrolls",
                RecordId = payroll.PayrollId,
                ActionType = "Payroll Paid",
                OldValues = null,
                NewValues = $"Status=Paid;PaymentDate={payroll.PaymentDate}",
                ActionDate = DateTime.Now,
                Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }
        catch { }

        // Notify Employee, Owner, Admin about payment
        try
        {
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            var ownerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Owner");
            var employee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.EmployeeId == payroll.EmployeeId);
            var name = employee?.FirstName + (string.IsNullOrEmpty(employee?.LastName) ? "" : " " + employee?.LastName);
            var title = "Payroll paid";
            var messageForEmployee = $"Your payroll for {System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(payroll.PayrollMonth ?? 0)} {payroll.PayrollYear} has been marked as paid.";
            var message = $"Payroll paid for {name ?? "Unknown"} - {System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(payroll.PayrollMonth ?? 0)}/{payroll.PayrollYear}.";
            if (adminRole != null)
                await _notifications.CreateForRoleAsync(adminRole.RoleId, "Payroll", title, message, HttpContext.Session.GetInt32("UserID"));
            if (ownerRole != null)
                await _notifications.CreateForRoleAsync(ownerRole.RoleId, "Payroll", title, message, HttpContext.Session.GetInt32("UserID"));
            try
            {
                if (employee?.User != null && employee.User.UserId != 0)
                {
                    await _notifications.CreateForUserAsync(employee.User.UserId, "Payroll", title, messageForEmployee, HttpContext.Session.GetInt32("UserID"));
                }
            }
            catch { }
        }
        catch { }

        TempData["Toast"] = "Payroll marked as Paid.";
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Details), new { payrollid = payrollid });
    }

    // GET: PAYROLLS/Generate
    public IActionResult Generate()
    {
        // Provide employee selector in the view
        ViewBag.Employees = _context.Employees
            .Select(e => new { e.EmployeeId, Display = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName })
            .ToList();
        return View();
    }

    // POST: PAYROLLS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int EmployeeId, int PayrollMonth, int PayrollYear)
    {
        // Server-side validation
        if (EmployeeId <= 0) ModelState.AddModelError("EmployeeId", "Employee is required.");
        if (PayrollMonth < 1 || PayrollMonth > 12) ModelState.AddModelError("PayrollMonth", "Month must be between 1 and 12.");
        if (PayrollYear < 2000 || PayrollYear > 2100) ModelState.AddModelError("PayrollYear", "Year is invalid.");

        if (!ModelState.IsValid)
        {
            ViewBag.Employees = _context.Employees
                .Select(e => new { e.EmployeeId, Display = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName })
                .ToList();
            ViewBag.SelectedMonth = PayrollMonth;
            ViewBag.SelectedEmployee = EmployeeId > 0 ? EmployeeId : (int?)null;
            ViewBag.SelectedYear = PayrollYear;
            return View();
        }

        var calc = await _payrollCalc.CalculatePayrollAsync(EmployeeId, PayrollMonth, PayrollYear);
        if (!calc.Success)
        {
            ModelState.AddModelError(string.Empty, calc.Error ?? "Unable to calculate payroll.");
            ViewBag.Employees = _context.Employees
                .Select(e => new { e.EmployeeId, Display = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName })
                .ToList();
            ViewBag.SelectedMonth = PayrollMonth;
            ViewBag.SelectedEmployee = EmployeeId > 0 ? EmployeeId : (int?)null;
            ViewBag.SelectedYear = PayrollYear;
            return View();
        }

        // Persist payroll
        var payroll = calc.Payroll!;
        try
        {
            _context.Payrolls.Add(payroll);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Surface DB errors to user
            ModelState.AddModelError(string.Empty, "Unable to save payroll: " + ex.Message);
            ViewBag.Employees = _context.Employees
                .Select(e => new { e.EmployeeId, Display = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName })
                .ToList();
            ViewBag.SelectedMonth = PayrollMonth;
            ViewBag.SelectedYear = PayrollYear;
            return View();
        }

        // Audit log: Payroll Generated
        try
        {
            var audit = new AuditLog
            {
                UserId = HttpContext.Session.GetInt32("UserID"),
                TableName = "Payrolls",
                RecordId = payroll.PayrollId,
                ActionType = "Payroll Generated",
                OldValues = null,
                NewValues = $"PayrollNumber={payroll.PayrollNumber};EmployeeId={payroll.EmployeeId};Month={payroll.PayrollMonth};Year={payroll.PayrollYear};Net={payroll.NetSalary}",
                ActionDate = DateTime.Now,
                Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }
        catch { }

        // Notification: Payroll generated to Employee, Admin and Owner
        try
        {
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            var ownerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Owner");
            var employee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.EmployeeId == payroll.EmployeeId);
            var name = employee?.FirstName + (string.IsNullOrEmpty(employee?.LastName) ? "" : " " + employee?.LastName);
            var title = "Payroll generated";
            var messageForEmployee = $"Your payroll for {System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(payroll.PayrollMonth ?? 0)} {payroll.PayrollYear} has been generated.";
            var message = $"Payroll generated for {name ?? "Unknown"} - {System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(payroll.PayrollMonth ?? 0)}/{payroll.PayrollYear}.";
            if (adminRole != null)
                await _notifications.CreateForRoleAsync(adminRole.RoleId, "Payroll", title, message, HttpContext.Session.GetInt32("UserID"));
            if (ownerRole != null)
                await _notifications.CreateForRoleAsync(ownerRole.RoleId, "Payroll", title, message, HttpContext.Session.GetInt32("UserID"));
            try
            {
                if (employee?.User != null && employee.User.UserId != 0)
                {
                    await _notifications.CreateForUserAsync(employee.User.UserId, "Payroll", title, messageForEmployee, HttpContext.Session.GetInt32("UserID"));
                }
            }
            catch { }
        }
        catch { }

        return RedirectToAction(nameof(Index));
    }

    // GET: PAYROLLS/Edit/5
    public async Task<IActionResult> Edit(int? payrollid)
    {
        if (payrollid == null)
        {
            return NotFound();
        }

        var payroll = await _context.Payrolls
            .Include(p => p.SalaryAdjustments)
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.PayrollId == payrollid);
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
    public async Task<IActionResult> Edit(int? payrollid, string? payrollStatus, DateOnly? paymentDate)
    {
        if (payrollid == null) return NotFound();

        var existing = await _context.Payrolls
            .Include(p => p.Employee)
            .Include(p => p.SalaryAdjustments)
            .FirstOrDefaultAsync(p => p.PayrollId == payrollid);
        if (existing == null) return NotFound();

        var allowed = new[] { "Pending", "Generated", "Paid", "Cancelled" };
        if (!string.IsNullOrWhiteSpace(payrollStatus) && !allowed.Contains(payrollStatus))
        {
            ModelState.AddModelError("PayrollStatus", "Invalid status.");
            return View(existing);
        }

        // If payroll already Paid, do not allow changing status or payment date
        if (existing.PayrollStatus == "Paid")
        {
            if (!string.IsNullOrWhiteSpace(payrollStatus) && payrollStatus != "Paid")
            {
                ModelState.AddModelError("PayrollStatus", "Paid payrolls cannot be modified.");
                // ensure Employee is present for view rendering
                if (existing.Employee == null)
                {
                    existing = await _context.Payrolls.Include(p => p.Employee).FirstOrDefaultAsync(p => p.PayrollId == payrollid);
                }
                return View(existing);
            }
            // keep existing values
        }
        else
        {
            // If changing to Paid, set PaymentDate automatically
            if (!string.IsNullOrWhiteSpace(payrollStatus) && payrollStatus == "Paid")
            {
                existing.PayrollStatus = "Paid";
                existing.PaymentDate = DateOnly.FromDateTime(DateTime.Now);
            }
            else if (!string.IsNullOrWhiteSpace(payrollStatus))
            {
                existing.PayrollStatus = payrollStatus;
            }
        }

        if (!ModelState.IsValid) return View(existing);

        try
        {
            _context.Update(existing);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PayrollExists(existing.PayrollId)) return NotFound(); else throw;
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: PAYROLLS/Delete/5
    public async Task<IActionResult> Delete(int? payrollid)
    {
        if (payrollid == null)
        {
            return NotFound();
        }
        var payroll = await _context.Payrolls
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(m => m.PayrollId == payrollid);
        if (payroll == null) return NotFound();
        return View(payroll);
    }

    // POST: PAYROLLS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? payrollid)
    {
        var payroll = await _context.Payrolls.FindAsync(payrollid);
        if (payroll == null) return NotFound();

        // Business rule: Paid payrolls cannot be cancelled or deleted.
        if (payroll.PayrollStatus == "Paid")
        {
            TempData["Toast"] = "Paid payrolls cannot be cancelled.";
            TempData["ToastType"] = "danger";
            return RedirectToAction(nameof(Details), new { payrollid = payrollid });
        }

        // Soft-cancel: mark payroll as Cancelled so it remains in DB but is not active
        payroll.PayrollStatus = "Cancelled";
        _context.Update(payroll);
        await _context.SaveChangesAsync();

        // Audit
        try
        {
            var audit = new AuditLog
            {
                UserId = HttpContext.Session.GetInt32("UserID"),
                TableName = "Payrolls",
                RecordId = payroll.PayrollId,
                ActionType = "Payroll Cancelled",
                OldValues = null,
                NewValues = $"Status=Cancelled",
                ActionDate = DateTime.Now,
                Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }
        catch { }

        // Notify Employee, Admin, Owner about cancellation
        try
        {
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            var ownerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Owner");
            var employee = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.EmployeeId == payroll.EmployeeId);
            var name = employee?.FirstName + (string.IsNullOrEmpty(employee?.LastName) ? "" : " " + employee?.LastName);
            var title = "Payroll cancelled";
            var messageForEmployee = $"Your payroll for {System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(payroll.PayrollMonth ?? 0)} {payroll.PayrollYear} has been cancelled.";
            var message = $"Payroll cancelled for {name ?? "Unknown"} - {System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(payroll.PayrollMonth ?? 0)}/{payroll.PayrollYear}.";
            if (adminRole != null)
                await _notifications.CreateForRoleAsync(adminRole.RoleId, "Payroll", title, message, HttpContext.Session.GetInt32("UserID"));
            if (ownerRole != null)
                await _notifications.CreateForRoleAsync(ownerRole.RoleId, "Payroll", title, message, HttpContext.Session.GetInt32("UserID"));
            try
            {
                if (employee?.User != null && employee.User.UserId != 0)
                {
                    await _notifications.CreateForUserAsync(employee.User.UserId, "Payroll", title, messageForEmployee, HttpContext.Session.GetInt32("UserID"));
                }
            }
            catch { }
        }
        catch { }

        return RedirectToAction(nameof(Index));
    }

    private bool PayrollExists(int? payrollid)
    {
        return _context.Payrolls.Any(e => e.PayrollId == payrollid);
    }
}

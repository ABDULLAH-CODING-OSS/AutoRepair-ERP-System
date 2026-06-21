
using AutoRepairERD.Filters;
using AutoRepairERD.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;

[RoleAuthorize("Admin","Owner")]
public class SalaryAdjustmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.NotificationService _notifications;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SalaryAdjustmentsController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notifications, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _notifications = notifications;
        _httpContextAccessor = httpContextAccessor;
    }

    // GET: SALARYADJUSTMENTS
    public async Task<IActionResult> Index()    
    {
        var list = await _context.SalaryAdjustments
            .Include(sa => sa.Payroll).ThenInclude(p => p.Employee)
            .ToListAsync();
        return View(list);
    }

    // GET: SALARYADJUSTMENTS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var salaryadjustment = await _context.SalaryAdjustments
            .Include(sa => sa.Payroll).ThenInclude(p => p.Employee)
            .FirstOrDefaultAsync(m => m.AdjustmentId == id);
        if (salaryadjustment == null)
        {
            return NotFound();
        }
        return View(salaryadjustment);
    }

    // GET: SALARYADJUSTMENTS/Create
    public async Task<IActionResult> Create()
    {
        // Provide eligible payrolls for dropdown: only Generated payrolls
        var payrollsForDropdown = await _context.Payrolls
            .Where(p => p.PayrollStatus == "Generated")
            .Include(p => p.Employee)
            .ToListAsync();
        ViewBag.Payrolls = payrollsForDropdown
            .Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = p.PayrollId.ToString(),
                Text = (p.PayrollNumber ?? "") + " - " + (p.Employee != null ? (p.Employee.EmployeeCode + " - " + (p.Employee.FirstName + " " + (p.Employee.LastName ?? "")).Trim()) : "Unknown") + " - " + (p.PayrollMonth != null ? System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(p.PayrollMonth ?? 0) : "") + " " + (p.PayrollYear != null ? p.PayrollYear.ToString() : "")
            })
            .ToList();
        return View();
    }

    // POST: SALARYADJUSTMENTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AdjustmentId,PayrollId,AdjustmentType,Amount,Reason")] SalaryAdjustment salaryadjustment)
    {
        // Defensive parse of PayrollId from form in case binder missed it
        try
        {
            var form = _httpContextAccessor.HttpContext?.Request?.Form;
            var val = form?["PayrollId"].FirstOrDefault();
            if (!string.IsNullOrEmpty(val) && int.TryParse(val, out var parsed))
            {
                salaryadjustment.PayrollId = parsed;
                if (ModelState.ContainsKey("PayrollId")) ModelState.Remove("PayrollId");
                ModelState.SetModelValue("PayrollId", new Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderResult(parsed.ToString()));
            }
        }
        catch (Exception ex)
        {
            throw;
        }

        // Server-side validation
        if (salaryadjustment.Amount == null || salaryadjustment.Amount <= 0m)
            ModelState.AddModelError("Amount", "Amount is required and must be greater than zero.");
        if (string.IsNullOrWhiteSpace(salaryadjustment.Reason) || salaryadjustment.Reason.Length < 10)
            ModelState.AddModelError("Reason", "Reason is required and must be at least 10 characters.");
        var allowedTypes = new[] { "Bonus", "Allowance", "Deduction", "Penalty" };
        if (string.IsNullOrWhiteSpace(salaryadjustment.AdjustmentType) || !allowedTypes.Contains(salaryadjustment.AdjustmentType))
            ModelState.AddModelError("AdjustmentType", "Adjustment type is required and must be one of: Bonus, Allowance, Deduction, Penalty.");

        // Verify payroll eligibility
        var payroll = await _context.Payrolls.FindAsync(salaryadjustment.PayrollId);
        if (payroll == null)
            ModelState.AddModelError("PayrollId", "Selected payroll does not exist.");
        else if (payroll.PayrollStatus == "Paid" || payroll.PayrollStatus == "Cancelled")
            ModelState.AddModelError(string.Empty, "Salary adjustments cannot be applied to Paid or Cancelled payrolls.");

        // Ensure fields that are not posted do not cause model validation failures
        salaryadjustment.IsActive = true;
        salaryadjustment.AdjustmentStatus = "Active";
        salaryadjustment.CreatedAt = DateTime.Now;

        // Remove ModelState entries for non-posted or navigation properties so they do not block create
        ModelState.Remove(nameof(SalaryAdjustment.Payroll));
        ModelState.Remove(nameof(SalaryAdjustment.IsActive));
        ModelState.Remove(nameof(SalaryAdjustment.AdjustmentStatus));
        ModelState.Remove(nameof(SalaryAdjustment.CreatedAt));

        //if (!ModelState.IsValid)
        //{
        //    var errors = string.Join(" | ",
        //        ModelState
        //            .SelectMany(x => x.Value.Errors)
        //            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
        //    );

        //    throw new Exception("ModelState Invalid: " + errors);
        //}
        // Remove framework-generated validation noise
        ModelState.Remove(nameof(SalaryAdjustment.Payroll));
        ModelState.Remove(nameof(SalaryAdjustment.AdjustmentStatus));
        ModelState.Remove(nameof(SalaryAdjustment.IsActive));
        ModelState.Remove(nameof(SalaryAdjustment.CreatedAt));

        // Only block if OUR manual validations added errors
        bool hasRealErrors =
            ModelState["Amount"]?.Errors.Count > 0 ||
            ModelState["Reason"]?.Errors.Count > 0 ||
            ModelState["AdjustmentType"]?.Errors.Count > 0 ||
            ModelState["PayrollId"]?.Errors.Count > 0;

        if (hasRealErrors)
        {
            var payrollsForDropdown2 = await _context.Payrolls
                .Where(p => p.PayrollStatus == "Generated")
                .Include(p => p.Employee)
                .ToListAsync();

            ViewBag.Payrolls = payrollsForDropdown2
                .Select(p => new SelectListItem
                {
                    Value = p.PayrollId.ToString(),
                    Text = $"{p.PayrollNumber} - {p.Employee?.EmployeeCode} - {p.Employee?.FirstName} {p.Employee?.LastName}"
                })
                .ToList();

            return View(salaryadjustment);
        }
        // Reached Save path — preparing to save salary adjustment.
        // Initialize status and timestamps
        salaryadjustment.IsActive = true;
        salaryadjustment.AdjustmentStatus = "Active";
        salaryadjustment.CreatedAt = DateTime.Now;

        _context.Add(salaryadjustment);
        await _context.SaveChangesAsync();

        // Notifications: Employee, Admin, Owner
        try
        {
            var employee = await _context.Payrolls.Include(p => p.Employee).ThenInclude(e => e.User).FirstOrDefaultAsync(p => p.PayrollId == salaryadjustment.PayrollId);
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            var ownerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Owner");
            var title = "Salary adjustment added";
            var message = $"A salary adjustment of {salaryadjustment.Amount:C} ({salaryadjustment.AdjustmentType}) was added to payroll {payroll?.PayrollNumber}.";
            if (adminRole != null) await _notifications.CreateForRoleAsync(adminRole.RoleId, "Payroll", title, message);
            if (ownerRole != null) await _notifications.CreateForRoleAsync(ownerRole.RoleId, "Payroll", title, message);
            if (employee?.Employee?.User?.UserId != 0)
                await _notifications.CreateForUserAsync(employee.Employee.User.UserId, "Payroll", "Salary adjustment added to your payroll.", message);
        }
        catch (Exception ex)
        {
            throw;
        }

        // Audit
        try
        {
            var audit = new AuditLog
            {
                UserId = _httpContextAccessor.HttpContext?.Session.GetInt32("UserID"),
                TableName = "SalaryAdjustments",
                RecordId = salaryadjustment.AdjustmentId,
                ActionType = "Salary Adjustment Created",
                OldValues = null,
                NewValues = $"Type={salaryadjustment.AdjustmentType};Amount={salaryadjustment.Amount};Reason={salaryadjustment.Reason}",
                ActionDate = DateTime.Now,
                Ipaddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
            };
            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw;
        }

        // final: no temporary diagnostics
        return RedirectToAction(nameof(Index));
    }

    // GET: SALARYADJUSTMENTS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var salaryadjustment = await _context.SalaryAdjustments
            .Include(sa => sa.Payroll).ThenInclude(p => p.Employee)
            .FirstOrDefaultAsync(sa => sa.AdjustmentId == id);
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
    public async Task<IActionResult> Edit(int? id, [Bind("AdjustmentId,PayrollId,AdjustmentType,Amount,Reason")] SalaryAdjustment salaryadjustment)
    {
        if (id != salaryadjustment.AdjustmentId)
        {
            return NotFound();
        }

        // Do not allow changing payroll assignment
        var existing = await _context.SalaryAdjustments.Include(sa => sa.Payroll).FirstOrDefaultAsync(sa => sa.AdjustmentId == id);
        if (existing == null) return NotFound();

        // Server-side validation for editable fields
        if (salaryadjustment.Amount == null || salaryadjustment.Amount <= 0m)
            ModelState.AddModelError("Amount", "Amount is required and must be greater than zero.");
        if (string.IsNullOrWhiteSpace(salaryadjustment.Reason) || salaryadjustment.Reason.Length < 10)
            ModelState.AddModelError("Reason", "Reason is required and must be at least 10 characters.");
        var allowedTypes = new[] { "Bonus", "Allowance", "Deduction", "Penalty" };
        if (string.IsNullOrWhiteSpace(salaryadjustment.AdjustmentType) || !allowedTypes.Contains(salaryadjustment.AdjustmentType))
            ModelState.AddModelError("AdjustmentType", "Adjustment type is required and must be one of: Bonus, Allowance, Deduction, Penalty.");

        // Prevent edits if payroll is Paid or Cancelled
        var payroll = await _context.Payrolls.FindAsync(existing.PayrollId);
        if (payroll != null && (payroll.PayrollStatus == "Paid" || payroll.PayrollStatus == "Cancelled"))
        {
            ModelState.AddModelError(string.Empty, "Salary adjustments cannot be modified for Paid or Cancelled payrolls.");
            return View(existing);
        }

        if (!ModelState.IsValid)
        {
            // Reload entity with navigation properties so view can render Payroll/Employee info
            var reload = await _context.SalaryAdjustments
                .Include(sa => sa.Payroll).ThenInclude(p => p.Employee)
                .FirstOrDefaultAsync(sa => sa.AdjustmentId == id);
            if (reload != null)
            {
                // restore attempted editable values so user sees their input
                reload.AdjustmentType = salaryadjustment.AdjustmentType;
                reload.Amount = salaryadjustment.Amount;
                reload.Reason = salaryadjustment.Reason;
                return View(reload);
            }
            return View(salaryadjustment);
        }

        // Apply changes only to editable fields
        existing.AdjustmentType = salaryadjustment.AdjustmentType;
        existing.Amount = salaryadjustment.Amount;
        existing.Reason = salaryadjustment.Reason;

        try
        {
            _context.Update(existing);
            await _context.SaveChangesAsync();

            // Notifications
            try
            {
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                var ownerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Owner");
                var employee = await _context.Payrolls.Include(p => p.Employee).ThenInclude(e => e.User).FirstOrDefaultAsync(p => p.PayrollId == existing.PayrollId);
                var title = "Salary adjustment updated";
                var message = $"A salary adjustment for payroll {payroll?.PayrollNumber} was updated. New amount: {existing.Amount:C}.";
                if (adminRole != null) await _notifications.CreateForRoleAsync(adminRole.RoleId, "Payroll", title, message);
                if (ownerRole != null) await _notifications.CreateForRoleAsync(ownerRole.RoleId, "Payroll", title, message);
                if (employee?.Employee?.User?.UserId != 0)
                    await _notifications.CreateForUserAsync(employee.Employee.User.UserId, "Payroll", title, message);
            }
            catch (Exception ex)
            {
                throw;
            }

            // Audit
            try
            {
                var audit = new AuditLog
                {
                    UserId = _httpContextAccessor.HttpContext?.Session.GetInt32("UserID"),
                    TableName = "SalaryAdjustments",
                    RecordId = existing.AdjustmentId,
                    ActionType = "Salary Adjustment Updated",
                    OldValues = null,
                    NewValues = $"Type={existing.AdjustmentType};Amount={existing.Amount};Reason={existing.Reason}",
                    ActionDate = DateTime.Now,
                    Ipaddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
                };
                _context.AuditLogs.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SalaryAdjustmentExists(existing.AdjustmentId))
                return NotFound();
            else throw;
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: SALARYADJUSTMENTS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var salaryadjustment = await _context.SalaryAdjustments
            .Include(sa => sa.Payroll).ThenInclude(p => p.Employee)
            .FirstOrDefaultAsync(m => m.AdjustmentId == id);
        if (salaryadjustment == null)
        {
            return NotFound();
        }

        return View(salaryadjustment);
    }

    // POST: SALARYADJUSTMENTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var salaryadjustment = await _context.SalaryAdjustments.FindAsync(id);
        if (salaryadjustment == null) return NotFound();

        // Prevent deletion if payroll is Paid
        var payroll = await _context.Payrolls.FindAsync(salaryadjustment.PayrollId);
        if (payroll != null && payroll.PayrollStatus == "Paid")
        {
            TempData["Toast"] = "Cannot cancel adjustments for paid payrolls.";
            TempData["ToastType"] = "danger";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // Soft-cancel: mark adjustment inactive and status Cancelled so it remains in history and does not affect payroll totals
        try
        {
            salaryadjustment.IsActive = false;
            salaryadjustment.AdjustmentStatus = "Cancelled";
            _context.Update(salaryadjustment);
            await _context.SaveChangesAsync();

            // Notifications
            try
            {
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                var ownerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Owner");
                var employee = await _context.Payrolls.Include(p => p.Employee).ThenInclude(e => e.User).FirstOrDefaultAsync(p => p.PayrollId == salaryadjustment.PayrollId);
                var title = "Salary adjustment cancelled";
                var message = $"A salary adjustment for payroll {payroll?.PayrollNumber} was cancelled.";
                if (adminRole != null) await _notifications.CreateForRoleAsync(adminRole.RoleId, "Payroll", title, message);
                if (ownerRole != null) await _notifications.CreateForRoleAsync(ownerRole.RoleId, "Payroll", title, message);
                if (employee?.Employee?.User?.UserId != 0)
                    await _notifications.CreateForUserAsync(employee.Employee.User.UserId, "Payroll", title, message);
            }
            catch { }

            // Audit
            try
            {
                var audit = new AuditLog
                {
                    UserId = _httpContextAccessor.HttpContext?.Session.GetInt32("UserID"),
                    TableName = "SalaryAdjustments",
                    RecordId = salaryadjustment.AdjustmentId,
                    ActionType = "Salary Adjustment Cancelled",
                    OldValues = null,
                    NewValues = $"Status=Cancelled",
                    ActionDate = DateTime.Now,
                    Ipaddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
                };
                _context.AuditLogs.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch { }
        }
        catch { }

        return RedirectToAction(nameof(Index));
    }

    private bool SalaryAdjustmentExists(int? adjustmentid)
    {
        return _context.SalaryAdjustments.Any(e => e.AdjustmentId == adjustmentid);
    }
}

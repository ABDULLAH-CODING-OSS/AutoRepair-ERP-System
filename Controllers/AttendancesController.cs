
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using AutoRepairERD.Filters;

[SessionAuthorize]
public class AttendancesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.NotificationService _notificationService;

    public AttendancesController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    // GET: ATTENDANCES  (legacy default-scaffold listing - kept for compatibility)
    [RoleAuthorize("Admin", "Owner")]
    public async Task<IActionResult> Index()
    {
        return View(await _context.Attendances
            .Include(a => a.Employee)
            .OrderByDescending(a => a.AttendanceDate)
            .ToListAsync());
    }

    // GET: ATTENDANCES/Management — the HR/Owner attendance management screen
    [RoleAuthorize("Admin", "Owner")]
    public async Task<IActionResult> Management(string? status, DateOnly? date)
    {
        ViewData["ActiveNav"] = "att-mgmt";

        var query = _context.Attendances.Include(a => a.Employee).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        if (date.HasValue)
        {
            query = query.Where(a => a.AttendanceDate == date.Value);
        }
        else
        {
            // Default view: today's attendance roster
            date = DateOnly.FromDateTime(DateTime.Now);
            query = query.Where(a => a.AttendanceDate == date.Value);
        }

        ViewBag.SelectedDate = date.Value;
        ViewBag.SelectedStatus = status;

        var records = await query.OrderBy(a => a.Employee.FirstName).ToListAsync();

        // Employees with no attendance record for the selected date (shown as "not marked")
        var markedEmployeeIds = records.Select(r => r.EmployeeId).ToHashSet();
        var unmarked = await _context.Employees
            .Where(e => e.IsActive == true && !markedEmployeeIds.Contains(e.EmployeeId))
            .OrderBy(e => e.FirstName)
            .ToListAsync();
        ViewBag.UnmarkedEmployees = unmarked;

        return View(records);
    }

    // GET: ATTENDANCES/Details/5
    [RoleAuthorize("Admin", "Owner")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var attendance = await _context.Attendances
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(m => m.AttendanceId == id);
        if (attendance == null)
        {
            return NotFound();
        }

        return View(attendance);
    }

    // GET: ATTENDANCES/Create
    [RoleAuthorize("Admin", "Owner")]
    public IActionResult Create()
    {
        ViewData["EmployeeId"] = new SelectList(
            _context.Employees.Where(e => e.IsActive == true)
            .Select(e => new { e.EmployeeId, Text = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName }),
            "EmployeeId",
            "Text");

        ViewData["StatusList"] = new SelectList(new[] { "Present", "Absent", "On Leave", "Sick", "Late" });

        return View();
    }

    // POST: ATTENDANCES/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RoleAuthorize("Admin", "Owner")]
    public async Task<IActionResult> Create(
    [Bind("AttendanceId,EmployeeId,AttendanceDate,CheckInTime,CheckOutTime,Status,Notes")]
    Attendance attendance)
    {
        ModelState.Remove("Employee");

        // Ensure attendance date provided
        if (!attendance.AttendanceDate.HasValue)
        {
            ModelState.AddModelError("AttendanceDate", "Attendance date is required.");
        }

        // Prevent future attendance dates
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (attendance.AttendanceDate.HasValue && attendance.AttendanceDate.Value > today)
        {
            ModelState.AddModelError("AttendanceDate", "Cannot mark attendance for a future date.");
        }

        // Validate check-in/check-out times only when both provided
        if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
        {
            if (attendance.CheckOutTime.Value <= attendance.CheckInTime.Value)
            {
                ModelState.AddModelError(
                    "CheckOutTime",
                    "Check Out Time must be after Check In Time.");
            }
        }

        bool attendanceExists = false;
        if (attendance.AttendanceDate.HasValue)
        {
            attendanceExists = _context.Attendances.Any(a =>
                a.EmployeeId == attendance.EmployeeId &&
                a.AttendanceDate == attendance.AttendanceDate.Value);
        }

        if (attendanceExists)
        {
            ModelState.AddModelError(
                "",
                "Attendance already exists for this employee on this date.");
        }

        if (ModelState.IsValid)
        {
            if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
            {
                var workingHours =
                    (decimal)(
                        attendance.CheckOutTime.Value.ToTimeSpan() -
                        attendance.CheckInTime.Value.ToTimeSpan())
                    .TotalHours;

                attendance.OvertimeHours =
                    workingHours > 8
                        ? workingHours - 8
                        : 0;
            }

            if (string.IsNullOrWhiteSpace(attendance.Status))
            {
                attendance.Status = "Present";
            }

            _context.Add(attendance);

            await _context.SaveChangesAsync();

            try
            {
                if (string.Equals(attendance.Status, "Absent", StringComparison.OrdinalIgnoreCase))
                {
                    var employee = await _context.Employees.FindAsync(attendance.EmployeeId);
                    var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                    if (adminRole != null && employee != null)
                    {
                        var title = "Attendance Absence";
                        var dateStr = attendance.AttendanceDate.ToString();
                        var message = $"{employee.FirstName} {employee.LastName} marked absent on {dateStr}.";
                        await _notificationService.CreateForRoleAsync(adminRole.RoleId, "Attendance", title, message);
                    }
                }
            }
            catch
            {
                // Do not block attendance workflow on notification failure
            }

            return RedirectToAction(nameof(Management));
        }

        ViewData["EmployeeId"] = new SelectList(
            _context.Employees.Where(e => e.IsActive == true)
            .Select(e => new { e.EmployeeId, Text = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName }),
            "EmployeeId",
            "Text");

        ViewData["StatusList"] = new SelectList(new[] { "Present", "Absent", "On Leave", "Sick", "Late" });

        return View(attendance);
    }

    // GET: ATTENDANCES/Edit/5
    [RoleAuthorize("Admin", "Owner")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var attendance = await _context.Attendances
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.AttendanceId == id);
        if (attendance == null)
        {
            return NotFound();
        }
        ViewData["EmployeeId"] = new SelectList(
            _context.Employees.Where(e => e.IsActive == true)
            .Select(e => new { e.EmployeeId, Text = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName }),
            "EmployeeId",
            "Text",
            attendance.EmployeeId);

        ViewData["StatusList"] = new SelectList(
            new[] { "Present", "Absent", "On Leave", "Sick", "Late" },
            attendance.Status);

        return View(attendance);
    }

    // POST: ATTENDANCES/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RoleAuthorize("Admin", "Owner")]
    public async Task<IActionResult> Edit(int? id, [Bind("AttendanceId,EmployeeId,AttendanceDate,CheckInTime,CheckOutTime,OvertimeHours,Status,Notes")] Attendance attendance)
    {
        if (id != attendance.AttendanceId)
        {
            return NotFound();
        }

        ModelState.Remove("Employee");

        var existing = await _context.Attendances.AsNoTracking().FirstOrDefaultAsync(a => a.AttendanceId == attendance.AttendanceId);
        var previousStatus = existing?.Status;

        // Ensure attendance date provided
        if (!attendance.AttendanceDate.HasValue)
        {
            ModelState.AddModelError("AttendanceDate", "Attendance date is required.");
        }
        // Prevent future attendance dates
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (attendance.AttendanceDate.HasValue && attendance.AttendanceDate.Value > today)
        {
            ModelState.AddModelError("AttendanceDate", "Cannot set attendance for a future date.");
        }
        // Validate check times only when both provided
        if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
        {
            if (attendance.CheckOutTime.Value <= attendance.CheckInTime.Value)
            {
                ModelState.AddModelError("CheckOutTime", "Check Out Time must be after Check In Time.");
            }
        }
        // Prevent duplicate attendance date for same employee (exclude current record)
        if (attendance.AttendanceDate.HasValue)
        {
            var dup = _context.Attendances.Any(a => a.EmployeeId == attendance.EmployeeId && a.AttendanceDate == attendance.AttendanceDate.Value && a.AttendanceId != attendance.AttendanceId);
            if (dup)
            {
                ModelState.AddModelError(string.Empty, "Another attendance record exists for this employee on the selected date.");
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
                {
                    var workingHours = (decimal)(attendance.CheckOutTime.Value.ToTimeSpan() - attendance.CheckInTime.Value.ToTimeSpan()).TotalHours;
                    attendance.OvertimeHours = workingHours > 8 ? workingHours - 8 : 0;
                }

                _context.Update(attendance);
                await _context.SaveChangesAsync();

                try
                {
                    var becameAbsent = !string.Equals(previousStatus, "Absent", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(attendance.Status, "Absent", StringComparison.OrdinalIgnoreCase);
                    if (becameAbsent)
                    {
                        var employee = await _context.Employees.FindAsync(attendance.EmployeeId);
                        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                        if (adminRole != null && employee != null)
                        {
                            var title = "Attendance Absence";
                            var dateStr = attendance.AttendanceDate.ToString();
                            var message = $"{employee.FirstName} {employee.LastName} marked absent on {dateStr}.";
                            await _notificationService.CreateForRoleAsync(adminRole.RoleId, "Attendance", title, message);
                        }
                    }
                }
                catch
                {
                    // swallow
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AttendanceExists(attendance.AttendanceId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Management));
        }
        ViewData["EmployeeId"] = new SelectList(
            _context.Employees.Where(e => e.IsActive == true)
            .Select(e => new { e.EmployeeId, Text = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName }),
            "EmployeeId",
            "Text",
            attendance.EmployeeId);

        attendance.Employee = await _context.Employees.FindAsync(attendance.EmployeeId);

        ViewData["StatusList"] = new SelectList(
            new[] { "Present", "Absent", "On Leave", "Sick", "Late" },
            attendance.Status);

        if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
        {
            var workingHours = (decimal)(attendance.CheckOutTime.Value.ToTimeSpan() - attendance.CheckInTime.Value.ToTimeSpan()).TotalHours;
            attendance.OvertimeHours = workingHours > 8 ? workingHours - 8 : 0;
        }

        return View(attendance);
    }

    // GET: ATTENDANCES/Delete/5
    [RoleAuthorize("Admin", "Owner")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var attendance = await _context.Attendances
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(m => m.AttendanceId == id);
        if (attendance == null)
        {
            return NotFound();
        }

        return View(attendance);
    }

    // POST: ATTENDANCES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [RoleAuthorize("Admin", "Owner")]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var attendance = await _context.Attendances.FindAsync(id);
        if (attendance != null)
        {
            _context.Attendances.Remove(attendance);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Management));
    }

    // ───────────────────────────────────────────────────────────────
    // SELF-SERVICE: any logged-in employee can mark their own attendance
    // and view their own history. No Admin/Owner role required here.
    // ───────────────────────────────────────────────────────────────

    // GET: ATTENDANCES/Mark — check in / check out for the current employee
    public async Task<IActionResult> Mark()
    {
        ViewData["ActiveNav"] = "mark-att";

        var userId = HttpContext.Session.GetInt32("UserID");
        if (!userId.HasValue) return RedirectToAction("Login", "Auth");

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId.Value);
        if (employee == null)
        {
            TempData["Error"] = "No employee record is linked to this account, so attendance cannot be marked.";
            return RedirectToAction("Index", "Home");
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var todayRecord = await _context.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId && a.AttendanceDate == today);

        var recentHistory = await _context.Attendances
            .Where(a => a.EmployeeId == employee.EmployeeId)
            .OrderByDescending(a => a.AttendanceDate)
            .Take(7)
            .ToListAsync();

        ViewBag.Employee = employee;
        ViewBag.RecentHistory = recentHistory;
        return View(todayRecord);
    }

    // POST: ATTENDANCES/CheckIn
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn()
    {
        var userId = HttpContext.Session.GetInt32("UserID");
        if (!userId.HasValue) return RedirectToAction("Login", "Auth");

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId.Value);
        if (employee == null) return RedirectToAction("Index", "Home");

        var today = DateOnly.FromDateTime(DateTime.Now);
        var existing = await _context.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId && a.AttendanceDate == today);

        if (existing == null)
        {
            _context.Attendances.Add(new Attendance
            {
                EmployeeId = employee.EmployeeId,
                AttendanceDate = today,
                CheckInTime = TimeOnly.FromDateTime(DateTime.Now),
                Status = "Present"
            });
            await _context.SaveChangesAsync();
            TempData["Toast"] = "Checked in successfully.";
        }
        else
        {
            TempData["Error"] = "You have already checked in today.";
        }

        return RedirectToAction(nameof(Mark));
    }

    // POST: ATTENDANCES/CheckOut
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut()
    {
        var userId = HttpContext.Session.GetInt32("UserID");
        if (!userId.HasValue) return RedirectToAction("Login", "Auth");

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId.Value);
        if (employee == null) return RedirectToAction("Index", "Home");

        var today = DateOnly.FromDateTime(DateTime.Now);
        var existing = await _context.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId && a.AttendanceDate == today);

        if (existing == null)
        {
            TempData["Error"] = "You need to check in before you can check out.";
        }
        else if (existing.CheckOutTime.HasValue)
        {
            TempData["Error"] = "You have already checked out today.";
        }
        else
        {
            existing.CheckOutTime = TimeOnly.FromDateTime(DateTime.Now);
            if (existing.CheckInTime.HasValue)
            {
                var workingHours = (decimal)(existing.CheckOutTime.Value.ToTimeSpan() - existing.CheckInTime.Value.ToTimeSpan()).TotalHours;
                existing.OvertimeHours = workingHours > 8 ? workingHours - 8 : 0;
            }
            _context.Update(existing);
            await _context.SaveChangesAsync();
            TempData["Toast"] = "Checked out successfully.";
        }

        return RedirectToAction(nameof(Mark));
    }

    // GET: ATTENDANCES/History — current employee's own attendance log
    public async Task<IActionResult> History(int? month, int? year)
    {
        ViewData["ActiveNav"] = "att-history";

        var userId = HttpContext.Session.GetInt32("UserID");
        if (!userId.HasValue) return RedirectToAction("Login", "Auth");

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId.Value);
        if (employee == null)
        {
            TempData["Error"] = "No employee record is linked to this account.";
            return RedirectToAction("Index", "Home");
        }

        var m = month ?? DateTime.Now.Month;
        var y = year ?? DateTime.Now.Year;

        var records = await _context.Attendances
            .Where(a => a.EmployeeId == employee.EmployeeId
                && a.AttendanceDate.HasValue
                && a.AttendanceDate.Value.Month == m
                && a.AttendanceDate.Value.Year == y)
            .OrderByDescending(a => a.AttendanceDate)
            .ToListAsync();

        ViewBag.Employee = employee;
        ViewBag.Month = m;
        ViewBag.Year = y;
        ViewBag.PresentCount = records.Count(r => r.Status == "Present");
        ViewBag.AbsentCount = records.Count(r => r.Status == "Absent");
        ViewBag.LeaveCount = records.Count(r => r.Status == "On Leave" || r.Status == "Sick");

        return View(records);
    }

    private bool AttendanceExists(int? id)
    {
        return _context.Attendances.Any(e => e.AttendanceId == id);
    }
}

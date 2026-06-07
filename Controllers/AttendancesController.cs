
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using AutoRepairERD.Filters;
[SessionAuthorize]

public class AttendancesController : Controller
{
    private readonly ApplicationDbContext _context;

    public AttendancesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: ATTENDANCES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Attendances
            .Include(a => a.Employee)
            .ToListAsync());
    }

    // GET: ATTENDANCES/Details/5
    public async Task<IActionResult> Details(int?  id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(m => m.AttendanceId == id);
        if (attendance == null)
        {
            return NotFound();
        }

        return View(attendance);
    }

    // GET: ATTENDANCES/Create
    //public IActionResult Create()
    //{
    //    return View();
    //}
    //public IActionResult Create()
    //{
    //    ViewBag.Employees = new SelectList(
    //        _context.Employees
    //            .Where(e => e.IsActive == true),
    //        "EmployeeId",
    //        "EmployeeCode");

    //    return View();
    //}
    public IActionResult Create()
    {
        ViewData["EmployeeId"] = new SelectList(
            _context.Employees.Where(e => e.IsActive == true)
            .Select(e => new { e.EmployeeId, Text = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName }),
            "EmployeeId",
            "Text");

        // Populate status dropdown on Create (optional for user)
        ViewData["StatusList"] = new SelectList(new[] { "Present", "Absent", "On Leave", "Sick", "Late" });

        return View();
    }

    // POST: ATTENDANCES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> Create([Bind("AttendanceId,EmployeeId,AttendanceDate,CheckInTime,CheckOutTime,OvertimeHours,Status,Employee")] Attendance attendance)
    //{
    //    if (ModelState.IsValid)
    //    {
    //        _context.Add(attendance);
    //        await _context.SaveChangesAsync();
    //        return RedirectToAction(nameof(Index));
    //    }
    //    return View(attendance);
    //}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
    [Bind("AttendanceId,EmployeeId,AttendanceDate,CheckInTime,CheckOutTime,Status")]
    Attendance attendance)
    {
        ModelState.Remove("Employee");

        if (attendance.CheckOutTime <= attendance.CheckInTime)
        {
            ModelState.AddModelError(
                "CheckOutTime",
                "Check Out Time must be after Check In Time.");
        }

        bool attendanceExists = _context.Attendances.Any(a =>
            a.EmployeeId == attendance.EmployeeId &&
            a.AttendanceDate == attendance.AttendanceDate);

        if (attendanceExists)
        {
            ModelState.AddModelError(
                "",
                "Attendance already exists for this employee on this date.");
        }

        if (ModelState.IsValid)
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

            // If user selected a status, use it; otherwise default to Present
            if (string.IsNullOrWhiteSpace(attendance.Status))
            {
                attendance.Status = "Present";
            }

            _context.Add(attendance);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        ViewData["EmployeeId"] = new SelectList(
            _context.Employees.Where(e => e.IsActive == true)
            .Select(e => new { e.EmployeeId, Text = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName }),
            "EmployeeId",
            "Text");

        // Repopulate status list when returning view on error
        ViewData["StatusList"] = new SelectList(new[] { "Present", "Absent", "On Leave", "Sick", "Late" });

        return View(attendance);
    }

    // GET: ATTENDANCES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id  == null)
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

        // Populate status dropdown for edit
        ViewData["StatusList"] = new SelectList(
            new[] { "Present", "Absent", "On Leave", "Sick", "Late" },
            attendance.Status);

        return View(attendance);
    }

    // POST: ATTENDANCES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("AttendanceId,EmployeeId,AttendanceDate,CheckInTime,CheckOutTime,OvertimeHours,Status")] Attendance attendance)
    {
        if (id != attendance.AttendanceId)
        {
            return NotFound();
        }

        // Remove navigation property from modelstate to avoid validation errors
        // when Employee (navigation) is not posted from the form.
        ModelState.Remove("Employee");

        if (ModelState.IsValid)
        {
            try
            {
                // Recalculate overtime based on CheckIn/CheckOut similar to Create
                if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
                {
                    var workingHours = (decimal)(attendance.CheckOutTime.Value.ToTimeSpan() - attendance.CheckInTime.Value.ToTimeSpan()).TotalHours;
                    attendance.OvertimeHours = workingHours > 8 ? workingHours - 8 : 0;
                }

                _context.Update(attendance);
                await _context.SaveChangesAsync();
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
            return RedirectToAction(nameof(Index));
        }
        ViewData["EmployeeId"] = new SelectList(
            _context.Employees.Where(e => e.IsActive == true)
            .Select(e => new { e.EmployeeId, Text = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName }),
            "EmployeeId",
            "Text",
            attendance.EmployeeId);

        // Ensure the Employee navigation property is populated so the readonly display works after validation errors
        attendance.Employee = await _context.Employees.FindAsync(attendance.EmployeeId);

        // Repopulate status list
        ViewData["StatusList"] = new SelectList(
            new[] { "Present", "Absent", "On Leave", "Sick", "Late" },
            attendance.Status);

        // Recalculate overtime for display when returning the view
        if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
        {
            var workingHours = (decimal)(attendance.CheckOutTime.Value.ToTimeSpan() - attendance.CheckInTime.Value.ToTimeSpan()).TotalHours;
            attendance.OvertimeHours = workingHours > 8 ? workingHours - 8 : 0;
        }

        return View(attendance);
    }

    // GET: ATTENDANCES/Delete/5
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
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var attendance = await _context.Attendances.FindAsync(id);
        if (attendance != null)
        {
            _context.Attendances.Remove(attendance);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool AttendanceExists(int? id)
    {
        return _context.Attendances.Any(e => e.AttendanceId == id);
    }
}

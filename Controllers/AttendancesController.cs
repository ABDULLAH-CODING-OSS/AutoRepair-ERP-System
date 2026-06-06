
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
        return View(await _context.Attendances.ToListAsync());
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
        ViewBag.Employees = _context.Employees
            .Where(e => e.IsActive == true)
            .Select(e => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName
            })
            .ToList();

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
    [Bind("AttendanceId,EmployeeId,AttendanceDate,CheckInTime,CheckOutTime")]
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

            attendance.Status = "Present";

            _context.Add(attendance);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Employees = new SelectList(
            _context.Employees.Where(e => e.IsActive == true),
            "EmployeeId",
            "EmployeeCode");

        return View(attendance);
    }

    // GET: ATTENDANCES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id  == null)
        {
            return NotFound();
        }

        var attendance = await _context.Attendances.FindAsync(id);
        if (attendance == null)
        {
            return NotFound();
        }
        return View(attendance);
    }

    // POST: ATTENDANCES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("AttendanceId,EmployeeId,AttendanceDate,CheckInTime,CheckOutTime,OvertimeHours,Status,Employee")] Attendance attendance)
    {
        if (id != attendance.AttendanceId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
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

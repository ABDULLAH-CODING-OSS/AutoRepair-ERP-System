
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using AutoRepairERD.Filters;

[RoleAuthorize("Admin","Owner")]
[SessionAuthorize]

public class EmployeesController : Controller
{
    private readonly ApplicationDbContext _context;

    public EmployeesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: EMPLOYEES
    //public async Task<IActionResult> Index()    
    //{
    //    return View(await _context.Employees.ToListAsync());
    //}
    public async Task<IActionResult> Index(string filter = "all")
    {
        var query = _context.Employees.Include(e => e.User).AsQueryable();

        if (filter == "active")
        {
            query = query.Where(e => e.IsActive == true);
        }
        else if (filter == "inactive")
        {
            query = query.Where(e => e.IsActive == false);
        }

        ViewBag.Filter = filter ?? "all";

        var employees = await query.ToListAsync();

        return View(employees);
    }
    //public async Task<IActionResult> Index()
    //{
    //    return View(await _context.Employees.ToListAsync());
    //}
    //public IActionResult Index()
    //{
    //    return Content("Employee Controller Works");
    //}

    //public async Task<IActionResult> Index()
    //{
    //    var count = await _context.Employees.CountAsync();
    //    return Content($"Count = {count}");
    //}
   
    // GET: EMPLOYEES/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var employee = await _context.Employees
            .Include(e => e.User)
            .Include(e => e.Attendances)
            .FirstOrDefaultAsync(m => m.EmployeeId == id);
        if (employee == null)
        {
            return NotFound();
        }

        // Attendance summary
        var total = employee.Attendances?.Count() ?? 0;
        var present = employee.Attendances?.Count(a => a.Status == "Present") ?? 0;
        var absent = employee.Attendances?.Count(a => a.Status == "Absent") ?? 0;
        var sick = employee.Attendances?.Count(a => a.Status == "Sick") ?? 0;

        ViewBag.TotalAttendances = total;
        ViewBag.PresentDays = present;
        ViewBag.AbsentDays = absent;
        ViewBag.SickDays = sick;

        return View(employee);
    }

    // GET: EMPLOYEES/Create
    //public IActionResult Create()
    //{
    //    return View();
    //}
    public IActionResult Create()
    {
        ViewBag.Roles = new SelectList(
            _context.Roles,
            "RoleId",
            "RoleName");

        return View(new EmployeeRegistrationViewModel());
    }

    // POST: EMPLOYEES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> Create([Bind("EmployeeId,UserId,EmployeeCode,FirstName,LastName,Cnic,Phone,Address,HireDate,Designation,BasicSalary,HourlyRate,IsActive,Attendances,JobOrderAdvisors,JobOrderMechanics,JobServiceItems,Payrolls,User")] Employee employee)
    //{
    //    if (ModelState.IsValid)
    //    {
    //        _context.Add(employee);
    //        await _context.SaveChangesAsync();
    //        return RedirectToAction(nameof(Index));
    //    }
    //    return View(employee);
    //}

    //catch
    //{
    //    await transaction.RollbackAsync();

    //    ViewBag.Roles = new SelectList(
    //        _context.Roles,
    //        "RoleId",
    //        "RoleName");

    //    return View(model);
    //}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeRegistrationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = new SelectList(
                _context.Roles,
                "RoleId",
                "RoleName");

            return View(model);
        }

        // HireDate validation: cannot be future date
        if (model.HireDate.HasValue && model.HireDate > DateOnly.FromDateTime(DateTime.Now))
        {
            ModelState.AddModelError("HireDate", "Hire Date cannot be in the future.");
            ViewBag.Roles = new SelectList(
                _context.Roles,
                "RoleId",
                "RoleName");

            return View(model);
        }

        // Friendly validations

        if (_context.Users.Any(u => u.Username == model.Username))
        {
            ModelState.AddModelError("Username", "Username already exists.");

            ViewBag.Roles = new SelectList(
                _context.Roles,
                "RoleId",
                "RoleName");

            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.Email) &&
            _context.Users.Any(u => u.Email == model.Email))
        {
            ModelState.AddModelError("Email", "Email already exists.");

            ViewBag.Roles = new SelectList(
                _context.Roles,
                "RoleId",
                "RoleName");

            return View(model);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var user = new User
            {
                Username = model.Username,
                PasswordHash = model.Password, // Later hash passwords
                Email = model.Email,
                FullName = $"{model.FirstName} {model.LastName}",
                Phone = model.Phone,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userRole = new UserRole
            {
                UserId = user.UserId,
                RoleId = model.RoleId
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            var nextEmployeeNumber = _context.Employees.Count() + 1;

            var employee = new Employee
            {
                UserId = user.UserId,
                EmployeeCode = $"EMP{nextEmployeeNumber:D3}",
                FirstName = model.FirstName,
                LastName = model.LastName,
                Cnic = model.Cnic,
                Phone = model.Phone,
                Address = model.Address,
                Designation = model.Designation,
                BasicSalary = model.BasicSalary,
                HourlyRate = model.HourlyRate,
                HireDate = model.HireDate,
                IsActive = true
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);

            ViewBag.Roles = new SelectList(
                _context.Roles,
                "RoleId",
                "RoleName");

            return View(model);
        }
    }

    // GET: EMPLOYEES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            return NotFound();
        }
        return View(employee);
    }

    // POST: EMPLOYEES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> Edit(int? id, [Bind("EmployeeId,UserId,EmployeeCode,FirstName,LastName,Cnic,Phone,Address,HireDate,Designation,BasicSalary,HourlyRate,IsActive,Attendances,JobOrderAdvisors,JobOrderMechanics,JobServiceItems,Payrolls,User")] Employee employee)
    //{
    //    if (id != employee.EmployeeId)
    //    {
    //        return NotFound();
    //    }

    //    if (ModelState.IsValid)
    //    {
    //        try
    //        {
    //            _context.Update(employee);
    //            await _context.SaveChangesAsync();
    //        }
    //        catch (DbUpdateConcurrencyException)
    //        {
    //            if (!EmployeeExists(employee.EmployeeId))
    //            {
    //                return NotFound();
    //            }
    //            else
    //            {
    //                throw;
    //            }
    //        }
    //        return RedirectToAction(nameof(Index));
    //    }
    //    return View(employee);
    //}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
    int? id,
    [Bind("EmployeeId,UserId,EmployeeCode,FirstName,LastName,Cnic,Phone,Address,HireDate,Designation,BasicSalary,HourlyRate,IsActive")]
    Employee employee)
    {
        if (id != employee.EmployeeId)
        {
            return NotFound();
        }

        ModelState.Remove("User");
        ModelState.Remove("Attendances");
        ModelState.Remove("JobOrderAdvisors");
        ModelState.Remove("JobOrderMechanics");
        ModelState.Remove("JobServiceItems");
        ModelState.Remove("Payrolls");

        // Prevent changing UserId via model binder (if form doesn't post it). Load existing and apply allowed updates.
        if (ModelState.IsValid)
        {
            // HireDate validation: cannot be future
            if (employee.HireDate.HasValue && employee.HireDate > DateOnly.FromDateTime(DateTime.Now))
            {
                ModelState.AddModelError("HireDate", "Hire Date cannot be in the future.");
                return View(employee);
            }

            var existing = await _context.Employees.FindAsync(employee.EmployeeId);
            if (existing == null)
                return NotFound();

            // Update allowed fields (do not overwrite UserId)
            // Validate EmployeeCode uniqueness if provided
            if (!string.IsNullOrWhiteSpace(employee.EmployeeCode))
            {
                var exists = _context.Employees.Any(e => e.EmployeeCode == employee.EmployeeCode && e.EmployeeId != employee.EmployeeId);
                if (exists)
                {
                    ModelState.AddModelError("EmployeeCode", "Employee Code already exists.");
                    return View(employee);
                }
            }

            existing.EmployeeCode = employee.EmployeeCode;
            existing.FirstName = employee.FirstName;
            existing.LastName = employee.LastName;
            existing.Cnic = employee.Cnic;
            existing.Phone = employee.Phone;
            existing.Address = employee.Address;
            existing.HireDate = employee.HireDate;
            existing.Designation = employee.Designation;
            existing.BasicSalary = employee.BasicSalary;
            existing.HourlyRate = employee.HourlyRate;
            existing.IsActive = employee.IsActive;

            try
            {
                _context.Update(existing);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(employee.EmployeeId))
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

        return View(employee);
    }

    // POST: EMPLOYEES/Activate/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int? id)
    {
        if (id == null)
            return NotFound();

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
            return NotFound();

        employee.IsActive = true;
        _context.Update(employee);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: EMPLOYEES/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(m => m.EmployeeId == id);
        if (employee == null)
        {
            return NotFound();
        }

        return View(employee);
    }

    // POST: EMPLOYEES/Delete/5
    //[HttpPost, ActionName("Delete")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> DeleteConfirmed(int? id)
    //{
    //    var employee = await _context.Employees.FindAsync(id);
    //    if (employee != null)
    //    {
    //        _context.Employees.Remove(employee);
    //    }

    //    await _context.SaveChangesAsync();
    //    return RedirectToAction(nameof(Index));
    //}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
        {
            return NotFound();
        }

        employee.IsActive = false;

        _context.Update(employee);

        await _context.SaveChangesAsync();

        TempData["Toast"] = "Employee deactivated.";
        TempData["ToastType"] = "warning";
        return RedirectToAction(nameof(Index));
    }
    private bool EmployeeExists(int? id)
    {
        return _context.Employees.Any(e => e.EmployeeId == id);
    }

}

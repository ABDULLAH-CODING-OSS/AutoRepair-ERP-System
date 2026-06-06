
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using AutoRepairERD.Filters;

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
    public async Task<IActionResult> Index()
    {
        return View(await _context.Employees
            .Where(e => e.IsActive == true)
            .ToListAsync());
    }

    // GET: EMPLOYEES/Details/5
    public async Task<IActionResult> Details(int? id)
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

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(employee);
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

        return RedirectToAction(nameof(Index));
    }
    private bool EmployeeExists(int? id)
    {
        return _context.Employees.Any(e => e.EmployeeId == id);
    }

}

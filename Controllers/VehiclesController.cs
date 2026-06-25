
using AutoRepairERD.Filters;
using AutoRepairERD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

[SessionAuthorize]
public class VehiclesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.NotificationService _notificationService;
    private readonly AutoRepairERD.Services.AuditService _auditService;

    public VehiclesController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notificationService, AutoRepairERD.Services.AuditService auditService)
    {
        _context = context;
        _notificationService = notificationService;
        _auditService = auditService;
    }

    // GET: VEHICLES
    public async Task<IActionResult> Index(string q)
    {
        var query = _context.Vehicles.Include(v => v.Customer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(v => (v.LicensePlate ?? "").Contains(q) || (v.Vin ?? "").Contains(q) || (v.Make ?? "").Contains(q) || (v.Model ?? "").Contains(q) || (v.Customer != null && ((v.Customer.FirstName ?? "").Contains(q) || (v.Customer.LastName ?? "").Contains(q))));
            ViewBag.SearchQuery = q;
        }
        var list = await query.ToListAsync();
        return View(list);
    }

    // GET: VEHICLES/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vehicle = await _context.Vehicles
            .Include(v => v.Customer)
            .Include(v => v.JobOrders.OrderByDescending(j => j.CreatedAt))
                .ThenInclude(j => j.Invoices)
                    .ThenInclude(i => i.Payments)
            .Include(v => v.JobOrders)
                .ThenInclude(j => j.JobServiceItems)
                    .ThenInclude(s => s.Service)
            .Include(v => v.JobOrders)
                .ThenInclude(j => j.JobPartItems)
                    .ThenInclude(p => p.Part)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.VehicleId == id);
        if (vehicle == null)
        {
            return NotFound();
        }

        return View(vehicle);
    }

    // GET: VEHICLES/Create
    //public IActionResult Create()
    //{
    //    return View();
    //}
    public IActionResult Create()
    {
        ViewBag.Customers = _context.Customers
            .Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = c.FirstName + " " + c.LastName + " (" + c.Phone + ")"
            })
            .ToList();

        return View();
    }

    // POST: VEHICLES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CustomerId,LicensePlate,Vin,Make,Model,ManufacturingYear,Color,Mileage,EngineNumber,Notes")] Vehicle vehicle)
    {
        ModelState.Remove("Customer");
        ModelState.Remove("JobOrders");
        ModelState.Remove("CreatedByUser");

        // Business validations
        // Manufacturing year must not be in the future
        if (vehicle.ManufacturingYear.HasValue && vehicle.ManufacturingYear.Value > DateTime.UtcNow.Year)
        {
            ModelState.AddModelError("ManufacturingYear", "Manufacturing year cannot be greater than the current year.");
        }

        // VIN: optional but if provided must be 17 characters
        //if (!string.IsNullOrWhiteSpace(vehicle.Vin) && vehicle.Vin.Length != 17)
        //{
        //    ModelState.AddModelError("Vin", "VIN must be exactly 17 characters.");
        //}

        // LicensePlate and VIN uniqueness checks
        if (!string.IsNullOrWhiteSpace(vehicle.LicensePlate))
        {
            bool exists = _context.Vehicles.Any(v => v.LicensePlate == vehicle.LicensePlate);
            if (exists)
            {
                ModelState.AddModelError("LicensePlate", "This vehicle registration number is already registered.");
            }
        }

        //if (!string.IsNullOrWhiteSpace(vehicle.Vin))
        //{
        //    bool vinExists = _context.Vehicles.Any(v => v.Vin == vehicle.Vin);
        //    if (vinExists)
        //    {
        //        ModelState.AddModelError("Vin", "This VIN is already registered.");
        //    }
        //}
        if (!string.IsNullOrWhiteSpace(vehicle.Vin))
        {
            if (vehicle.Vin.Length != 17)
            {
                ModelState.AddModelError(
                    "Vin",
                    "VIN must be exactly 17 characters.");
            }
            else
            {
                bool vinExists = _context.Vehicles.Any(
                    v => v.Vin == vehicle.Vin &&
                         v.VehicleId != vehicle.VehicleId);

                if (vinExists)
                {
                    ModelState.AddModelError(
                        "Vin",
                        "This VIN is already registered.");
                }
            }
        }

        if (ModelState.IsValid)
        {
            vehicle.CreatedByUserId = HttpContext.Session.GetInt32("UserID");
            _context.Add(vehicle);
            try
            {
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogCreateAsync("Vehicles", vehicle.VehicleId, $"{vehicle.LicensePlate} - {vehicle.Make} {vehicle.Model}");

                // Batch 3: NEW VEHICLE REGISTERED - notify Admin and Service Advisor
                try
                {
                    var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                    var saRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Service Advisor");
                    var customer = await _context.Customers.FindAsync(vehicle.CustomerId);
                    var custName = customer != null ? (customer.FirstName + (string.IsNullOrEmpty(customer.LastName) ? "" : " " + customer.LastName)) : "Unknown";
                    var title = "New Vehicle Registered";
                    var message = $"New vehicle {vehicle.LicensePlate} registered for {custName}.";
                    if (adminRole != null)
                        await _notificationService.CreateForRoleAsync(adminRole.RoleId, "Vehicle", title, message);
                    if (saRole != null)
                        await _notificationService.CreateForRoleAsync(saRole.RoleId, "Vehicle", title, message);
                }
                catch { }

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                var msg = dbEx.InnerException?.Message ?? dbEx.Message;
                if (msg != null && msg.Contains("LicensePlate", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("LicensePlate", "This vehicle registration number is already registered.");
                }
                else if (msg != null && msg.Contains("VIN", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("Vin", "This VIN is already registered.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Unable to save changes. Try again later.");
                }
            }
        }

        // If we got here, validation failed
        // Re-populate customers dropdown if needed for the view
        ViewBag.Customers = _context.Customers
            .Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = c.FirstName + " " + c.LastName + " (" + c.Phone + ")"
            })
            .ToList();
        // If we got here, validation failed

        ViewBag.Customers = _context.Customers
            .Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = c.FirstName + " " + c.LastName + " (" + c.Phone + ")"
            })
            .ToList();

        vehicle.Customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == vehicle.CustomerId);

        return View(vehicle);
    }

        // Include Customer navigation if available so View can show owner info properly
        //vehicle.Customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == vehicle.CustomerId);

        // Aggregate ModelState errors into a model-level message for clearer feedback
        //    if (!ModelState.IsValid)
        //    {
        //        var list = ModelState.Where(kvp => kvp.Value.Errors.Any())
        //            .Select(kvp => new { Field = kvp.Key, Errors = kvp.Value.Errors.Select(e => !string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.ErrorMessage : (e.Exception?.Message ?? string.Empty)).Where(m => !string.IsNullOrWhiteSpace(m)) })
        //            .Where(x => x.Errors.Any())
        //            .Select(x => x.Field + ": " + string.Join("; ", x.Errors));
        //        var msg = string.Join(" | ", list);
        //        if (!string.IsNullOrWhiteSpace(msg)) ModelState.AddModelError(string.Empty, msg);
        //    }

        //    return View(vehicle);
        //}

        // GET: VEHICLES/Edit/5
        //public async Task<IActionResult> Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var vehicle = await _context.Vehicles.FindAsync(id);
        //    if (vehicle == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(vehicle);
        //}
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vehicle = await _context.Vehicles
            .Include(v => v.Customer)
            .FirstOrDefaultAsync(v => v.VehicleId == id);

        if (vehicle == null)
        {
            return NotFound();
        }

        return View(vehicle);
    }
    //POST: VEHICLES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("VehicleId,CustomerId,CreatedByUserId,LicensePlate,Vin,Make,Model,ManufacturingYear,Color,Mileage,EngineNumber,Notes")] Vehicle vehicle)
    {
        if (id != vehicle.VehicleId)
        {
            return NotFound();
        }

        ModelState.Remove("Customer");
        ModelState.Remove("JobOrders");
        ModelState.Remove("CreatedByUser");

        // Ensure owner cannot be changed: preserve original CustomerId from DB
        var existing = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.VehicleId == vehicle.VehicleId);
        if (existing == null)
        {
            return NotFound();
        }
        vehicle.CustomerId = existing.CustomerId;

        // Business validations for Edit
        if (vehicle.ManufacturingYear.HasValue && vehicle.ManufacturingYear.Value > DateTime.UtcNow.Year)
        {
            ModelState.AddModelError("ManufacturingYear", "Manufacturing year cannot be greater than the current year.");
        }

        if (vehicle.Mileage.HasValue && vehicle.Mileage.Value < 0)
        {
            ModelState.AddModelError("Mileage", "Mileage cannot be negative.");
        }
        // Uniqueness checks (exclude current record)
        if (!string.IsNullOrWhiteSpace(vehicle.LicensePlate))
        {
            bool exists = _context.Vehicles.Any(v => v.LicensePlate == vehicle.LicensePlate && v.VehicleId != vehicle.VehicleId);
            if (exists)
            {
                ModelState.AddModelError("LicensePlate", "This vehicle registration number is already registered.");
            }
        }

        //if (!string.IsNullOrWhiteSpace(vehicle.Vin) && vehicle.Vin.Length != 17)
        //{
        //    ModelState.AddModelError("Vin", "VIN must be exactly 17 characters.");
        //}


        //if (!string.IsNullOrWhiteSpace(vehicle.Vin))
        //{
        //    bool vinExists = _context.Vehicles.Any(v => v.Vin == vehicle.Vin && v.VehicleId != vehicle.VehicleId);
        //    if (vinExists)
        //    {
        //        ModelState.AddModelError("Vin", "This VIN is already registered.");
        //    }
        //}
        if (!string.IsNullOrWhiteSpace(vehicle.Vin))
        {
            if (vehicle.Vin.Length != 17)
            {
                ModelState.AddModelError(
                    "Vin",
                    "VIN must be exactly 17 characters.");
            }
            else
            {
                bool vinExists = _context.Vehicles.Any(
                    v => v.Vin == vehicle.Vin &&
                         v.VehicleId != vehicle.VehicleId);

                if (vinExists)
                {
                    ModelState.AddModelError(
                        "Vin",
                        "This VIN is already registered.");
                }
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(vehicle);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogUpdateAsync("Vehicles", vehicle.VehicleId, null, $"{vehicle.LicensePlate} - {vehicle.Make} {vehicle.Model}");

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VehicleExists(vehicle.VehicleId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (DbUpdateException dbEx)
            {
                var msg = dbEx.InnerException?.Message ?? dbEx.Message;
                if (msg != null && msg.Contains("LicensePlate", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("LicensePlate", "This vehicle registration number is already registered.");
                }
                else if (msg != null && msg.Contains("VIN", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("Vin", "This VIN is already registered.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Unable to save changes. Try again later.");
                }
            }
        }
        vehicle.Customer = await _context.Customers
    .FirstOrDefaultAsync(c => c.CustomerId == vehicle.CustomerId);

        return View(vehicle);
    }
        //vehicle.Customer = await _context.Customers
        //    .FirstOrDefaultAsync(c => c.CustomerId == vehicle.CustomerId);

        // Aggregate ModelState errors for summary display including exception messages
        //    if (!ModelState.IsValid)
        //    {
        //        var list = ModelState.Where(kvp => kvp.Value.Errors.Any())
        //            .Select(kvp => new { Field = kvp.Key, Errors = kvp.Value.Errors.Select(e => !string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.ErrorMessage : (e.Exception?.Message ?? string.Empty)).Where(m => !string.IsNullOrWhiteSpace(m)) })
        //            .Where(x => x.Errors.Any())
        //            .Select(x => x.Field + ": " + string.Join("; ", x.Errors));
        //        var msg = string.Join(" | ", list);
        //        if (!string.IsNullOrWhiteSpace(msg)) ModelState.AddModelError(string.Empty, msg);

        //        // Also put errors and posted values into TempData for immediate visibility in UI
        //        TempData["VehicleEditErrors"] = msg;
        //        TempData["VehiclePostedValues"] = $"VehicleId={vehicle.VehicleId}; LicensePlate={vehicle.LicensePlate}; Vin={vehicle.Vin}; Make={vehicle.Make}; Model={vehicle.Model}; ManufacturingYear={vehicle.ManufacturingYear}; Mileage={vehicle.Mileage}";
        //    }

        //    return View(vehicle);
        //}


        // GET: VEHICLES/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(m => m.VehicleId == id);
        if (vehicle == null)
        {
            return NotFound();
        }

        return View(vehicle);
    }

    // POST: VEHICLES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle != null)
        {
            var vehicleInfo = $"{vehicle.LicensePlate} - {vehicle.Make} {vehicle.Model}";
            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogDeleteAsync("Vehicles", (int)id, vehicleInfo);
        }
        else
        {
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool VehicleExists(int? id)
    {
        return _context.Vehicles.Any(e => e.VehicleId == id);
    }
}

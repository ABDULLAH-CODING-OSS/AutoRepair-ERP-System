using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

namespace AutoRepairERD.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VehiclesApiController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // GET: api/vehicles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAll()
        {
            var list = await _context.Vehicles
                .AsNoTracking()
                .Select(v => new VehicleDto
                {
                    VehicleId = v.VehicleId,
                    CustomerId = v.CustomerId,
                    CreatedByUserId = v.CreatedByUserId,
                    LicensePlate = v.LicensePlate,
                    Vin = v.Vin,
                    Make = v.Make,
                    Model = v.Model,
                    ManufacturingYear = v.ManufacturingYear,
                    Color = v.Color,
                    Mileage = v.Mileage,
                    EngineNumber = v.EngineNumber,
                    Notes = v.Notes
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/vehicles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleDto>> GetById(int id)
        {
            var v = await _context.Vehicles
                .AsNoTracking()
                .Where(x => x.VehicleId == id)
                .Select(v => new VehicleDto
                {
                    VehicleId = v.VehicleId,
                    CustomerId = v.CustomerId,
                    CreatedByUserId = v.CreatedByUserId,
                    LicensePlate = v.LicensePlate,
                    Vin = v.Vin,
                    Make = v.Make,
                    Model = v.Model,
                    ManufacturingYear = v.ManufacturingYear,
                    Color = v.Color,
                    Mileage = v.Mileage,
                    EngineNumber = v.EngineNumber,
                    Notes = v.Notes
                })
                .FirstOrDefaultAsync();

            if (v == null)
                return NotFound();

            return Ok(v);
        }

        // POST: api/vehicles
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
        {
            if (dto == null)
                return BadRequest("Request body is required.");
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);
            var vehicle = new Vehicle
            {
                CustomerId = dto.CustomerId,
                CreatedByUserId = dto.CreatedByUserId,
                LicensePlate = dto.LicensePlate,
                Vin = dto.Vin,
                Make = dto.Make,
                Model = dto.Model,
                ManufacturingYear = dto.ManufacturingYear,
                Color = dto.Color,
                Mileage = dto.Mileage,
                EngineNumber = dto.EngineNumber,
                Notes = dto.Notes
            };
            ModelState.Remove(nameof(Vehicle.Customer));
            ModelState.Remove(nameof(Vehicle.CreatedByUser));

            TryValidateModel(vehicle);

            // Validate model attributes
            //TryValidateModel(vehicle);
            //if (!ModelState.IsValid)
            //    return ValidationProblem(ModelState);

            // Business validations from MVC
            if (vehicle.ManufacturingYear.HasValue && vehicle.ManufacturingYear.Value > DateTime.UtcNow.Year)
            {
                ModelState.AddModelError("ManufacturingYear", "Manufacturing year cannot be greater than the current year.");
                return ValidationProblem(ModelState);
            }

            if (vehicle.Mileage.HasValue && vehicle.Mileage.Value < 0)
            {
                ModelState.AddModelError("Mileage", "Mileage cannot be negative.");
                return ValidationProblem(ModelState);
            }

            if (!string.IsNullOrWhiteSpace(vehicle.LicensePlate))
            {
                if (await _context.Vehicles.AnyAsync(v => v.LicensePlate == vehicle.LicensePlate))
                {
                    ModelState.AddModelError("LicensePlate", "This vehicle registration number is already registered.");
                    return ValidationProblem(ModelState);
                }
            }

            if (!string.IsNullOrWhiteSpace(vehicle.Vin))
            {
                if (vehicle.Vin.Length != 17)
                {
                    ModelState.AddModelError("Vin", "VIN must be exactly 17 characters.");
                    return ValidationProblem(ModelState);
                }

                if (await _context.Vehicles.AnyAsync(v => v.Vin == vehicle.Vin))
                {
                    ModelState.AddModelError("Vin", "This VIN is already registered.");
                    return ValidationProblem(ModelState);
                }
            }

            try
            {
                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();

                var result = new VehicleDto
                {
                    VehicleId = vehicle.VehicleId,
                    CustomerId = vehicle.CustomerId,
                    CreatedByUserId = vehicle.CreatedByUserId,
                    LicensePlate = vehicle.LicensePlate,
                    Vin = vehicle.Vin,
                    Make = vehicle.Make,
                    Model = vehicle.Model,
                    ManufacturingYear = vehicle.ManufacturingYear,
                    Color = vehicle.Color,
                    Mileage = vehicle.Mileage,
                    EngineNumber = vehicle.EngineNumber,
                    Notes = vehicle.Notes
                };

                return CreatedAtAction(nameof(GetById), new { id = vehicle.VehicleId }, result);
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A database error occurred while creating the vehicle." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // PUT: api/vehicles/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateVehicleDto dto)
        {
            if (dto == null)
                return BadRequest("Request body is required.");
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);
            var existing = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.VehicleId == id);
            if (existing == null)
                return NotFound();

            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
                return NotFound();

            // Preserve owner: cannot change CustomerId
            vehicle.LicensePlate = dto.LicensePlate;
            vehicle.Vin = dto.Vin;
            vehicle.Make = dto.Make;
            vehicle.Model = dto.Model;
            vehicle.ManufacturingYear = dto.ManufacturingYear;
            vehicle.Color = dto.Color;
            vehicle.Mileage = dto.Mileage;
            vehicle.EngineNumber = dto.EngineNumber;
            vehicle.Notes = dto.Notes;

            ModelState.Remove(nameof(Vehicle.Customer));
ModelState.Remove(nameof(Vehicle.CreatedByUser));

TryValidateModel(vehicle);
            // Validate
            //TryValidateModel(vehicle);
            //if (!ModelState.IsValid)
            //    return ValidationProblem(ModelState);

            if (vehicle.ManufacturingYear.HasValue && vehicle.ManufacturingYear.Value > DateTime.UtcNow.Year)
            {
                ModelState.AddModelError("ManufacturingYear", "Manufacturing year cannot be greater than the current year.");
                return ValidationProblem(ModelState);
            }

            if (vehicle.Mileage.HasValue && vehicle.Mileage.Value < 0)
            {
                ModelState.AddModelError("Mileage", "Mileage cannot be negative.");
                return ValidationProblem(ModelState);
            }

            if (!string.IsNullOrWhiteSpace(vehicle.LicensePlate))
            {
                if (await _context.Vehicles.AnyAsync(v => v.LicensePlate == vehicle.LicensePlate && v.VehicleId != vehicle.VehicleId))
                {
                    ModelState.AddModelError("LicensePlate", "This vehicle registration number is already registered.");
                    return ValidationProblem(ModelState);
                }
            }

            if (!string.IsNullOrWhiteSpace(vehicle.Vin))
            {
                if (vehicle.Vin.Length != 17)
                {
                    ModelState.AddModelError("Vin", "VIN must be exactly 17 characters.");
                    return ValidationProblem(ModelState);
                }

                if (await _context.Vehicles.AnyAsync(v => v.Vin == vehicle.Vin && v.VehicleId != vehicle.VehicleId))
                {
                    ModelState.AddModelError("Vin", "This VIN is already registered.");
                    return ValidationProblem(ModelState);
                }
            }

            try
            {
                _context.Entry(vehicle).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Vehicles.AnyAsync(e => e.VehicleId == id))
                    return NotFound();
                return Conflict(new { message = "A concurrency error occurred while updating the vehicle." });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A database error occurred while updating the vehicle." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // DELETE: api/vehicles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
                return NotFound();

            try
            {
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A database error occurred while deleting the vehicle." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // DTOs
        public class VehicleDto
        {
            public int VehicleId { get; set; }
            public int CustomerId { get; set; }
            public int? CreatedByUserId { get; set; }
            public string LicensePlate { get; set; } = string.Empty;
            public string? Vin { get; set; }
            public string Make { get; set; } = string.Empty;
            public string Model { get; set; } = string.Empty;
            public int? ManufacturingYear { get; set; }
            public string? Color { get; set; }
            public int? Mileage { get; set; }
            public string? EngineNumber { get; set; }
            public string? Notes { get; set; }
        }

        public class CreateVehicleDto
        {
            public int CustomerId { get; set; }
            public int? CreatedByUserId { get; set; }
            public string LicensePlate { get; set; } = string.Empty;
            public string? Vin { get; set; }
            public string Make { get; set; } = string.Empty;
            public string Model { get; set; } = string.Empty;
            public int? ManufacturingYear { get; set; }
            public string? Color { get; set; }
            public int? Mileage { get; set; }
            public string? EngineNumber { get; set; }
            public string? Notes { get; set; }
        }

        public class UpdateVehicleDto
        {
            public string LicensePlate { get; set; } = string.Empty;
            public string? Vin { get; set; }
            public string Make { get; set; } = string.Empty;
            public string Model { get; set; } = string.Empty;
            public int? ManufacturingYear { get; set; }
            public string? Color { get; set; }
            public int? Mileage { get; set; }
            public string? EngineNumber { get; set; }
            public string? Notes { get; set; }
        }
    }
}

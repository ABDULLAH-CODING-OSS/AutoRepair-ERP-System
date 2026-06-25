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
    public class ServicesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ServicesApiController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // GET: api/services
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ServiceDto>>> GetAll()
        {
            var list = await _context.Services
                .AsNoTracking()
                .Select(s => new ServiceDto
                {
                    ServiceId = s.ServiceId,
                    ServiceName = s.ServiceName,
                    Description = s.Description,
                    StandardHours = s.StandardHours,
                    FixedPrice = s.FixedPrice,
                    IsActive = s.IsActive
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/services/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceDto>> GetById(int id)
        {
            var s = await _context.Services
                .AsNoTracking()
                .Where(x => x.ServiceId == id)
                .Select(s => new ServiceDto
                {
                    ServiceId = s.ServiceId,
                    ServiceName = s.ServiceName,
                    Description = s.Description,
                    StandardHours = s.StandardHours,
                    FixedPrice = s.FixedPrice,
                    IsActive = s.IsActive
                })
                .FirstOrDefaultAsync();

            if (s == null)
                return NotFound();

            return Ok(s);
        }

        // POST: api/services
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateServiceDto dto)
        {
            if (dto == null)
                return BadRequest("Request body is required.");

            var service = new Service
            {
                ServiceName = dto.ServiceName,
                Description = dto.Description,
                StandardHours = dto.StandardHours,
                FixedPrice = dto.FixedPrice,
                IsActive = dto.IsActive
            };

            TryValidateModel(service);
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                var result = new ServiceDto
                {
                    ServiceId = service.ServiceId,
                    ServiceName = service.ServiceName,
                    Description = service.Description,
                    StandardHours = service.StandardHours,
                    FixedPrice = service.FixedPrice,
                    IsActive = service.IsActive
                };

                return CreatedAtAction(nameof(GetById), new { id = service.ServiceId }, result);
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A database error occurred while creating the service." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // PUT: api/services/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateServiceDto dto)
        {
            if (dto == null)
                return BadRequest("Request body is required.");

            var service = await _context.Services.FindAsync(id);
            if (service == null)
                return NotFound();

            service.ServiceName = dto.ServiceName;
            service.Description = dto.Description;
            service.StandardHours = dto.StandardHours;
            service.FixedPrice = dto.FixedPrice;
            service.IsActive = dto.IsActive;

            TryValidateModel(service);
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                _context.Entry(service).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Services.AnyAsync(e => e.ServiceId == id))
                    return NotFound();
                return Conflict(new { message = "A concurrency error occurred while updating the service." });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A database error occurred while updating the service." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // DELETE: api/services/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
                return NotFound();

            try
            {
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A database error occurred while deleting the service." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // DTOs
        public class ServiceDto
        {
            public int ServiceId { get; set; }
            public string ServiceName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal? StandardHours { get; set; }
            public decimal? FixedPrice { get; set; }
            public bool IsActive { get; set; }
        }

        public class CreateServiceDto
        {
            public string ServiceName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal? StandardHours { get; set; }
            public decimal? FixedPrice { get; set; }
            public bool IsActive { get; set; }
        }

        public class UpdateServiceDto
        {
            public string ServiceName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal? StandardHours { get; set; }
            public decimal? FixedPrice { get; set; }
            public bool IsActive { get; set; }
        }
    }
}

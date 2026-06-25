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
    public class CustomersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CustomersApiController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // GET: api/customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
        {
            var customers = await _context.Customers
                .AsNoTracking()
                .Select(c => new CustomerDto
                {
                    CustomerId = c.CustomerId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Phone = c.Phone,
                    Email = c.Email,
                    Address = c.Address,
                    City = c.City,
                    CreatedAt = c.CreatedAt,
                    CreatedByUserId = c.CreatedByUserId
                })
                .ToListAsync();

            return Ok(customers);
        }

        // GET: api/customers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerDto>> GetById(int id)
        {
            var c = await _context.Customers
                .AsNoTracking()
                .Where(x => x.CustomerId == id)
                .Select(cu => new CustomerDto
                {
                    CustomerId = cu.CustomerId,
                    FirstName = cu.FirstName,
                    LastName = cu.LastName,
                    Phone = cu.Phone,
                    Email = cu.Email,
                    Address = cu.Address,
                    City = cu.City,
                    CreatedAt = cu.CreatedAt,
                    CreatedByUserId = cu.CreatedByUserId
                })
                .FirstOrDefaultAsync();

            if (c == null)
                return NotFound();

            return Ok(c);
        }

        // POST: api/customers
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
        {
            if (dto == null)
                return BadRequest("Request body is required.");

            // Map to entity and reuse model validation attributes on Customer
            var customer = new Customer
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                City = dto.City,
                CreatedAt = DateTime.Now,
                CreatedByUserId = dto.CreatedByUserId
            };

            // Validate data annotations defined on Customer model
            TryValidateModel(customer);
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // Business rules from MVC: email required, format, uniqueness, phone uniqueness
            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                ModelState.AddModelError("Email", "Email is required.");
                return ValidationProblem(ModelState);
            }
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(customer.Email))
            {
                ModelState.AddModelError("Email", "Invalid email format.");
                return ValidationProblem(ModelState);
            }
            if (await _context.Customers.AnyAsync(c => c.Email == customer.Email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return ValidationProblem(ModelState);
            }
            if (!string.IsNullOrWhiteSpace(customer.Phone) && await _context.Customers.AnyAsync(c => c.Phone == customer.Phone))
            {
                ModelState.AddModelError("Phone", "Phone number already exists.");
                return ValidationProblem(ModelState);
            }

            try
            {
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                var resultDto = new CustomerDto
                {
                    CustomerId = customer.CustomerId,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Phone = customer.Phone,
                    Email = customer.Email,
                    Address = customer.Address,
                    City = customer.City,
                    CreatedAt = customer.CreatedAt,
                    CreatedByUserId = customer.CreatedByUserId
                };

                return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, resultDto);
            }
            catch (DbUpdateException)
            {
                // Do not expose SQL details
                return Conflict(new { message = "A database error occurred while creating the customer." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // PUT: api/customers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
        {
            if (dto == null)
                return BadRequest("Request body is required.");

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound();

            // Apply updates
            customer.FirstName = dto.FirstName;
            customer.LastName = dto.LastName;
            customer.Phone = dto.Phone;
            customer.Email = dto.Email;
            customer.Address = dto.Address;
            customer.City = dto.City;

            // Validate using model attributes
            TryValidateModel(customer);
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // Server-side validation consistent with MVC Edit
            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                ModelState.AddModelError("Email", "Email is required.");
                return ValidationProblem(ModelState);
            }
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(customer.Email))
            {
                ModelState.AddModelError("Email", "Invalid email format.");
                return ValidationProblem(ModelState);
            }
            if (await _context.Customers.AnyAsync(c => c.Email == customer.Email && c.CustomerId != customer.CustomerId))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return ValidationProblem(ModelState);
            }
            if (!string.IsNullOrWhiteSpace(customer.Phone) && await _context.Customers.AnyAsync(c => c.Phone == customer.Phone && c.CustomerId != customer.CustomerId))
            {
                ModelState.AddModelError("Phone", "Phone number already exists.");
                return ValidationProblem(ModelState);
            }

            try
            {
                _context.Entry(customer).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Customers.AnyAsync(e => e.CustomerId == id))
                    return NotFound();
                return Conflict(new { message = "A concurrency error occurred while updating the customer." });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A database error occurred while updating the customer." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // DELETE: api/customers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Vehicles)
                .Include(c => c.JobOrders)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
                return NotFound();

            var hasVehicles = customer.Vehicles != null && customer.Vehicles.Any();
            var hasJobs = customer.JobOrders != null && customer.JobOrders.Any();
            if (hasVehicles || hasJobs)
            {
                return Conflict(new { message = "Cannot delete this customer because related vehicles or job orders exist. Please remove related records first or deactivate the customer." });
            }

            try
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A database error occurred while deleting the customer." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // DTOs - placed here to avoid modifying other files
        public class CustomerDto
        {
            public int CustomerId { get; set; }
            public int? CreatedByUserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string? LastName { get; set; }
            public string Phone { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
            public DateTime? CreatedAt { get; set; }
        }

        public class CreateCustomerDto
        {
            public int? CreatedByUserId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string? LastName { get; set; }
            public string Phone { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
        }

        public class UpdateCustomerDto
        {
            public string FirstName { get; set; } = string.Empty;
            public string? LastName { get; set; }
            public string Phone { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
        }
    }
}

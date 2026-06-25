using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Filters;
using AutoRepairERD.Models;
using AutoRepairERD.Models.ViewModels;
using Microsoft.Extensions.Configuration;

namespace AutoRepairERD.Controllers
{
    [SessionAuthorize]
    [RoleAuthorize("Owner", "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AdminController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<IActionResult> Settings()
        {
            ViewData["ActiveNav"] = "settings";

            var vm = new SettingsViewModel
            {
                PayrollCompletenessThreshold = _config["Payroll:CompletenessThreshold"] ?? "0.9",
                TotalUsers = await _context.Users.CountAsync(),
                TotalRoles = await _context.Roles.CountAsync(),
                TotalEmployees = await _context.Employees.CountAsync(),
                ConnectionDatabase = _config.GetConnectionString("DefaultConnection") ?? ""
            };

            return View(vm);
        }
    }
}

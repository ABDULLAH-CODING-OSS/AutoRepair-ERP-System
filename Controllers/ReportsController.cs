using System;
using Microsoft.AspNetCore.Mvc;
using AutoRepairERD.Filters;
using AutoRepairERD.Services;

namespace AutoRepairERD.Controllers
{
    [SessionAuthorize]
    [RoleAuthorize("Owner", "Admin")]
    public class ReportsController : Controller
    {
        private readonly ReportingService _reports;

        public ReportsController(ReportingService reports)
        {
            _reports = reports;
        }

        public async Task<IActionResult> Sales(DateTime? from, DateTime? to)
        {
            ViewData["ActiveNav"] = "rep-sales";
            var vm = await _reports.GetSalesReportAsync(from, to);
            return View(vm);
        }

        public async Task<IActionResult> Parts()
        {
            ViewData["ActiveNav"] = "rep-parts";
            var vm = await _reports.GetPartsReportAsync();
            return View(vm);
        }

        public async Task<IActionResult> Outstanding()
        {
            ViewData["ActiveNav"] = "rep-out";
            var vm = await _reports.GetOutstandingReportAsync();
            return View(vm);
        }

        public async Task<IActionResult> TopServices()
        {
            ViewData["ActiveNav"] = "rep-top";
            var vm = await _reports.GetTopServicesReportAsync();
            return View(vm);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using AutoRepairERD.Filters;
using AutoRepairERD.Services;
using AutoRepairERD.Models.ViewModels;

namespace AutoRepairERD.Controllers
{
    [SessionAuthorize]
    public class DashboardController : Controller
    {
        private readonly DashboardService _dashboard;

        public DashboardController(DashboardService dashboard)
        {
            _dashboard = dashboard;
        }

        [RoleAuthorize("Owner", "Admin")]
        public async Task<IActionResult> Owner()
        {
            var vm = await _dashboard.GetOwnerDashboardAsync();
            ViewData["ActiveNav"] = "owner-dash";
            return View(vm);
        }

        public async Task<IActionResult> Admin()
        {
            // Admin dashboard is also open to Owner, so Owner can see system-health metrics too.
            var vm = await _dashboard.GetAdminDashboardAsync();
            ViewData["ActiveNav"] = "admin-dash";
            return View(vm);
        }

        [RoleAuthorize("Owner", "Admin", "Service Advisor")]
        public async Task<IActionResult> Advisor()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("RoleName") ?? "";
            ViewData["ActiveNav"] = "advisor-dash";

            if (role == "Service Advisor" && userId.HasValue)
            {
                var vm = await _dashboard.GetAdvisorDashboardAsync(userId.Value);
                if (vm == null)
                {
                    TempData["Error"] = "No employee record is linked to this account yet.";
                    return RedirectToAction("Index", "Home");
                }
                return View(vm);
            }

            // Owner/Admin viewing the advisor dashboard in preview mode: show real figures for
            // the first Service Advisor on staff, or an empty state if none exist yet.
            var advisorUserId = await _dashboard.GetFirstUserIdForDesignationAsync("Service Advisor");
            var previewVm = advisorUserId.HasValue ? await _dashboard.GetAdvisorDashboardAsync(advisorUserId.Value) : null;
            return View(previewVm ?? new AdvisorDashboardViewModel { AdvisorName = "No Service Advisor on staff yet" });
        }

        [RoleAuthorize("Owner", "Admin", "Mechanic")]
        public async Task<IActionResult> Mechanic()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var role = HttpContext.Session.GetString("RoleName") ?? "";
            ViewData["ActiveNav"] = "mech-dash";

            if (role == "Mechanic" && userId.HasValue)
            {
                var vm = await _dashboard.GetMechanicDashboardAsync(userId.Value);
                if (vm == null)
                {
                    TempData["Error"] = "No employee record is linked to this account yet.";
                    return RedirectToAction("Index", "Home");
                }
                return View(vm);
            }

            var mechanicUserId = await _dashboard.GetFirstUserIdForDesignationAsync("Mechanic");
            var previewVm = mechanicUserId.HasValue ? await _dashboard.GetMechanicDashboardAsync(mechanicUserId.Value) : null;
            return View(previewVm ?? new MechanicDashboardViewModel { MechanicName = "No Mechanic on staff yet" });
        }
    }
}

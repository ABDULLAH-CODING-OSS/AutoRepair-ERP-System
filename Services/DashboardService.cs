using System;
using System.Linq;
using System.Threading.Tasks;
using AutoRepairERD.Models;
using AutoRepairERD.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Services
{
    /// <summary>
    /// Computes the KPI figures shown on the four role-based dashboards. All numbers come
    /// straight from the database - nothing here is hardcoded sample data.
    /// </summary>
    public class DashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<OwnerDashboardViewModel> GetOwnerDashboardAsync()
        {
            var now = DateTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            var vm = new OwnerDashboardViewModel
            {
                RevenueThisMonth = await _context.Invoices
                    .Where(i => i.InvoiceDate >= startOfThisMonth)
                    .SumAsync(i => (decimal?)i.GrandTotal) ?? 0m,

                RevenueLastMonth = await _context.Invoices
                    .Where(i => i.InvoiceDate >= startOfLastMonth && i.InvoiceDate < startOfThisMonth)
                    .SumAsync(i => (decimal?)i.GrandTotal) ?? 0m,

                ActiveJobOrders = await _context.JobOrders
                    .CountAsync(j => j.Status != "Completed" && j.Status != "Cancelled"),

                CompletedJobOrdersThisMonth = await _context.JobOrders
                    .CountAsync(j => j.Status == "Completed" && j.CompletionDate >= startOfThisMonth),

                TotalCustomers = await _context.Customers.CountAsync(),
                TotalEmployees = await _context.Employees.CountAsync(e => e.IsActive == true),
                LowStockAlerts = await _context.LowStockAlerts.CountAsync(a => a.Status == "Active"),
            };

            var totalPaid = await _context.Payments.SumAsync(p => (decimal?)p.AmountPaid) ?? 0m;
            var totalInvoiced = await _context.Invoices.SumAsync(i => (decimal?)i.GrandTotal) ?? 0m;
            vm.OutstandingBalance = totalInvoiced - totalPaid;
            if (vm.OutstandingBalance < 0) vm.OutstandingBalance = 0;

            // Last 6 months revenue trend
            for (int i = 5; i >= 0; i--)
            {
                var monthStart = startOfThisMonth.AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);
                var sum = await _context.Invoices
                    .Where(inv => inv.InvoiceDate >= monthStart && inv.InvoiceDate < monthEnd)
                    .SumAsync(inv => (decimal?)inv.GrandTotal) ?? 0m;
                vm.RevenueTrend.Add(new MonthPoint { Label = monthStart.ToString("MMM"), Value = sum });
            }

            vm.RecentJobOrders = await _context.JobOrders
                .Include(j => j.Customer)
                .Include(j => j.Vehicle)
                .OrderByDescending(j => j.CreatedAt)
                .Take(5)
                .ToListAsync();

            vm.RecentInvoices = await _context.Invoices
                .Include(inv => inv.JobOrder).ThenInclude(jo => jo.Customer)
                .OrderByDescending(inv => inv.InvoiceDate)
                .Take(5)
                .ToListAsync();

            return vm;
        }

        public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
        {
            var sevenDaysAgo = DateTime.Now.AddDays(-7);

            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(u => u.IsActive == true),
                TotalEmployees = await _context.Employees.CountAsync(),
                ActiveEmployees = await _context.Employees.CountAsync(e => e.IsActive == true),
                RolesCount = await _context.Roles.CountAsync(),
                RecentAuditLogCount = await _context.AuditLogs.CountAsync(a => a.ActionDate >= sevenDaysAgo),
            };

            vm.RecentAuditLogs = await _context.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.ActionDate)
                .Take(8)
                .ToListAsync();

            vm.RecentNotifications = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(8)
                .ToListAsync();

            return vm;
        }

        public async Task<AdvisorDashboardViewModel?> GetAdvisorDashboardAsync(int userId)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null) return null;

            var startOfThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            var vm = new AdvisorDashboardViewModel
            {
                AdvisorName = $"{employee.FirstName} {employee.LastName}",
                MyOpenJobs = await _context.JobOrders.CountAsync(j =>
                    j.AdvisorId == employee.EmployeeId && j.Status != "Completed" && j.Status != "Cancelled"),
                MyCompletedJobsThisMonth = await _context.JobOrders.CountAsync(j =>
                    j.AdvisorId == employee.EmployeeId && j.Status == "Completed" && j.CompletionDate >= startOfThisMonth),
                PendingInvoices = await _context.Invoices.CountAsync(i => i.PaymentStatus != "Paid"),
            };

            vm.MyRecentJobs = await _context.JobOrders
                .Where(j => j.AdvisorId == employee.EmployeeId)
                .Include(j => j.Customer)
                .Include(j => j.Vehicle)
                .Include(j => j.Mechanic)
                .OrderByDescending(j => j.CreatedAt)
                .Take(6)
                .ToListAsync();

            vm.RecentCustomers = await _context.Customers
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync();

            return vm;
        }

        /// <summary>Used when a manager previews a role dashboard but isn't that role themselves.</summary>
        public async Task<int?> GetFirstUserIdForDesignationAsync(string designation)
        {
            var employee = await _context.Employees
                .Where(e => e.Designation == designation && e.IsActive == true && e.UserId != null)
                .OrderBy(e => e.EmployeeId)
                .FirstOrDefaultAsync();
            return employee?.UserId;
        }

        public async Task<MechanicDashboardViewModel?> GetMechanicDashboardAsync(int userId)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null) return null;

            var today = DateOnly.FromDateTime(DateTime.Now);
            var startOfThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            var vm = new MechanicDashboardViewModel
            {
                MechanicName = $"{employee.FirstName} {employee.LastName}",
                AssignedPending = await _context.JobOrders.CountAsync(j => j.MechanicId == employee.EmployeeId && j.Status == "Pending"),
                AssignedInProgress = await _context.JobOrders.CountAsync(j => j.MechanicId == employee.EmployeeId && j.Status == "In Progress"),
                CompletedThisMonth = await _context.JobOrders.CountAsync(j => j.MechanicId == employee.EmployeeId && j.Status == "Completed" && j.CompletionDate >= startOfThisMonth),
            };

            vm.TodayAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId && a.AttendanceDate == today);
            vm.CheckedInToday = vm.TodayAttendance != null;

            vm.MyJobs = await _context.JobOrders
                .Where(j => j.MechanicId == employee.EmployeeId && j.Status != "Completed" && j.Status != "Cancelled")
                .Include(j => j.Customer)
                .Include(j => j.Vehicle)
                .OrderByDescending(j => j.CreatedAt)
                .Take(6)
                .ToListAsync();

            return vm;
        }
    }
}

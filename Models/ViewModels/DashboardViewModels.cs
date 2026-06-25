using System;
using System.Collections.Generic;
using AutoRepairERD.Models;

namespace AutoRepairERD.Models.ViewModels
{
    public class MonthPoint
    {
        public string Label { get; set; } = "";
        public decimal Value { get; set; }
    }

    public class OwnerDashboardViewModel
    {
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueLastMonth { get; set; }
        public decimal OutstandingBalance { get; set; }
        public int ActiveJobOrders { get; set; }
        public int CompletedJobOrdersThisMonth { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalEmployees { get; set; }
        public int LowStockAlerts { get; set; }
        public List<MonthPoint> RevenueTrend { get; set; } = new();
        public List<JobOrder> RecentJobOrders { get; set; } = new();
        public List<Invoice> RecentInvoices { get; set; } = new();
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int RolesCount { get; set; }
        public int RecentAuditLogCount { get; set; }
        public List<AuditLog> RecentAuditLogs { get; set; } = new();
        public List<Notification> RecentNotifications { get; set; } = new();
    }

    public class AdvisorDashboardViewModel
    {
        public string AdvisorName { get; set; } = "";
        public int MyOpenJobs { get; set; }
        public int MyCompletedJobsThisMonth { get; set; }
        public int PendingInvoices { get; set; }
        public List<JobOrder> MyRecentJobs { get; set; } = new();
        public List<Customer> RecentCustomers { get; set; } = new();
    }

    public class MechanicDashboardViewModel
    {
        public string MechanicName { get; set; } = "";
        public int AssignedPending { get; set; }
        public int AssignedInProgress { get; set; }
        public int CompletedThisMonth { get; set; }
        public bool CheckedInToday { get; set; }
        public Attendance? TodayAttendance { get; set; }
        public List<JobOrder> MyJobs { get; set; } = new();
    }
}

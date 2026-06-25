using System;
using System.Collections.Generic;

namespace AutoRepairERD.Models.ViewModels
{
    public class SalesReportViewModel
    {
        // Aliases used by views
        public DateTime? From { get; set; }
        public DateTime? To   { get; set; }

        // Keep originals for controller compatibility
        public DateTime FromDate { get => From ?? DateTime.MinValue; set => From = value; }
        public DateTime ToDate   { get => To   ?? DateTime.MinValue; set => To   = value; }

        public decimal TotalSales { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int InvoiceCount { get; set; }
        public List<MonthPoint> DailyTrend { get; set; } = new();
        public List<Invoice> Invoices { get; set; } = new();
    }

    public class PartConsumptionRow
    {
        public string PartName { get; set; } = "";
        // Views use PartNumber; Sku kept as alias
        public string? PartNumber { get; set; }
        public string? Sku { get => PartNumber; set => PartNumber = value; }
        public string? CategoryName { get; set; }
        // Views use TotalQty / TotalRevenue / JobCount
        public int TotalQty { get; set; }
        public int JobCount { get; set; }
        public decimal TotalRevenue { get; set; }
        // Keep originals for controller compatibility
        public int QuantityUsed { get => TotalQty; set => TotalQty = value; }
        public decimal RevenueGenerated { get => TotalRevenue; set => TotalRevenue = value; }
        public int CurrentStock { get; set; }
    }

    public class PartsReportViewModel
    {
        public List<PartConsumptionRow> Rows { get; set; } = new();
        public int TotalQuantityUsed { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class OutstandingRow
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public DateTime? InvoiceDate { get; set; }
        // Views use CustomerId (int) and CustomerName
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public string? CustomerPhone { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public int DaysOutstanding { get; set; }
        // Views use PaymentStatus
        public string? PaymentStatus { get; set; }
    }

    public class OutstandingReportViewModel
    {
        public List<OutstandingRow> Rows { get; set; } = new();
        public decimal TotalOutstanding { get; set; }
    }

    public class TopServiceRow
    {
        public string ServiceName { get; set; } = "";
        // Views use JobCount and Category
        public int JobCount { get; set; }
        public string? Category { get; set; }
        // Keep originals for controller compatibility
        public int TimesPerformed { get => JobCount; set => JobCount = value; }
        public decimal TotalHours { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class TopServicesReportViewModel
    {
        public List<TopServiceRow> Rows { get; set; } = new();
    }

    public class SettingsViewModel
    {
        public string PayrollCompletenessThreshold { get; set; } = "0.9";
        public int TotalUsers { get; set; }
        public int TotalRoles { get; set; }
        public int TotalEmployees { get; set; }
        public string ConnectionDatabase { get; set; } = "";
    }
}

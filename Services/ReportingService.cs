using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoRepairERD.Models;
using AutoRepairERD.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Services
{
    /// <summary>
    /// Builds the four management reports (Sales, Parts Consumption, Outstanding Balances,
    /// Top Services) directly from the live database.
    /// </summary>
    public class ReportingService
    {
        private readonly ApplicationDbContext _context;

        public ReportingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SalesReportViewModel> GetSalesReportAsync(DateTime? from, DateTime? to)
        {
            var fromDate = from ?? DateTime.Now.AddDays(-29).Date;
            var toDate = (to ?? DateTime.Now.Date).Date.AddDays(1).AddTicks(-1);

            var invoices = await _context.Invoices
                .Include(i => i.JobOrder).ThenInclude(j => j.Customer)
                .Include(i => i.Payments)
                .Where(i => i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            var vm = new SalesReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Invoices = invoices,
                InvoiceCount = invoices.Count,
                TotalSales = invoices.Sum(i => i.GrandTotal),
                TotalPaid = invoices.Sum(i => i.Payments.Sum(p => p.AmountPaid)),
            };
            vm.TotalOutstanding = vm.TotalSales - vm.TotalPaid;
            if (vm.TotalOutstanding < 0) vm.TotalOutstanding = 0;

            // Daily trend (cap to last 31 buckets max, grouped by date)
            var byDay = invoices
                .Where(i => i.InvoiceDate.HasValue)
                .GroupBy(i => i.InvoiceDate!.Value.Date)
                .OrderBy(g => g.Key)
                .Select(g => new MonthPoint { Label = g.Key.ToString("dd MMM"), Value = g.Sum(x => x.GrandTotal) });
            vm.DailyTrend = byDay.ToList();

            return vm;
        }

        public async Task<PartsReportViewModel> GetPartsReportAsync()
        {
            var rows = await _context.JobPartItems
                .Include(jp => jp.Part).ThenInclude(p => p.Category)
                .GroupBy(jp => jp.Part)
                .Select(g => new PartConsumptionRow
                {
                    PartName = g.Key.PartName,
                    Sku = g.Key.Sku,
                    CategoryName = g.Key.Category != null ? g.Key.Category.CategoryName : null,
                    QuantityUsed = g.Sum(x => x.Quantity ?? 0),
                    RevenueGenerated = g.Sum(x => x.TotalPrice ?? 0),
                    CurrentStock = g.Key.CurrentStock ?? 0
                })
                .OrderByDescending(r => r.QuantityUsed)
                .ToListAsync();

            return new PartsReportViewModel
            {
                Rows = rows,
                TotalQuantityUsed = rows.Sum(r => r.QuantityUsed),
                TotalRevenue = rows.Sum(r => r.RevenueGenerated)
            };
        }

        public async Task<OutstandingReportViewModel> GetOutstandingReportAsync()
        {
            var invoices = await _context.Invoices
                .Include(i => i.JobOrder).ThenInclude(j => j.Customer)
                .Include(i => i.Payments)
                .Where(i => i.PaymentStatus != "Paid")
                .ToListAsync();

            var rows = invoices
                .Select(i =>
                {
                    var paid = i.Payments.Sum(p => p.AmountPaid);
                    var balance = i.GrandTotal - paid;
                    return new OutstandingRow
                    {
                        InvoiceId = i.InvoiceId,
                        InvoiceNumber = i.InvoiceNumber,
                        InvoiceDate = i.InvoiceDate,
                        CustomerName = i.JobOrder?.Customer != null
                            ? $"{i.JobOrder.Customer.FirstName} {i.JobOrder.Customer.LastName}".Trim()
                            : "—",
                        CustomerPhone = i.JobOrder?.Customer?.Phone,
                        GrandTotal = i.GrandTotal,
                        AmountPaid = paid,
                        Balance = balance,
                        DaysOutstanding = i.InvoiceDate.HasValue ? (int)(DateTime.Now - i.InvoiceDate.Value).TotalDays : 0
                    };
                })
                .Where(r => r.Balance > 0)
                .OrderByDescending(r => r.Balance)
                .ToList();

            return new OutstandingReportViewModel
            {
                Rows = rows,
                TotalOutstanding = rows.Sum(r => r.Balance)
            };
        }

        public async Task<TopServicesReportViewModel> GetTopServicesReportAsync()
        {
            var rows = await _context.JobServiceItems
                .Include(js => js.Service)
                .GroupBy(js => js.Service)
                .Select(g => new TopServiceRow
                {
                    ServiceName = g.Key.ServiceName,
                    TimesPerformed = g.Count(),
                    TotalHours = g.Sum(x => x.HoursWorked ?? 0),
                    TotalRevenue = g.Sum(x => x.ServicePrice ?? 0)
                })
                .OrderByDescending(r => r.TimesPerformed)
                .ToListAsync();

            return new TopServicesReportViewModel { Rows = rows };
        }
    }
}

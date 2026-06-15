using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AutoRepairERD.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Services
{
    public class NotificationHostedService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public NotificationHostedService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Runs once per day at midnight-ish or every 24 hours from start
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

                    // Rule 25: Customer follow-up reminder (vehicles serviced 60 days ago)
                    // For follow-up reminders, look for JobOrders completed ~60 days ago and notify the Service Advisor
                    var cutoff = DateTime.Now.AddDays(-60);
                    var jobs = await context.JobOrders
                        .Include(j => j.Customer)
                        .Include(j => j.Vehicle)
                        .Where(j => j.CompletionDate != null && j.CompletionDate <= cutoff && j.CompletionDate > cutoff.AddDays(-2))
                        .ToListAsync();

                    foreach (var job in jobs)
                    {
                        var customerName = job.Customer?.FirstName + (string.IsNullOrEmpty(job.Customer?.LastName) ? "" : " " + job.Customer?.LastName);
                        var title = "Customer follow-up reminder";
                        var message = $"Customer {customerName ?? "Unknown"}'s vehicle {job.Vehicle?.LicensePlate ?? "Unknown"} was serviced about 60 days ago. Follow up for satisfaction and retention.";
                        var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Service Advisor");
                        if (role != null)
                        {
                            await notificationService.CreateForRoleAsync(role.RoleId, "Customer Follow-Up", title, message, null, "JobOrder", job.JobOrderId);
                        }
                    }

                    // Rule 29: Daily summary (placeholder)
                    var ownerRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Owner");
                    if (ownerRole != null)
                    {
                        var title = "Daily ERP Summary";
                        var message = "Daily summary generated.";
                        await notificationService.CreateForRoleAsync(ownerRole.RoleId, "System", title, message);
                    }
                }
                catch
                {
                    // swallow errors; do not crash host
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}

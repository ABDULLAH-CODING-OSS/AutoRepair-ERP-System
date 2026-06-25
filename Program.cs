using System;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
// Register SQL Server DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Notification service and hosted background service
builder.Services.AddScoped<AutoRepairERD.Services.NotificationService>();
builder.Services.AddHostedService<AutoRepairERD.Services.NotificationHostedService>();
// Payroll calculation service
builder.Services.AddScoped<AutoRepairERD.Services.PayrollCalculationService>();
// Reporting service (sales / parts / outstanding / top services aggregates)
builder.Services.AddScoped<AutoRepairERD.Services.ReportingService>();
// Dashboard KPI service
builder.Services.AddScoped<AutoRepairERD.Services.DashboardService>();
// Purchase order receiving service (stock-in workflow)
builder.Services.AddScoped<AutoRepairERD.Services.PurchaseOrderReceivingService>();
// Audit service for logging CRUD operations
builder.Services.AddScoped<AutoRepairERD.Services.AuditService>();

var app = builder.Build();

// Attempt to apply migrations and seed database on startup. If model and database are out of sync,
// catch and log the error so the app can still start for local testing.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        AutoRepairERD.Services.DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        // Log to console for developer visibility and continue startup.
        Console.WriteLine("Database migration/seed skipped: " + ex.Message);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
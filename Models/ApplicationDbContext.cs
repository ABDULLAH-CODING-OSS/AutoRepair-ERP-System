using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<JobOrder> JobOrders { get; set; }

    public virtual DbSet<JobPartItem> JobPartItems { get; set; }

    public virtual DbSet<JobServiceItem> JobServiceItems { get; set; }

    public virtual DbSet<LowStockAlert> LowStockAlerts { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Part> Parts { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Payroll> Payrolls { get; set; }

    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    public virtual DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SalaryAdjustment> SalaryAdjustments { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<StockTransaction> StockTransactions { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=AutoRepairERPDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69263C2B9A16D2");

            entity.HasOne(d => d.Employee).WithMany(p => p.Attendances)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Employees");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("PK__AuditLog__EB5F6CDDD537CB6A");

            entity.Property(e => e.ActionDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs).HasConstraintName("FK_AuditLogs_Users");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A2BF0AAFA7B");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64B813CB37ED");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Customers).HasConstraintName("FK_Customers_Users");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04FF1C8D2292F");

            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.User).WithMany(p => p.Employees)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employees_Users");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoices__D796AAD5FE6A02AF");

            entity.Property(e => e.InvoiceDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.InvoiceStatus).HasDefaultValue("Unpaid");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Invoices).HasConstraintName("FK_Invoices_Users");

            entity.HasOne(d => d.JobOrder).WithMany(p => p.Invoices)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_JobOrders");
        });

        modelBuilder.Entity<JobOrder>(entity =>
        {
            entity.HasKey(e => e.JobOrderId).HasName("PK__JobOrder__EACFC526F9FCB543");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.Advisor).WithMany(p => p.JobOrderAdvisors).HasConstraintName("FK_JobOrders_Advisor");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.JobOrders).HasConstraintName("FK_JobOrders_Users");

            entity.HasOne(d => d.Customer).WithMany(p => p.JobOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobOrders_Customers");

            entity.HasOne(d => d.Mechanic).WithMany(p => p.JobOrderMechanics).HasConstraintName("FK_JobOrders_Mechanic");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.JobOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobOrders_Vehicles");
        });

        modelBuilder.Entity<JobPartItem>(entity =>
        {
            entity.HasKey(e => e.JobPartItemId).HasName("PK__JobPartI__B000CCF49CD43A33");

            entity.HasOne(d => d.JobOrder).WithMany(p => p.JobPartItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobPartItems_JobOrders");

            entity.HasOne(d => d.Part).WithMany(p => p.JobPartItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobPartItems_Parts");
        });

        modelBuilder.Entity<JobServiceItem>(entity =>
        {
            entity.HasKey(e => e.JobServiceItemId).HasName("PK__JobServi__70C9F8ED2CFAF40B");

            entity.HasOne(d => d.JobOrder).WithMany(p => p.JobServiceItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobServiceItems_JobOrders");

            entity.HasOne(d => d.Mechanic).WithMany(p => p.JobServiceItems).HasConstraintName("FK_JobServiceItems_Employees");

            entity.HasOne(d => d.Service).WithMany(p => p.JobServiceItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobServiceItems_Services");
        });

        modelBuilder.Entity<LowStockAlert>(entity =>
        {
            entity.HasKey(e => e.AlertId).HasName("PK__LowStock__EBB16AED77FCB95F");

            entity.Property(e => e.AlertDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Active");

            entity.HasOne(d => d.Part).WithMany(p => p.LowStockAlerts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LowStockAlerts_Parts");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E3260D0BFA9");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasConstraintName("FK_Notifications_Users");
        });

        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasKey(e => e.PartId).HasName("PK__Parts__7C3F0D306206254D");

            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Category).WithMany(p => p.Parts).HasConstraintName("FK_Parts_Categories");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Parts).HasConstraintName("FK_Parts_Suppliers");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A58A4F891EB");

            entity.Property(e => e.PaymentDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Invoices");
        });

        modelBuilder.Entity<Payroll>(entity =>
        {
            entity.HasKey(e => e.PayrollId).HasName("PK__Payrolls__99DFC69208724628");

            entity.Property(e => e.PayrollStatus).HasDefaultValue("Pending");

            entity.HasOne(d => d.Employee).WithMany(p => p.Payrolls)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payrolls_Employees");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.PurchaseOrderId).HasName("PK__Purchase__036BAC44A7BE89C8");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.PurchaseOrders).HasConstraintName("FK_PurchaseOrders_Users");

            entity.HasOne(d => d.Supplier).WithMany(p => p.PurchaseOrders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseOrders_Suppliers");
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(e => e.PoitemId).HasName("PK__Purchase__CA5147B0CB59FC19");

            entity.HasOne(d => d.Part).WithMany(p => p.PurchaseOrderItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseOrderItems_Parts");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.PurchaseOrderItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseOrderItems_PurchaseOrders");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3ACDE88543");
        });

        modelBuilder.Entity<SalaryAdjustment>(entity =>
        {
            entity.HasKey(e => e.AdjustmentId).HasName("PK__SalaryAd__E60DB8B343D40294");

            entity.HasOne(d => d.Payroll).WithMany(p => p.SalaryAdjustments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalaryAdjustments_Payrolls");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Services__C51BB0EAB8B49EF2");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__StockTra__55433A4B908A0EDE");

            entity.Property(e => e.TransactionDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Part).WithMany(p => p.StockTransactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockTransactions_Parts");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__4BE666948E86EA7D");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC0C2340C3");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.UserRoleId).HasName("PK__UserRole__3D978A55C3819B05");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Roles");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Users");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId).HasName("PK__Vehicles__476B54B2ECBEFA68");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Vehicles).HasConstraintName("FK_Vehicles_Users");

            entity.HasOne(d => d.Customer).WithMany(p => p.Vehicles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vehicles_Customers");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}


using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoRepairERP.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Categori__19093A2BF0AAFA7B", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Roles__8AFACE3ACDE88543", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    ServiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    StandardHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FixedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Services__C51BB0EAB8B49EF2", x => x.ServiceID);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    SupplierID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ContactPerson = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Supplier__4BE666948E86EA7D", x => x.SupplierID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    PasswordHash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__1788CCAC0C2340C3", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "Parts",
                columns: table => new
                {
                    PartID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryID = table.Column<int>(type: "int", nullable: true),
                    SupplierID = table.Column<int>(type: "int", nullable: true),
                    SKU = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    PartName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentStock = table.Column<int>(type: "int", nullable: true),
                    ReorderLevel = table.Column<int>(type: "int", nullable: true),
                    Unit = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    RackLocation = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Parts__7C3F0D306206254D", x => x.PartID);
                    table.ForeignKey(
                        name: "FK_Parts_Categories",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID");
                    table.ForeignKey(
                        name: "FK_Parts_Suppliers",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierID");
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    TableName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    RecordID = table.Column<int>(type: "int", nullable: true),
                    ActionType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    OldValues = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    NewValues = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    ActionDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    IPAddress = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AuditLog__EB5F6CDDD537CB6A", x => x.AuditLogID);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedByUserID = table.Column<int>(type: "int", nullable: true),
                    FirstName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    City = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Customer__A4AE64B813CB37ED", x => x.CustomerID);
                    table.ForeignKey(
                        name: "FK_Customers_Users",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    EmployeeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    EmployeeCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FirstName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    CNIC = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Designation = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    BasicSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Employee__7AD04FF1C8D2292F", x => x.EmployeeID);
                    table.ForeignKey(
                        name: "FK_Employees_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Message = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    NotificationType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Notifica__20CF2E3260D0BFA9", x => x.NotificationID);
                    table.ForeignKey(
                        name: "FK_Notifications_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    PurchaseOrderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierID = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserID = table.Column<int>(type: "int", nullable: true),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpectedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Purchase__036BAC44A7BE89C8", x => x.PurchaseOrderID);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers",
                        column: x => x.SupplierID,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierID");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Users",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserRoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserRole__3D978A55C3819B05", x => x.UserRoleID);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID");
                    table.ForeignKey(
                        name: "FK_UserRoles_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "LowStockAlerts",
                columns: table => new
                {
                    AlertID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartID = table.Column<int>(type: "int", nullable: false),
                    CurrentQuantity = table.Column<int>(type: "int", nullable: true),
                    ReorderLevel = table.Column<int>(type: "int", nullable: true),
                    AlertDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__LowStock__EBB16AED77FCB95F", x => x.AlertID);
                    table.ForeignKey(
                        name: "FK_LowStockAlerts_Parts",
                        column: x => x.PartID,
                        principalTable: "Parts",
                        principalColumn: "PartID");
                });

            migrationBuilder.CreateTable(
                name: "StockTransactions",
                columns: table => new
                {
                    TransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartID = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    PreviousStock = table.Column<int>(type: "int", nullable: true),
                    NewStock = table.Column<int>(type: "int", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StockTra__55433A4B908A0EDE", x => x.TransactionID);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Parts",
                        column: x => x.PartID,
                        principalTable: "Parts",
                        principalColumn: "PartID");
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    VehicleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserID = table.Column<int>(type: "int", nullable: true),
                    LicensePlate = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    VIN = table.Column<string>(type: "varchar(17)", unicode: false, maxLength: 17, nullable: true),
                    Make = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ManufacturingYear = table.Column<int>(type: "int", nullable: true),
                    Color = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Mileage = table.Column<int>(type: "int", nullable: true),
                    EngineNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Vehicles__476B54B2ECBEFA68", x => x.VehicleID);
                    table.ForeignKey(
                        name: "FK_Vehicles_Customers",
                        column: x => x.CustomerID,
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_Vehicles_Users",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Attendance",
                columns: table => new
                {
                    AttendanceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeID = table.Column<int>(type: "int", nullable: false),
                    AttendanceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CheckInTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CheckOutTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    OvertimeHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Attendan__8B69263C2B9A16D2", x => x.AttendanceID);
                    table.ForeignKey(
                        name: "FK_Attendance_Employees",
                        column: x => x.EmployeeID,
                        principalTable: "Employees",
                        principalColumn: "EmployeeID");
                });

            migrationBuilder.CreateTable(
                name: "Payrolls",
                columns: table => new
                {
                    PayrollID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeID = table.Column<int>(type: "int", nullable: false),
                    PayrollMonth = table.Column<int>(type: "int", nullable: true),
                    PayrollYear = table.Column<int>(type: "int", nullable: true),
                    PayrollNumber = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true),
                    TotalWorkingDays = table.Column<int>(type: "int", nullable: true),
                    TotalPresentDays = table.Column<int>(type: "int", nullable: true),
                    OvertimeHours = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BonusAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeductionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GrossSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NetSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PayrollStatus = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Payrolls__99DFC69208724628", x => x.PayrollID);
                    table.ForeignKey(
                        name: "FK_Payrolls_Employees",
                        column: x => x.EmployeeID,
                        principalTable: "Employees",
                        principalColumn: "EmployeeID");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                columns: table => new
                {
                    POItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseOrderID = table.Column<int>(type: "int", nullable: false),
                    PartID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Purchase__CA5147B0CB59FC19", x => x.POItemID);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_Parts",
                        column: x => x.PartID,
                        principalTable: "Parts",
                        principalColumn: "PartID");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders",
                        column: x => x.PurchaseOrderID,
                        principalTable: "PurchaseOrders",
                        principalColumn: "PurchaseOrderID");
                });

            migrationBuilder.CreateTable(
                name: "JobOrders",
                columns: table => new
                {
                    JobOrderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerID = table.Column<int>(type: "int", nullable: false),
                    VehicleID = table.Column<int>(type: "int", nullable: false),
                    AdvisorID = table.Column<int>(type: "int", nullable: true),
                    MechanicID = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserID = table.Column<int>(type: "int", nullable: true),
                    JobNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ComplaintDescription = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    DiagnosisNotes = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    EstimatedCompletionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "Pending"),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FinalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__JobOrder__EACFC526F9FCB543", x => x.JobOrderID);
                    table.ForeignKey(
                        name: "FK_JobOrders_Advisor",
                        column: x => x.AdvisorID,
                        principalTable: "Employees",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_JobOrders_Customers",
                        column: x => x.CustomerID,
                        principalTable: "Customers",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_JobOrders_Mechanic",
                        column: x => x.MechanicID,
                        principalTable: "Employees",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_JobOrders_Users",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_JobOrders_Vehicles",
                        column: x => x.VehicleID,
                        principalTable: "Vehicles",
                        principalColumn: "VehicleID");
                });

            migrationBuilder.CreateTable(
                name: "SalaryAdjustments",
                columns: table => new
                {
                    AdjustmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollID = table.Column<int>(type: "int", nullable: false),
                    AdjustmentType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Reason = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SalaryAd__E60DB8B343D40294", x => x.AdjustmentID);
                    table.ForeignKey(
                        name: "FK_SalaryAdjustments_Payrolls",
                        column: x => x.PayrollID,
                        principalTable: "Payrolls",
                        principalColumn: "PayrollID");
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    InvoiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobOrderID = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserID = table.Column<int>(type: "int", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InvoiceStatus = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "Unpaid"),
                    PaymentStatus = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Invoices__D796AAD5FE6A02AF", x => x.InvoiceID);
                    table.ForeignKey(
                        name: "FK_Invoices_JobOrders",
                        column: x => x.JobOrderID,
                        principalTable: "JobOrders",
                        principalColumn: "JobOrderID");
                    table.ForeignKey(
                        name: "FK_Invoices_Users",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "JobPartItems",
                columns: table => new
                {
                    JobPartItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobOrderID = table.Column<int>(type: "int", nullable: false),
                    PartID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__JobPartI__B000CCF49CD43A33", x => x.JobPartItemID);
                    table.ForeignKey(
                        name: "FK_JobPartItems_JobOrders",
                        column: x => x.JobOrderID,
                        principalTable: "JobOrders",
                        principalColumn: "JobOrderID");
                    table.ForeignKey(
                        name: "FK_JobPartItems_Parts",
                        column: x => x.PartID,
                        principalTable: "Parts",
                        principalColumn: "PartID");
                });

            migrationBuilder.CreateTable(
                name: "JobServiceItems",
                columns: table => new
                {
                    JobServiceItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobOrderID = table.Column<int>(type: "int", nullable: false),
                    ServiceID = table.Column<int>(type: "int", nullable: false),
                    MechanicID = table.Column<int>(type: "int", nullable: true),
                    HoursWorked = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ServicePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__JobServi__70C9F8ED2CFAF40B", x => x.JobServiceItemID);
                    table.ForeignKey(
                        name: "FK_JobServiceItems_Employees",
                        column: x => x.MechanicID,
                        principalTable: "Employees",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_JobServiceItems_JobOrders",
                        column: x => x.JobOrderID,
                        principalTable: "JobOrders",
                        principalColumn: "JobOrderID");
                    table.ForeignKey(
                        name: "FK_JobServiceItems_Services",
                        column: x => x.ServiceID,
                        principalTable: "Services",
                        principalColumn: "ServiceID");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceID = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    TransactionReference = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Payments__9B556A58A4F891EB", x => x.PaymentID);
                    table.ForeignKey(
                        name: "FK_Payments_Invoices",
                        column: x => x.InvoiceID,
                        principalTable: "Invoices",
                        principalColumn: "InvoiceID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_EmployeeID",
                table: "Attendance",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserID",
                table: "AuditLogs",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CreatedByUserID",
                table: "Customers",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserID",
                table: "Employees",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ__Employee__1F64254845D6CF98",
                table: "Employees",
                column: "EmployeeCode",
                unique: true,
                filter: "[EmployeeCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedByUserID",
                table: "Invoices",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_JobOrderID",
                table: "Invoices",
                column: "JobOrderID");

            migrationBuilder.CreateIndex(
                name: "UQ__Invoices__D776E981AF49C804",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_AdvisorID",
                table: "JobOrders",
                column: "AdvisorID");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_CreatedByUserID",
                table: "JobOrders",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_CustomerID",
                table: "JobOrders",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_MechanicID",
                table: "JobOrders",
                column: "MechanicID");

            migrationBuilder.CreateIndex(
                name: "IX_JobOrders_VehicleID",
                table: "JobOrders",
                column: "VehicleID");

            migrationBuilder.CreateIndex(
                name: "UQ__JobOrder__A6B9B88202D22806",
                table: "JobOrders",
                column: "JobNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobPartItems_JobOrderID",
                table: "JobPartItems",
                column: "JobOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_JobPartItems_PartID",
                table: "JobPartItems",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_JobServiceItems_JobOrderID",
                table: "JobServiceItems",
                column: "JobOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_JobServiceItems_MechanicID",
                table: "JobServiceItems",
                column: "MechanicID");

            migrationBuilder.CreateIndex(
                name: "IX_JobServiceItems_ServiceID",
                table: "JobServiceItems",
                column: "ServiceID");

            migrationBuilder.CreateIndex(
                name: "IX_LowStockAlerts_PartID",
                table: "LowStockAlerts",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserID",
                table: "Notifications",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Parts_CategoryID",
                table: "Parts",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Parts_SupplierID",
                table: "Parts",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "UQ__Parts__CA1ECF0DF73028DB",
                table: "Parts",
                column: "SKU",
                unique: true,
                filter: "[SKU] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceID",
                table: "Payments",
                column: "InvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_EmployeeID",
                table: "Payrolls",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_PartID",
                table: "PurchaseOrderItems",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderID",
                table: "PurchaseOrderItems",
                column: "PurchaseOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedByUserID",
                table: "PurchaseOrders",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierID",
                table: "PurchaseOrders",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "UQ__Roles__8A2B61603EBC7476",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalaryAdjustments_PayrollID",
                table: "SalaryAdjustments",
                column: "PayrollID");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_PartID",
                table: "StockTransactions",
                column: "PartID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleID",
                table: "UserRoles",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserID",
                table: "UserRoles",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__536C85E47D4D5C4C",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Users__A9D105344D029389",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CreatedByUserID",
                table: "Vehicles",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CustomerID",
                table: "Vehicles",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "UQ__Vehicles__026BC15CBFD16B8F",
                table: "Vehicles",
                column: "LicensePlate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Vehicles__C5DF234C0DA3F9F8",
                table: "Vehicles",
                column: "VIN",
                unique: true,
                filter: "[VIN] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendance");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "JobPartItems");

            migrationBuilder.DropTable(
                name: "JobServiceItems");

            migrationBuilder.DropTable(
                name: "LowStockAlerts");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "SalaryAdjustments");

            migrationBuilder.DropTable(
                name: "StockTransactions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "Payrolls");

            migrationBuilder.DropTable(
                name: "Parts");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "JobOrders");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

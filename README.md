# AutoRepair ERP — Merged Project

A full-featured Workshop Management System built with **ASP.NET Core MVC (.NET 10)**, **EF Core 10**, and **SQL Server / LocalDB**.

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | **10.0** | `dotnet --version` should show `10.x.x` |
| SQL Server | Any edition | or **LocalDB** (ships with Visual Studio) |
| EF Core CLI tools | Latest | `dotnet tool install -g dotnet-ef` |

> The original frontend README incorrectly stated .NET 8. This project targets **net10.0**.

---

## 1 — Configure the Connection String

Open `appsettings.json` and update `DefaultConnection`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AutoRepairERPDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

For a full SQL Server instance, replace `(localdb)\\MSSQLLocalDB` with your server name.

---

## 2 — Create the Database

```bash
# From the project root (same folder as AutoRepairERP.csproj)
dotnet ef migrations add InitialCreate
dotnet ef database update
```

This creates all tables from the EF Core model. The seeder runs automatically on first app startup and creates all roles and 4 demo accounts.

---

## 3 — Run

```bash
dotnet run
```

Open your browser at:

- **HTTP:** http://localhost:5127
- **HTTPS:** https://localhost:7081

The app redirects to `/Auth/Login` automatically.

---

## 4 — Demo Credentials

Password for all accounts: **`Passw0rd!`**

| Username | Role | Sidebar / Access |
|----------|------|-----------------|
| `owner` | Owner | Full access + all reports |
| `admin` | Admin | Users, roles, audit log, settings, all modules |
| `advisor` | Service Advisor | Customers, vehicles, job orders, invoices, own attendance |
| `mechanic` | Mechanic | My assigned jobs, own attendance |

---

## 5 — Project Layout

```
AutoRepairERP.csproj            ← renamed (namespace inside stays AutoRepairERD)
Controllers/
  AttendancesController.cs      ← extended: Mark/CheckIn/CheckOut/History added
  DashboardController.cs        ← NEW: 4 role-based dashboards
  ReportsController.cs          ← NEW: Sales / Parts / Outstanding / TopServices
  PurchaseOrdersController.cs   ← extended: dedicated Receive dock
  AdminController.cs            ← NEW: Settings page
Services/
  DashboardService.cs           ← NEW
  ReportingService.cs           ← NEW
  PurchaseOrderReceivingService.cs ← NEW
  DbSeeder.cs                   ← NEW: idempotent roles + demo users
Helpers/
  BadgeHelper.cs                ← NEW: maps status strings to badge-* CSS classes
Models/ViewModels/              ← DashboardViewModels, ReportViewModels (NEW)
Views/
  Auth/Login.cshtml             ← RESKINNED (video background + glass card)
  Auth/SelectRole.cshtml        ← RESKINNED
  Shared/_Layout.cshtml         ← REWRITTEN (dark gold theme)
  Shared/_SidebarNav.cshtml     ← REWRITTEN (6 roles, real routes, live badges)
  Dashboard/                    ← NEW (Owner / Admin / Advisor / Mechanic)
  Reports/                      ← NEW (Sales / Parts / Outstanding / TopServices)
  Attendances/Management.cshtml ← NEW (admin roster with unmarked employees)
  Attendances/Mark.cshtml       ← NEW (self-service check-in/out)
  Attendances/History.cshtml    ← NEW (own monthly log)
  PurchaseOrders/Receive.cshtml ← NEW (goods-receiving dock)
  Admin/Settings.cshtml         ← NEW
  [all other views]             ← RESKINNED (Bootstrap → AutoRepair CSS)
wwwroot/
  css/site.css                  ← dark-gold design system (~1400 lines)
  js/site.js                    ← car-transition animations, live clock
  videos/mechanic-bg.mp4        ← login page background video
```

---

## 6 — Role Access Matrix

| Role | Dashboard route | Key modules |
|------|----------------|-------------|
| Owner | `/Dashboard/Owner` | Everything + Reports |
| Admin | `/Dashboard/Admin` | Users, Roles, Audit Log, Settings, all modules |
| Service Advisor | `/Dashboard/Advisor` | Customers, Vehicles, Job Orders, Invoices |
| Mechanic | `/Dashboard/Mechanic` | My Assigned Jobs |
| Inventory Manager | `/` (Home) | Parts, Categories, Suppliers, Purchase Orders, Stock |
| Receptionist | `/` (Home) | Customers, Vehicles, Job Orders |

---

## 7 — Known Limitations (Fix Before Production)

1. **Plain-text passwords.** The original backend stored and compared passwords as plain text. Replace with `BCrypt.Net-Next` or `IPasswordHasher<T>` before real use.

2. **Session-based auth.** Sessions expire on server restart. Acceptable for a local workshop server; review if deploying to a farm.

3. **No migrations in repo.** Run `dotnet ef migrations add InitialCreate` before first use (step 2 above). The Migrations folder in the original repo was empty.

4. **EF Core 10 preview NuGet packages** may show warnings on restore — this is expected for a .NET 10 project.

---

## 8 — Build Check

```bash
dotnet build   # 0 errors expected
dotnet run
```

If you see namespace errors, confirm every file uses `namespace AutoRepairERD.*` (not `AutoRepairERP`). Only the `.csproj` filename changed — all internal namespaces intentionally kept as `AutoRepairERD` to avoid breaking `using` references.

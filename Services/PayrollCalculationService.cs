using System;
using System.Linq;
using System.Threading.Tasks;
using AutoRepairERD.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Services
{
    public class PayrollCalculationResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public Payroll? Payroll { get; set; }
        // When recalculation runs, this holds the attendance-computed present days (if available)
        public int? ComputedPresentDays { get; set; }
    }

    public class PayrollCalculationService
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public PayrollCalculationService(ApplicationDbContext context, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<PayrollCalculationResult> CalculatePayrollAsync(int employeeId, int month, int year)
        {
            // Validate month/year
            if (month < 1 || month > 12) return new PayrollCalculationResult { Success = false, Error = "Payroll month is invalid." };
            if (year < 2000 || year > 2100) return new PayrollCalculationResult { Success = false, Error = "Payroll year is invalid." };

            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null) return new PayrollCalculationResult { Success = false, Error = "Employee not found." };
            if (employee.IsActive != true) return new PayrollCalculationResult { Success = false, Error = "Employee is not active." };

            // Duplicate payroll check: ignore CANCELLED payrolls so a cancelled payroll can be regenerated
            var exists = await _context.Payrolls.AnyAsync(p => p.EmployeeId == employeeId && p.PayrollMonth == month && p.PayrollYear == year && p.PayrollStatus != "Cancelled");
            if (exists) return new PayrollCalculationResult { Success = false, Error = "Payroll already exists for this employee and period." };

            // Fetch attendances for the period
            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == employeeId && a.AttendanceDate.HasValue && a.AttendanceDate.Value.Month == month && a.AttendanceDate.Value.Year == year)
                .ToListAsync();

            // Attendance completeness: require attendance for a configurable percentage of working days (weekdays)
            // Business rule: weekdays (Mon-Fri) count as working days. A completeness threshold (default 90%) is applied.
            var threshold = 0.9m;
            var cfg = _config["Payroll:CompletenessThreshold"];
            if (!string.IsNullOrWhiteSpace(cfg) && decimal.TryParse(cfg, out var cfgVal)) threshold = Math.Clamp(cfgVal, 0.0m, 1.0m);

            var daysInMonth = DateTime.DaysInMonth(year, month);
            var workingDates = new System.Collections.Generic.List<DateOnly>();
            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateOnly(year, month, d);
                var dow = date.ToDateTime(TimeOnly.MinValue).DayOfWeek;
                if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday) continue; // skip weekends
                workingDates.Add(date);
            }

            var requiredWorkingDays = workingDates.Count;

            // Distinct attendance dates for the period (only consider dates that are working dates)
            var attendanceDates = attendances
                .Where(a => a.AttendanceDate.HasValue)
                .Select(a => a.AttendanceDate!.Value)
                .Where(d => workingDates.Contains(d))
                .Distinct()
                .ToList();

            var presentDates = attendances
                .Where(a => a.AttendanceDate.HasValue && !string.IsNullOrWhiteSpace(a.Status) && new[] { "Present", "Sick", "Leave" }.Contains(a.Status))
                .Select(a => a.AttendanceDate!.Value)
                .Where(d => workingDates.Contains(d))
                .Distinct()
                .ToList();

            var presentCount = presentDates.Count;
            var minRequired = (int)Math.Ceiling(requiredWorkingDays * (double)threshold);
            if (presentCount < minRequired)
            {
                return new PayrollCalculationResult { Success = false, Error = "Attendance records are incomplete for this payroll period." };
            }

            var totalWorkingDays = requiredWorkingDays;
            if (totalWorkingDays == 0)
            {
                return new PayrollCalculationResult { Success = false, Error = "No attendance records found for payroll period." };
            }

            var totalPresentDays = presentCount;
            var totalAbsentDays = Math.Max(0, requiredWorkingDays - totalPresentDays);

            var overtimeHours = attendances.Sum(a => a.OvertimeHours ?? 0);

            var hourlyRate = employee.HourlyRate ?? 0m;
            var overtimePay = hourlyRate * overtimeHours;

            // Job bonus: Completed Jobs * 100 (count both as mechanic or advisor)
            var completedJobsCount = await _context.JobOrders
                .Where(j => j.Status == "Completed" && j.CompletionDate.HasValue && j.CompletionDate.Value.Month == month && j.CompletionDate.Value.Year == year
                    && (j.MechanicId == employeeId || j.AdvisorId == employeeId))
                .CountAsync();
            var jobBonus = completedJobsCount * 100m;

            // Salary adjustments for this payroll will be linked after payroll is created; initial aggregate is zero
            var salaryAdjustmentsTotal = 0m;

            var basicSalary = employee.BasicSalary ?? 0m;

            // Daily rate per requirements: BasicSalary / TotalWorkingDays (expected working days)
            var dailyRate = requiredWorkingDays > 0 ? decimal.Divide(basicSalary, requiredWorkingDays) : 0m;
            var attendanceDeduction = dailyRate * totalAbsentDays;

            var grossSalary = basicSalary + jobBonus + salaryAdjustmentsTotal + overtimePay;
            var netSalary = grossSalary - attendanceDeduction;

            if (netSalary < 0)
            {
                return new PayrollCalculationResult { Success = false, Error = "Calculated net salary is negative. Please review adjustments." };
            }

            // Generate payroll number
            var payrollNumber = await GeneratePayrollNumberAsync(year, month);

            var payroll = new Payroll
            {
                EmployeeId = employeeId,
                PayrollMonth = month,
                PayrollYear = year,
                TotalWorkingDays = totalWorkingDays,
                TotalPresentDays = totalPresentDays,
                OvertimeHours = overtimeHours,
                BonusAmount = jobBonus,
                DeductionAmount = attendanceDeduction,
                GrossSalary = grossSalary,
                NetSalary = netSalary,
                PaymentDate = null,
                PayrollStatus = "Generated",
                // store generated number if model supports it
                PayrollNumber = payrollNumber
            };

            return new PayrollCalculationResult { Success = true, Payroll = payroll };
        }

        public async Task<string> GeneratePayrollNumberAsync(int year, int month)
        {
            var prefix = $"PAY{year}{month:D2}";
            // Look for existing payroll numbers with this prefix and pick next sequence
            var existing = await _context.Payrolls
                .Where(p => p.PayrollNumber != null && p.PayrollNumber.StartsWith(prefix))
                .OrderByDescending(p => p.PayrollNumber)
                .Select(p => p.PayrollNumber)
                .FirstOrDefaultAsync();

            int seq = 1;
            if (!string.IsNullOrWhiteSpace(existing) && existing.Length >= prefix.Length + 3)
            {
                var tail = existing.Substring(prefix.Length);
                if (int.TryParse(tail, out var last)) seq = last + 1;
            }

            return prefix + seq.ToString("D3");
        }

        // Recalculate payroll values. If manualPresentDays is provided, it will be used instead of computing present days from attendances.
        public async Task<PayrollCalculationResult> RecalculatePayrollAsync(Payroll existingPayroll, int? manualPresentDays = null)
        {
            if (existingPayroll == null) return new PayrollCalculationResult { Success = false, Error = "Payroll not provided." };

            var employee = await _context.Employees.FindAsync(existingPayroll.EmployeeId);
            if (employee == null) return new PayrollCalculationResult { Success = false, Error = "Employee not found." };

            var month = existingPayroll.PayrollMonth ?? 0;
            var year = existingPayroll.PayrollYear ?? 0;
            if (month < 1 || month > 12) return new PayrollCalculationResult { Success = false, Error = "Payroll month is invalid." };
            if (year < 2000 || year > 2100) return new PayrollCalculationResult { Success = false, Error = "Payroll year is invalid." };

            // Fetch attendances for computing overtime and for optional validation
            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == existingPayroll.EmployeeId && a.AttendanceDate.HasValue && a.AttendanceDate.Value.Month == month && a.AttendanceDate.Value.Year == year)
                .ToListAsync();

            // Calculate expected working weekdays for the month (Mon-Fri)
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var workingDates = new System.Collections.Generic.List<DateOnly>();
            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateOnly(year, month, d);
                var dow = date.ToDateTime(TimeOnly.MinValue).DayOfWeek;
                if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday) continue;
                workingDates.Add(date);
            }
            var requiredWorkingDays = workingDates.Count;
            if (requiredWorkingDays == 0) return new PayrollCalculationResult { Success = false, Error = "No working days for this payroll period." };

            // Determine present count: manual override or compute from attendances
            int presentCount;
            if (manualPresentDays.HasValue)
            {
                presentCount = Math.Clamp(manualPresentDays.Value, 0, requiredWorkingDays);
            }
            else
            {
                var presentDates = attendances
                    .Where(a => a.AttendanceDate.HasValue && !string.IsNullOrWhiteSpace(a.Status) && new[] { "Present", "Sick", "Leave" }.Contains(a.Status))
                    .Select(a => a.AttendanceDate!.Value)
                    .Where(d => workingDates.Contains(d))
                    .Distinct()
                    .ToList();
                presentCount = presentDates.Count;
            }

            var totalAbsentDays = Math.Max(0, requiredWorkingDays - presentCount);
            var overtimeHours = attendances.Sum(a => a.OvertimeHours ?? 0);

            var hourlyRate = employee.HourlyRate ?? 0m;
            var overtimePay = hourlyRate * overtimeHours;

            // Job bonus: Completed Jobs * 100
            var completedJobsCount = await _context.JobOrders
                .Where(j => j.Status == "Completed" && j.CompletionDate.HasValue && j.CompletionDate.Value.Month == month && j.CompletionDate.Value.Year == year
                    && (j.MechanicId == employee.EmployeeId || j.AdvisorId == employee.EmployeeId))
                .CountAsync();
            var jobBonus = completedJobsCount * 100m;

            // Aggregate salary adjustments linked to this payroll
            var adjustments = await _context.SalaryAdjustments
                .Where(sa => sa.PayrollId == existingPayroll.PayrollId && (sa.IsActive == null || sa.IsActive == true) && (sa.AdjustmentStatus == null || sa.AdjustmentStatus != "Cancelled"))
                .ToListAsync();

            // Separate adjustments by type and apply ERP rules:
            // Bonus and Allowance increase salary, Deduction and Penalty decrease salary
            var bonusTotal = adjustments.Where(a => string.Equals(a.AdjustmentType, "Bonus", StringComparison.OrdinalIgnoreCase)).Sum(a => a.Amount ?? 0m);
            var allowanceTotal = adjustments.Where(a => string.Equals(a.AdjustmentType, "Allowance", StringComparison.OrdinalIgnoreCase)).Sum(a => a.Amount ?? 0m);
            var deductionTotal = adjustments.Where(a => string.Equals(a.AdjustmentType, "Deduction", StringComparison.OrdinalIgnoreCase)).Sum(a => a.Amount ?? 0m);
            var penaltyTotal = adjustments.Where(a => string.Equals(a.AdjustmentType, "Penalty", StringComparison.OrdinalIgnoreCase)).Sum(a => a.Amount ?? 0m);

            var salaryAdjustmentsTotal = bonusTotal + allowanceTotal - deductionTotal - penaltyTotal;

            var basicSalary = employee.BasicSalary ?? 0m;
            var dailyRate = requiredWorkingDays > 0 ? decimal.Divide(basicSalary, requiredWorkingDays) : 0m;
            var attendanceDeduction = dailyRate * totalAbsentDays;

            // Gross salary: base + job bonus + positive adjustments + overtime
            var grossSalary = basicSalary + jobBonus + bonusTotal + allowanceTotal + overtimePay;
            var netSalary = grossSalary - attendanceDeduction - deductionTotal - penaltyTotal;

            if (netSalary < 0) return new PayrollCalculationResult { Success = false, Error = "Calculated net salary is negative. Please review adjustments." };

            existingPayroll.TotalWorkingDays = requiredWorkingDays;
            existingPayroll.TotalPresentDays = presentCount;
            existingPayroll.OvertimeHours = overtimeHours;
            existingPayroll.BonusAmount = jobBonus;
            existingPayroll.DeductionAmount = attendanceDeduction;
            existingPayroll.GrossSalary = grossSalary;
            existingPayroll.NetSalary = netSalary;

            return new PayrollCalculationResult { Success = true, Payroll = existingPayroll, ComputedPresentDays = presentCount };
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace AutoRepairERD.ViewModels
{
    public class EmployeeRegistrationViewModel
    {
        // User

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string? Email { get; set; }

        public int RoleId { get; set; }

        // Employee

        public string? EmployeeCode { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        public string? LastName { get; set; }

        public string? Phone { get; set; }
        //public string? Cnic { get; set; }
        [Required]
        [RegularExpression(@"^\d{5}-\d{7}-\d{1}$",
    ErrorMessage = "CNIC format should be 12345-1234567-1")]
        public string Cnic { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? Designation { get; set; }

        public decimal? BasicSalary { get; set; }

        public decimal? HourlyRate { get; set; }

        public DateOnly? HireDate { get; set; }
    }
}
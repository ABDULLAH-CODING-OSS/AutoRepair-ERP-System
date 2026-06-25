namespace AutoRepairERD.Helpers
{
    /// <summary>
    /// Maps the various free-text status/type strings stored across the schema (JobOrder.Status,
    /// Invoice.PaymentStatus, PurchaseOrder.Status, Attendance.Status, StockTransaction.TransactionType,
    /// LowStockAlert.Status, Employee/User.IsActive, etc.) to the matching badge-* CSS class defined in
    /// site.css, so every reskinned view renders status pills consistently without duplicating logic.
    /// </summary>
    public static class BadgeHelper
    {
        public static string Css(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return "badge-neutral";

            return status.Trim() switch
            {
                "Pending" => "badge-pending",
                "In Progress" => "badge-progress",
                "Completed" => "badge-completed",
                "Cancelled" => "badge-cancelled",
                "Invoiced" => "badge-invoiced",
                "Paid" => "badge-paid",
                "Unpaid" => "badge-unpaid",
                "Partially Paid" => "badge-partial",
                "Active" => "badge-active",
                "Inactive" => "badge-inactive",
                "Ordered" => "badge-ordered",
                "Received" => "badge-received",
                "Present" => "badge-present",
                "Absent" => "badge-absent",
                "Late" => "badge-late",
                "On Leave" => "badge-onleave",
                "Sick" => "badge-sick",
                "Resolved" => "badge-resolved",
                "Stock In" => "badge-stockin",
                "IN" => "badge-stockin",
                "Stock Out" => "badge-stockout",
                "OUT" => "badge-stockout",
                "Generated" => "badge-generated",
                "Draft" => "badge-pending",
                _ => "badge-neutral",
            };
        }

        public static string Css(bool? isActive) => isActive == true ? "badge-active" : "badge-inactive";

        public static string Text(bool? isActive) => isActive == true ? "Active" : "Inactive";
    }
}

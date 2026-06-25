namespace AutoRepairERD.Helpers;

/// <summary>
/// Helper class for salary adjustment type options
/// </summary>
public static class AdjustmentTypeHelper
{
    public const string Bonus = "Bonus";
    public const string Allowance = "Allowance";
    public const string Deduction = "Deduction";
    public const string Penalty = "Penalty";

    /// <summary>
    /// Gets all available adjustment types as a list
    /// </summary>
    public static List<string> GetAdjustmentTypes()
    {
        return new List<string>
        {
            Bonus,
            Allowance,
            Deduction,
            Penalty
        };
    }

    /// <summary>
    /// Gets CSS class for badge styling based on adjustment type
    /// </summary>
    public static string GetCssClass(string? adjustmentType)
    {
        return adjustmentType switch
        {
            Bonus or Allowance => "badge-active",
            Deduction or Penalty => "badge-warning",
            _ => "badge-neutral"
        };
    }
}

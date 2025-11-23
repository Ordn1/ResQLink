namespace ResQLink.Helpers;

public static class StockExtensions
{
    public static string ComputeStatus(int quantity, int maxCapacity)
    {
        if (quantity <= 0) return "Empty";
        if (maxCapacity <= 0) return "Unknown";
        var pct = (quantity * 100.0m) / maxCapacity;
        if (pct <= 25m) return "Low";
        if (pct < 75m) return "Medium";
        return "High";
    }

    public static decimal ComputePercent(int quantity, int maxCapacity)
    {
        if (maxCapacity <= 0) return 0;
        return Math.Round((quantity * 100.0m) / maxCapacity, 2);
    }

    public static string GetStatusClass(string status) => status switch
    {
        "Empty" => "st-empty",
        "Low" => "st-low",
        "Medium" => "st-medium",
        "High" => "st-ok",
        _ => "st-unknown"
    };
}

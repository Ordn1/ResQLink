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
}
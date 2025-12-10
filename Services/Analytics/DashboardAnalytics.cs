namespace ResQLink.Services.Analytics;

/// <summary>
/// Analytics data for disaster response metrics
/// </summary>
public class DashboardAnalytics
{
    // Disaster Metrics
    public int ActiveDisasters { get; set; }
    public int TotalDisasters { get; set; }
    public int DisastersThisMonth { get; set; }
    public int DisastersLastMonth { get; set; }
    public double DisasterGrowthRate { get; set; }
    
    // Evacuee Metrics
    public int TotalEvacuees { get; set; }
    public int ActiveEvacuees { get; set; }
    public int EvacueesThisMonth { get; set; }
    public Dictionary<string, int> EvacueesByStatus { get; set; } = new();
    
    // Shelter Metrics
    public int TotalShelters { get; set; }
    public int ActiveShelters { get; set; }
    public int ShelterCapacity { get; set; }
    public int ShelterOccupancy { get; set; }
    public double OccupancyRate { get; set; }
    
    // Inventory Metrics
    public int TotalReliefGoods { get; set; }
    public int LowStockItems { get; set; }
    public int OutOfStockItems { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<StockAlert> StockAlerts { get; set; } = new();
    
    // Financial Metrics
    public decimal TotalBudget { get; set; }
    public decimal BudgetUtilized { get; set; }
    public decimal BudgetRemaining { get; set; }
    public double BudgetUtilizationRate { get; set; }
    public int PendingProcurements { get; set; }
    public decimal ProcurementValue { get; set; }
    
    // Distribution Metrics
    public int TotalDistributions { get; set; }
    public int DistributionsThisWeek { get; set; }
    public Dictionary<string, int> DistributionsByCategory { get; set; } = new();
    
    // Volunteer Metrics
    public int TotalVolunteers { get; set; }
    public int ActiveVolunteers { get; set; }
    public int VolunteersOnLeave { get; set; }
    
    // Trending Data
    public List<TrendDataPoint> DisasterTrend { get; set; } = new();
    public List<TrendDataPoint> EvacueeTrend { get; set; } = new();
    public List<TrendDataPoint> DistributionTrend { get; set; } = new();
    
    // Recent Activities
    public List<RecentActivity> RecentActivities { get; set; } = new();
}

public class StockAlert
{
    public int StockId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public int MaxCapacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AlertLevel { get; set; } = string.Empty; // Critical, Warning, Info
}

public class TrendDataPoint
{
    public DateTime Date { get; set; }
    public int Value { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class RecentActivity
{
    public DateTime Timestamp { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
}

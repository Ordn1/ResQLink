using ResQLink.Data.Entities;

namespace ResQLink.Models.Reports;

public class OperationsReportData
{
    // 1. Executive Summary
    public ExecutiveSummary Summary { get; set; } = new();
    
    // 2. Disaster Overview
    public List<DisasterInfo> ActiveDisasters { get; set; } = new();
    
    // 3. Evacuee Statistics
    public EvacueeStatistics EvacueeStats { get; set; } = new();
    
    // 4. Shelter Operations
    public ShelterOperations ShelterOps { get; set; } = new();
    
    // 4.5. Financial Summary
    public FinancialSummary FinancialInfo { get; set; } = new();
    
    // 5. Volunteer Deployment
    public VolunteerDeployment VolunteerInfo { get; set; } = new();
    
    // 6. Operational Issues (Editable)
    public OperationalIssues Issues { get; set; } = new();
    
    // 7. Inventory Report (NEW)
    public InventoryReport InventoryInfo { get; set; } = new();
    
    // Metadata
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;
}

public class ExecutiveSummary
{
    public int TotalEvacuees { get; set; }
    public int ActiveShelters { get; set; }
    public int TotalVolunteers { get; set; }
    public int ActiveDisasters { get; set; }
    public string CurrentDisasterOverview { get; set; } = string.Empty;
}

public class DisasterInfo
{
    public int DisasterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DisasterType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<string> AffectedBarangays { get; set; } = new();
    public string Severity { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int DaysActive => EndDate.HasValue 
        ? (EndDate.Value - StartDate).Days 
        : (DateTime.UtcNow - StartDate).Days;
}

public class EvacueeStatistics
{
    public int TotalEvacuees { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public Dictionary<string, int> ByShelter { get; set; } = new();
    public List<EvacueeMasterListEntry> MasterList { get; set; } = new();
}

public class EvacueeMasterListEntry
{
    public int EvacueeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Shelter { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}

public class ShelterOperations
{
    public List<ShelterInfo> ActiveShelters { get; set; } = new();
    public List<ShelterNeed> Needs { get; set; } = new();
}

public class ShelterInfo
{
    public int ShelterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public int AssignedVolunteers { get; set; }
    public double OccupancyPercent => Capacity > 0 ? (CurrentOccupancy * 100.0 / Capacity) : 0;
}

public class ShelterNeed
{
    public string ShelterName { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public int RequiredQuantity { get; set; }
    public string Priority { get; set; } = "Medium";
}

public class FinancialSummary
{
    public decimal TotalFundsReceived { get; set; }
    public decimal TotalExpenditures { get; set; }
    public decimal Balance => TotalFundsReceived - TotalExpenditures;
    public List<ProcurementItem> ProcurementBreakdown { get; set; } = new();
    public List<ExpenditureItem> ExpenditureBreakdown { get; set; } = new();
}

public class ProcurementItem
{
    public string BarangayName { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime RequestDate { get; set; }
    public string Supplier { get; set; } = string.Empty;
}

public class ExpenditureItem
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Reference { get; set; } = string.Empty;
}

public class VolunteerDeployment
{
    public int TotalActiveVolunteers { get; set; }
    public List<VolunteerAssignment> Assignments { get; set; } = new();
}

public class VolunteerAssignment
{
    public string VolunteerName { get; set; } = string.Empty;
    public string AssignedShelter { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class OperationalIssues
{
    public string NeededSupport { get; set; } = string.Empty;
    public string UrgentSupplies { get; set; } = string.Empty;
    public string FundingRequests { get; set; } = string.Empty;
    public string Concerns { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

// NEW: Inventory Report Classes
public class InventoryReport
{
    // 1. Executive Summary
    public InventoryExecutiveSummary ExecutiveSummary { get; set; } = new();
    
    // 2. Current Inventory Status
    public List<CurrentInventoryItem> CurrentInventory { get; set; } = new();
    
    // 3. Stock-In Summary
    public List<StockInTransaction> StockInSummary { get; set; } = new();
    
    // 4. Stock-Out Summary
    public List<StockOutTransaction> StockOutSummary { get; set; } = new();
}

public class InventoryExecutiveSummary
{
    public int TotalItemsInStock { get; set; }
    public int CriticallyLowItems { get; set; }
    public int LowStockItems { get; set; }
    public int TotalStockReceived { get; set; }
    public int TotalStockReleased { get; set; }
    public string OverallCondition { get; set; } = "Good";
    public List<string> ImmediateProcurementNeeds { get; set; } = new();
}

public class CurrentInventoryItem
{
    public int StockId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string StockLevel { get; set; } = "Normal"; // Normal, Low, Critical
    public string StorageLocation { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public bool IsPerishable { get; set; }
}

public class StockInTransaction
{
    public int TransactionId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;
}

public class StockOutTransaction
{
    public int TransactionId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int QuantityReleased { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string ReleasedBy { get; set; } = string.Empty;
    public int RemainingBalance { get; set; }
}
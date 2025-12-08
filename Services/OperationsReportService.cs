using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Models.Reports;

namespace ResQLink.Services;

public interface IOperationsReportService
{
    Task<OperationsReportData> GenerateReportDataAsync();
    Task<byte[]> GeneratePdfReportAsync(OperationsReportData data);
    Task<bool> SaveOperationalIssuesAsync(OperationalIssues issues);
}

public class OperationsReportService : IOperationsReportService
{
    private readonly AppDbContext _context;

    public OperationsReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OperationsReportData> GenerateReportDataAsync()
    {
        var report = new OperationsReportData();

        // 1. Executive Summary
        report.Summary = await GenerateExecutiveSummaryAsync();

        // 2. Disaster Overview
        report.ActiveDisasters = await GetActiveDisastersInfoAsync();

        // 3. Evacuee Statistics
        report.EvacueeStats = await GenerateEvacueeStatisticsAsync();

        // 4. Shelter Operations
        report.ShelterOps = await GenerateShelterOperationsAsync();

        // 4.5. Financial Summary
        report.FinancialInfo = await GenerateFinancialSummaryAsync();

        // 5. Volunteer Deployment
        report.VolunteerInfo = await GenerateVolunteerDeploymentAsync();

        // 6. Load saved operational issues (if any)
        report.Issues = await LoadOperationalIssuesAsync();

        // 7. Inventory Report (NEW)
        report.InventoryInfo = await GenerateInventoryReportAsync();

        return report;
    }

    private async Task<ExecutiveSummary> GenerateExecutiveSummaryAsync()
    {
        var activeDisasters = await _context.Disasters
            .Where(d => d.Status == "Active" || d.Status == "Open")
            .ToListAsync();

        var totalEvacuees = await _context.Evacuees.CountAsync();
        var activeShelters = await _context.Shelters.CountAsync(s => s.IsActive);
        var totalVolunteers = await _context.Volunteers.CountAsync(v => v.Status == "Active");

        return new ExecutiveSummary
        {
            ActiveDisasters = activeDisasters.Count,
            TotalEvacuees = totalEvacuees,
            ActiveShelters = activeShelters,
            TotalVolunteers = totalVolunteers,
            CurrentDisasterOverview = string.Join(", ", activeDisasters.Select(d => d.Title))
        };
    }

    private async Task<List<DisasterInfo>> GetActiveDisastersInfoAsync()
    {
        var disasters = await _context.Disasters
            .Where(d => d.Status == "Active" || d.Status == "Open")
            .OrderByDescending(d => d.StartDate)
            .ToListAsync();

        return disasters.Select(d => new DisasterInfo
        {
            DisasterId = d.DisasterId,
            Title = d.Title,
            DisasterType = d.DisasterType,
            Location = d.Location,
            AffectedBarangays = d.Location.Split(',').Select(l => l.Trim()).ToList(),
            Severity = d.Severity,
            StartDate = d.StartDate,
            EndDate = d.EndDate
        }).ToList();
    }

    private async Task<EvacueeStatistics> GenerateEvacueeStatisticsAsync()
    {
        var evacuees = await _context.Evacuees
            .Include(e => e.Shelter)
            .ToListAsync();

        var stats = new EvacueeStatistics
        {
            TotalEvacuees = evacuees.Count,
            ByStatus = evacuees.GroupBy(e => e.Status)
                .ToDictionary(g => g.Key, g => g.Count()),
            ByShelter = evacuees
                .Where(e => e.Shelter != null)
                .GroupBy(e => e.Shelter!.Name)
                .ToDictionary(g => g.Key, g => g.Count()),
            MasterList = evacuees
                .OrderByDescending(e => e.RegisteredAt)
                .Take(100)
                .Select(e => new EvacueeMasterListEntry
                {
                    EvacueeId = e.EvacueeId,
                    Name = $"{e.FirstName} {e.LastName}",
                    Shelter = e.Shelter?.Name ?? "Unassigned",
                    Status = e.Status,
                    RegisteredAt = e.RegisteredAt
                }).ToList()
        };

        return stats;
    }

    private async Task<ShelterOperations> GenerateShelterOperationsAsync()
    {
        var shelters = await _context.Shelters
            .Include(s => s.Evacuees)
            .Where(s => s.IsActive)
            .ToListAsync();

        var volunteers = await _context.Volunteers
            .Where(v => v.AssignedShelterId != null)
            .GroupBy(v => v.AssignedShelterId)
            .ToDictionaryAsync(g => g.Key!.Value, g => g.Count());

        var shelterInfos = shelters.Select(s => new ShelterInfo
        {
            ShelterId = s.ShelterId,
            Name = s.Name,
            Location = s.Location ?? "N/A",
            Capacity = s.Capacity,
            CurrentOccupancy = s.CurrentOccupancy,
            AssignedVolunteers = volunteers.ContainsKey(s.ShelterId) ? volunteers[s.ShelterId] : 0
        }).ToList();

        // Get shelter needs from stock levels
        var stockNeeds = await _context.Stocks
            .Include(s => s.Shelter)
            .Include(s => s.ReliefGood)
            .Where(s => s.Quantity <= 10 && s.ShelterId != null)
            .Select(s => new ShelterNeed
            {
                ShelterName = s.Shelter!.Name,
                ItemName = s.ReliefGood.Name,
                CurrentQuantity = s.Quantity,
                RequiredQuantity = s.MaxCapacity,
                Priority = s.Quantity == 0 ? "Critical" : s.Quantity <= 5 ? "High" : "Medium"
            })
            .ToListAsync();

        return new ShelterOperations
        {
            ActiveShelters = shelterInfos,
            Needs = stockNeeds
        };
    }

    private async Task<FinancialSummary> GenerateFinancialSummaryAsync()
    {
        var financialSummary = new FinancialSummary();

        // Load Procurement Requests (Funds Received)
        var procurementRequests = await _context.ProcurementRequests
            .Include(pr => pr.Supplier)
            .Where(pr => pr.Status == "Approved" || pr.Status == "Ordered")
            .OrderByDescending(pr => pr.RequestDate)
            .ToListAsync();

        financialSummary.TotalFundsReceived = procurementRequests.Sum(pr => pr.TotalAmount);
        
        financialSummary.ProcurementBreakdown = procurementRequests
            .Select(pr => new ProcurementItem
            {
                BarangayName = pr.BarangayName,
                RequestId = pr.RequestId.ToString(),
                Status = pr.Status,
                Amount = pr.TotalAmount,
                RequestDate = pr.RequestDate,
                Supplier = pr.Supplier?.SupplierName ?? "N/A"
            })
            .ToList();

        // Load Budget Items (Expenditures)
        var budgetItems = await _context.BarangayBudgetItems
            .Include(bi => bi.Budget)
            .Where(bi => bi.Budget.Status == "Approved")
            .OrderByDescending(bi => bi.CreatedAt)
            .ToListAsync();

        financialSummary.TotalExpenditures = budgetItems.Sum(bi => bi.Amount);
        
        financialSummary.ExpenditureBreakdown = budgetItems
            .Select(bi => new ExpenditureItem
            {
                Category = bi.Category,
                Description = bi.Description,
                Amount = bi.Amount,
                Date = bi.CreatedAt,
                Reference = $"Budget #{bi.BudgetId} - {bi.Budget.BarangayName}"
            })
            .ToList();

        return financialSummary;
    }

    private async Task<VolunteerDeployment> GenerateVolunteerDeploymentAsync()
    {
        var volunteers = await _context.Volunteers
            .Include(v => v.AssignedShelter)
            .Where(v => v.Status == "Active")
            .ToListAsync();

        return new VolunteerDeployment
        {
            TotalActiveVolunteers = volunteers.Count,
            Assignments = volunteers.Select(v => new VolunteerAssignment
            {
                VolunteerName = $"{v.FirstName} {v.LastName}",
                AssignedShelter = v.AssignedShelter?.Name ?? "Unassigned",
                Skills = v.Skills ?? "General",
                Status = v.Status
            }).ToList()
        };
    }

    // NEW: Generate Inventory Report
    private async Task<InventoryReport> GenerateInventoryReportAsync()
    {
        var inventoryReport = new InventoryReport();

        // Load all stocks with related data including categories
        var stocks = await _context.Stocks
            .Include(s => s.ReliefGood)
                .ThenInclude(rg => rg.Categories)
                .ThenInclude(rgc => rgc.Category)
            .Include(s => s.Shelter)
            .ToListAsync();

        // Current Inventory Status
        inventoryReport.CurrentInventory = stocks.Select(s => new CurrentInventoryItem
        {
            StockId = s.StockId,
            ItemName = s.ReliefGood.Name,
            Category = s.ReliefGood.Categories.FirstOrDefault()?.Category?.CategoryName ?? "Uncategorized",
            AvailableQuantity = s.Quantity,
            Unit = s.ReliefGood.Unit,
            StockLevel = DetermineStockLevel(s.Quantity, s.MaxCapacity),
            StorageLocation = s.Shelter?.Name ?? "Main Warehouse",
            ExpirationDate = s.ReliefGood.ExpirationDate,
            IsPerishable = s.ReliefGood.RequiresExpiration || s.ReliefGood.ExpirationDate.HasValue
        }).OrderBy(i => i.StockLevel).ThenBy(i => i.ItemName).ToList();

        // Stock-In Transactions: Use Stocks table with CreatedAt timestamp as a proxy
        var recentStocks = await _context.Stocks
            .Include(s => s.ReliefGood)
            .Include(s => s.Shelter)
            .Where(s => s.LastUpdated >= DateTime.UtcNow.AddDays(-30))
            .OrderByDescending(s => s.LastUpdated)
            .Take(50)
            .ToListAsync();

        inventoryReport.StockInSummary = recentStocks.Select(s => new StockInTransaction
        {
            TransactionId = s.StockId,
            ItemName = s.ReliefGood.Name,
            Quantity = s.Quantity,
            Unit = s.ReliefGood.Unit,
            Supplier = "Central Warehouse", // Default since we don't have supplier tracking on stocks
            BatchNumber = $"STOCK-{s.StockId}",
            ExpirationDate = s.ReliefGood.ExpirationDate,
            ReceivedDate = s.LastUpdated,
            ReceivedBy = "System"
        }).ToList();

        // Stock-Out Transactions: Use Resource Allocations and Distributions
        var resourceDistributions = await _context.ResourceDistributions
            .Include(rd => rd.Allocation)
                .ThenInclude(a => a.Stock)
                .ThenInclude(s => s.ReliefGood)
            .Include(rd => rd.Allocation)
                .ThenInclude(a => a.Shelter)
            .Include(rd => rd.DistributedBy)
            .OrderByDescending(rd => rd.DistributedAt)
            .Take(50)
            .ToListAsync();

        inventoryReport.StockOutSummary = resourceDistributions.Select(rd => new StockOutTransaction
        {
            TransactionId = rd.DistributionId,
            ItemName = rd.Allocation.Stock.ReliefGood.Name,
            QuantityReleased = rd.DistributedQuantity,
            Unit = rd.Allocation.Stock.ReliefGood.Unit,
            Destination = rd.Allocation.Shelter?.Name ?? "Direct Distribution",
            Purpose = "Distribution",
            ReleaseDate = rd.DistributedAt,
            ReleasedBy = rd.DistributedBy?.Username ?? "System",
            RemainingBalance = rd.Allocation.Stock.Quantity
        }).ToList();

        // Executive Summary
        var criticalItems = inventoryReport.CurrentInventory.Count(i => i.StockLevel == "Critical");
        var lowStockItems = inventoryReport.CurrentInventory.Count(i => i.StockLevel == "Low");

        inventoryReport.ExecutiveSummary = new InventoryExecutiveSummary
        {
            TotalItemsInStock = inventoryReport.CurrentInventory.Count,
            CriticallyLowItems = criticalItems,
            LowStockItems = lowStockItems,
            TotalStockReceived = recentStocks.Sum(s => s.Quantity),
            TotalStockReleased = resourceDistributions.Sum(rd => rd.DistributedQuantity),
            OverallCondition = criticalItems > 5 ? "Critical" : lowStockItems > 10 ? "Needs Attention" : "Good",
            ImmediateProcurementNeeds = inventoryReport.CurrentInventory
                .Where(i => i.StockLevel == "Critical")
                .Select(i => i.ItemName)
                .Take(10)
                .ToList()
        };

        return inventoryReport;
    }

    private string DetermineStockLevel(int quantity, int maxCapacity)
    {
        if (quantity == 0) return "Critical";
        
        var percentage = maxCapacity > 0 ? (quantity * 100.0 / maxCapacity) : 0;
        
        if (percentage < 10) return "Critical";
        if (percentage < 25) return "Low";
        return "Normal";
    }

    private async Task<OperationalIssues> LoadOperationalIssuesAsync()
    {
        // In a real implementation, this would load from a dedicated table
        // For now, return empty structure
        return new OperationalIssues();
    }

    public async Task<bool> SaveOperationalIssuesAsync(OperationalIssues issues)
    {
        // In a real implementation, this would save to database
        // For now, just return true
        await Task.CompletedTask;
        return true;
    }

    public async Task<byte[]> GeneratePdfReportAsync(OperationsReportData data)
    {
        var pdfGenerator = new OperationsReportPdfGenerator();
        return await Task.FromResult(pdfGenerator.Generate(data));
    }
}
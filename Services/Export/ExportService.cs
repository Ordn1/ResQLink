using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using System.Text;
using System.Text.Json;

namespace ResQLink.Services.Export;

/// <summary>
/// Service for exporting data to various formats
/// </summary>
public class ExportService : IExportService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ExportService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ExportResult> ExportDisastersAsync(ExportOptions options)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.Disasters.AsNoTracking().AsQueryable();
        
        if (options.StartDate.HasValue)
            query = query.Where(d => d.StartDate >= options.StartDate.Value);
        
        if (options.EndDate.HasValue)
            query = query.Where(d => d.StartDate <= options.EndDate.Value);

        var disasters = await query.ToListAsync();

        return options.Format switch
        {
            ExportFormat.CSV => ExportToCsv(disasters, "Disasters", d => new
            {
                d.DisasterId,
                d.Title,
                d.DisasterType,
                d.Severity,
                d.Status,
                d.StartDate,
                d.EndDate,
                d.Location,
                d.CreatedAt
            }),
            ExportFormat.JSON => ExportToJson(disasters, "Disasters"),
            _ => throw new NotImplementedException($"Format {options.Format} not implemented")
        };
    }

    public async Task<ExportResult> ExportEvacueesAsync(ExportOptions options)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.Evacuees
            .Include(e => e.Disaster)
            .Include(e => e.Shelter)
            .AsNoTracking()
            .AsQueryable();
        
        if (options.StartDate.HasValue)
            query = query.Where(e => e.RegisteredAt >= options.StartDate.Value);

        var evacuees = await query.ToListAsync();

        return options.Format switch
        {
            ExportFormat.CSV => ExportToCsv(evacuees, "Evacuees", e => new
            {
                e.EvacueeId,
                e.FirstName,
                e.LastName,
                e.Status,
                DisasterName = e.Disaster.Title,
                ShelterName = e.Shelter?.Name ?? "N/A",
                RegisteredAt = e.RegisteredAt
            }),
            ExportFormat.JSON => ExportToJson(evacuees, "Evacuees"),
            _ => throw new NotImplementedException($"Format {options.Format} not implemented")
        };
    }

    public async Task<ExportResult> ExportInventoryAsync(ExportOptions options)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var stocks = await context.Stocks
            .Include(s => s.ReliefGood)
            .Include(s => s.Disaster)
            .Include(s => s.Shelter)
            .AsNoTracking()
            .ToListAsync();

        return options.Format switch
        {
            ExportFormat.CSV => ExportToCsv(stocks, "Inventory", s => new
            {
                s.StockId,
                ItemName = s.ReliefGood.Name,
                Unit = s.ReliefGood.Unit,
                s.Quantity,
                s.MaxCapacity,
                s.Status,
                s.Location,
                s.UnitCost,
                TotalValue = s.Quantity * s.UnitCost,
                DisasterName = s.Disaster != null ? s.Disaster.Title : "N/A",
                ShelterName = s.Shelter != null ? s.Shelter.Name : "N/A",
                s.LastUpdated
            }),
            ExportFormat.JSON => ExportToJson(stocks, "Inventory"),
            _ => throw new NotImplementedException($"Format {options.Format} not implemented")
        };
    }

    public async Task<ExportResult> ExportBudgetsAsync(ExportOptions options)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var budgets = await context.BarangayBudgets
            .Include(b => b.Items)
            .Include(b => b.CreatedBy)
            .AsNoTracking()
            .ToListAsync();

        return options.Format switch
        {
            ExportFormat.CSV => ExportToCsv(budgets, "Budgets", b => new
            {
                b.BudgetId,
                b.BarangayName,
                b.Year,
                b.TotalAmount,
                ItemsTotal = b.Items.Sum(i => i.Amount),
                RemainingAmount = b.TotalAmount - b.Items.Sum(i => i.Amount),
                Utilized = b.Items.Sum(i => i.Amount),
                UtilizationRate = b.TotalAmount > 0 ? (b.Items.Sum(i => i.Amount) / b.TotalAmount) * 100 : 0,
                b.Status,
                CreatedBy = b.CreatedBy?.Username ?? "Unknown",
                b.CreatedAt
            }),
            ExportFormat.JSON => ExportToJson(budgets, "Budgets"),
            _ => throw new NotImplementedException($"Format {options.Format} not implemented")
        };
    }

    public async Task<ExportResult> ExportAuditLogsAsync(ExportOptions options)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.AuditLogs.AsNoTracking().AsQueryable();
        
        if (options.StartDate.HasValue)
            query = query.Where(a => a.Timestamp >= options.StartDate.Value);
        
        if (options.EndDate.HasValue)
            query = query.Where(a => a.Timestamp <= options.EndDate.Value);

        var logs = await query.OrderByDescending(a => a.Timestamp).ToListAsync();

        return options.Format switch
        {
            ExportFormat.CSV => ExportToCsv(logs, "AuditLogs", a => new
            {
                a.AuditLogId,
                a.Timestamp,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.UserName,
                a.UserType,
                a.Description,
                a.Severity,
                a.IsSuccessful
            }),
            ExportFormat.JSON => ExportToJson(logs, "AuditLogs"),
            _ => throw new NotImplementedException($"Format {options.Format} not implemented")
        };
    }

    public async Task<ExportResult> ExportVolunteersAsync(ExportOptions options)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var volunteers = await context.Volunteers
            .Include(v => v.AssignedShelter)
            .Include(v => v.AssignedDisaster)
            .AsNoTracking()
            .ToListAsync();

        return options.Format switch
        {
            ExportFormat.CSV => ExportToCsv(volunteers, "Volunteers", v => new
            {
                v.VolunteerId,
                v.FirstName,
                v.LastName,
                v.Email,
                v.ContactNumber,
                v.Status,
                v.Skills,
                v.Availability,
                AssignedShelter = v.AssignedShelter?.Name ?? "N/A",
                AssignedDisaster = v.AssignedDisaster?.Title ?? "N/A",
                v.RegisteredAt
            }),
            ExportFormat.JSON => ExportToJson(volunteers, "Volunteers"),
            _ => throw new NotImplementedException($"Format {options.Format} not implemented")
        };
    }

    public Task<ExportResult> ExportCustomReportAsync<T>(IEnumerable<T> data, ExportOptions options, string reportName) where T : class
    {
        return Task.FromResult(options.Format switch
        {
            ExportFormat.CSV => ExportToCsv(data, reportName, item => item),
            ExportFormat.JSON => ExportToJson(data, reportName),
            _ => throw new NotImplementedException($"Format {options.Format} not implemented")
        });
    }

    private ExportResult ExportToCsv<T>(IEnumerable<T> data, string fileName, Func<T, object> selector)
    {
        var records = data.Select(selector).ToList();
        
        if (!records.Any())
        {
            return new ExportResult
            {
                Data = Encoding.UTF8.GetBytes("No data available"),
                FileName = $"{fileName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv",
                ContentType = "text/csv",
                RecordCount = 0
            };
        }

        var sb = new StringBuilder();
        
        // Headers
        var first = records.First();
        var properties = first.GetType().GetProperties();
        sb.AppendLine(string.Join(",", properties.Select(p => p.Name)));
        
        // Data
        foreach (var record in records)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(record);
                var stringValue = value?.ToString() ?? "";
                // Escape commas and quotes
                if (stringValue.Contains(',') || stringValue.Contains('"'))
                    return $"\"{stringValue.Replace("\"", "\"\"")}\"";
                return stringValue;
            });
            sb.AppendLine(string.Join(",", values));
        }

        return new ExportResult
        {
            Data = Encoding.UTF8.GetBytes(sb.ToString()),
            FileName = $"{fileName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv",
            ContentType = "text/csv",
            RecordCount = records.Count
        };
    }

    private ExportResult ExportToJson<T>(IEnumerable<T> data, string fileName)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return new ExportResult
        {
            Data = Encoding.UTF8.GetBytes(json),
            FileName = $"{fileName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json",
            ContentType = "application/json",
            RecordCount = data.Count()
        };
    }
}

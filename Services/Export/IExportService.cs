namespace ResQLink.Services.Export;

/// <summary>
/// Export format types
/// </summary>
public enum ExportFormat
{
    CSV,
    Excel,
    JSON,
    PDF
}

/// <summary>
/// Export options configuration
/// </summary>
public class ExportOptions
{
    public ExportFormat Format { get; set; } = ExportFormat.CSV;
    public bool IncludeHeaders { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string>? SelectedColumns { get; set; }
    public string? FilterCriteria { get; set; }
}

/// <summary>
/// Export result containing file data and metadata
/// </summary>
public class ExportResult
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Interface for export service
/// </summary>
public interface IExportService
{
    Task<ExportResult> ExportDisastersAsync(ExportOptions options);
    Task<ExportResult> ExportEvacueesAsync(ExportOptions options);
    Task<ExportResult> ExportInventoryAsync(ExportOptions options);
    Task<ExportResult> ExportBudgetsAsync(ExportOptions options);
    Task<ExportResult> ExportAuditLogsAsync(ExportOptions options);
    Task<ExportResult> ExportVolunteersAsync(ExportOptions options);
    Task<ExportResult> ExportCustomReportAsync<T>(IEnumerable<T> data, ExportOptions options, string reportName) where T : class;
}

# Archiving System Documentation

## Overview
The ResQLink disaster management system now uses an **archiving system** instead of hard deletes to preserve data integrity and maintain historical records. Archived records are hidden from normal queries but remain in the database for audit trails and potential recovery.

## Implementation Date
Completed: December 9, 2025

## Key Benefits

### 1. **Data Preservation**
- No permanent data loss
- Complete audit trail maintained
- Historical records preserved for compliance

### 2. **Referential Integrity**
- No broken foreign key relationships
- Related records remain accessible
- Prevents cascading delete issues

### 3. **Recovery Capability**
- Archived records can be restored if needed
- Mistakes can be undone
- Data analysis remains complete

### 4. **Compliance & Auditing**
- Full history of all operations
- Who archived what and when
- Reason for archiving tracked

## Architecture

### IArchivable Interface
All archivable entities implement the `IArchivable` interface:

```csharp
public interface IArchivable
{
    bool IsArchived { get; set; }           // Archive flag
    DateTime? ArchivedAt { get; set; }      // When archived
    int? ArchivedBy { get; set; }           // User who archived
    string? ArchiveReason { get; set; }     // Why archived (max 500 chars)
}
```

### Entities with Archiving Support

The following entities implement `IArchivable`:

1. **ReliefGood** - Relief items/supplies
2. **Disaster** - Disaster records
3. **Category** - Item categories
4. **Supplier** - Supplier information
5. **Stock** - Inventory stock records
6. **ProcurementRequest** - Procurement requests
7. **BarangayBudget** - Budget allocations

### Database Schema

Each table has these additional columns:

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `IsArchived` | BIT | No | 0 | Archive status flag |
| `ArchivedAt` | DATETIME2 | Yes | NULL | Timestamp of archiving |
| `ArchivedBy` | INT | Yes | NULL | User ID who archived |
| `ArchiveReason` | NVARCHAR(500) | Yes | NULL | Reason for archiving |

**Indexes Created:**
- Non-clustered indexes on `IsArchived` column for all tables
- Includes `IsActive` or `Status` columns for optimized filtering

## How It Works

### 1. Global Query Filters
Entity Framework automatically filters out archived records:

```csharp
// In AppDbContext.OnModelCreating()
modelBuilder.Entity<ReliefGood>().HasQueryFilter(e => !e.IsArchived);
modelBuilder.Entity<Disaster>().HasQueryFilter(e => !e.IsArchived);
// ... etc for all archivable entities
```

**Result:** Archived records are **automatically excluded** from all queries unless explicitly requested.

### 2. Archive Operation
When a "delete" operation is performed:

```csharp
// Old code (hard delete):
_db.ReliefGoods.Remove(item);

// New code (archive):
item.IsArchived = true;
item.ArchivedAt = DateTime.UtcNow;
item.ArchivedBy = _authState.UserId;
item.ArchiveReason = "User requested deletion";
item.IsActive = false; // Also mark inactive
```

### 3. Service Layer Updates

#### InventoryService.DeleteAsync()
- Archives relief goods instead of deleting
- Preserves stock history
- Logs archival action in audit trail

#### DisasterService.DeleteAsync()
- Archives disasters instead of deleting
- Sets status to "Closed"
- Tracks related evacuees, shelters, and stocks
- No more foreign key cascade issues

#### CategoryService.DeleteAsync()
- Archives categories instead of deleting
- Tracks linked relief goods count
- No need to reassign items first

## Usage Examples

### Archiving a Record
```csharp
// Service method automatically archives
var (success, error) = await _inventoryService.DeleteAsync(itemId);
// Item is archived, not deleted
```

### Querying Active Records (Default)
```csharp
// Only non-archived records returned
var items = await _db.ReliefGoods.ToListAsync();
```

### Querying Including Archived Records
```csharp
// Use IgnoreQueryFilters() to include archived
var allItems = await _db.ReliefGoods
    .IgnoreQueryFilters()
    .ToListAsync();
```

### Querying Only Archived Records
```csharp
// Get archived records only
var archivedItems = await _db.ReliefGoods
    .IgnoreQueryFilters()
    .Where(x => x.IsArchived)
    .ToListAsync();
```

### Restoring Archived Records
```csharp
// To restore (unarchive)
var item = await _db.ReliefGoods
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(x => x.RgId == id);
    
if (item != null && item.IsArchived)
{
    item.IsArchived = false;
    item.ArchivedAt = null;
    item.ArchivedBy = null;
    item.ArchiveReason = null;
    item.IsActive = true;
    await _db.SaveChangesAsync();
}
```

## API Endpoints Affected

### DELETE Operations Now Archive:
- `DELETE /api/reliefgoods/{id}` - Archives relief good
- `DELETE /api/disasters/{id}` - Archives disaster
- `DELETE /api/categories/{id}` - Archives category
- `DELETE /api/suppliers/{id}` - Archives supplier
- `DELETE /api/stocks/{id}` - Archives stock
- `DELETE /api/procurement/{id}` - Archives procurement request
- `DELETE /api/budgets/{id}` - Archives budget

**Note:** API endpoints keep their names for backward compatibility, but perform archiving internally.

## Audit Trail Integration

All archive operations are logged:

```csharp
await _auditService.LogAsync(
    action: "ARCHIVE",
    entityType: "ReliefGood",
    entityId: id,
    userId: _authState.UserId,
    description: $"Relief good '{item.Name}' archived",
    severity: "Info",
    isSuccessful: true
);
```

## Performance Considerations

### Indexes
- All archive columns have indexes for fast filtering
- Composite indexes include `IsActive`/`Status` for common queries
- Minimal performance impact on normal operations

### Query Performance
- Archived records increase table size
- Regular queries only scan non-archived records (filtered by index)
- Periodic cleanup may be needed for very old archives

## Administration

### Viewing Archived Records
Create admin page to view archived records:

```razor
@page "/admin/archived"
@inject AppDbContext DB

<h2>Archived Records</h2>

@foreach (var item in archivedItems)
{
    <div>
        <strong>@item.Name</strong>
        <p>Archived: @item.ArchivedAt</p>
        <p>Reason: @item.ArchiveReason</p>
        <button @onclick="() => RestoreAsync(item.RgId)">Restore</button>
    </div>
}

@code {
    List<ReliefGood> archivedItems = new();
    
    protected override async Task OnInitializedAsync()
    {
        archivedItems = await DB.ReliefGoods
            .IgnoreQueryFilters()
            .Where(x => x.IsArchived)
            .ToListAsync();
    }
}
```

### Bulk Archive Cleanup
For very old archived records (optional):

```sql
-- Permanently delete archived records older than 5 years
DELETE FROM Relief_Goods 
WHERE IsArchived = 1 
AND ArchivedAt < DATEADD(YEAR, -5, GETUTCDATE());

-- Do this for each table as needed
```

## Migration Applied

### SQL Script: `Add_Archive_Fields.sql`
- Added archive columns to 7 tables
- Created 7 indexes for performance
- All changes are backward-compatible

**Tables Updated:**
1. Relief_Goods
2. Disasters
3. Categories
4. Suppliers
5. Stocks
6. ProcurementRequests
7. BarangayBudgets

## Testing Recommendations

### Test Cases

1. **Archive Operation**
   - Delete a record via UI
   - Verify it's archived (not deleted)
   - Verify it disappears from normal lists
   - Check audit log entry

2. **Query Filtering**
   - List all records (should exclude archived)
   - Search for specific record (shouldn't find archived)
   - Admin view (should show archived with flag)

3. **Foreign Key Relationships**
   - Archive parent record
   - Verify child records still accessible
   - Verify no cascade delete errors

4. **Restoration**
   - Archive a record
   - Restore it via admin function
   - Verify it appears in normal lists again

5. **Performance**
   - Archive 1000+ records
   - Measure query performance
   - Verify indexes are being used

## Future Enhancements

### Potential Features:
1. **Scheduled Auto-Archive**
   - Auto-archive old closed disasters
   - Auto-archive inactive suppliers after 2 years

2. **Archive Analytics Dashboard**
   - Show archive statistics
   - Archive reasons breakdown
   - Archive trends over time

3. **Bulk Restore**
   - Select multiple archived items
   - Restore with one action
   - Batch operations for efficiency

4. **Archive Export**
   - Export archived records to external storage
   - Archive to cold storage after X years
   - Compliance reporting

5. **Archive Notifications**
   - Notify admins when items archived
   - Weekly digest of archived items
   - Warning before archiving critical data

## Troubleshooting

### Issue: Can't find a record
**Solution:** Check if it's archived using `IgnoreQueryFilters()`

### Issue: Foreign key constraint errors
**Solution:** Archiving should prevent these. If occurring, check if cascade delete is still configured.

### Issue: Performance degradation
**Solution:** Check index usage. Consider archiving cleanup for very old records.

### Issue: Need to permanently delete
**Solution:** Use `IgnoreQueryFilters()`, find the archived record, and use `Remove()`.

## Code Examples

### Check if Record is Archived
```csharp
var item = await _db.ReliefGoods
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(x => x.RgId == id);

if (item?.IsArchived == true)
{
    // Handle archived record
}
```

### Get Archive Statistics
```csharp
var stats = new
{
    TotalReliefGoods = await _db.ReliefGoods.IgnoreQueryFilters().CountAsync(),
    ActiveReliefGoods = await _db.ReliefGoods.CountAsync(),
    ArchivedReliefGoods = await _db.ReliefGoods
        .IgnoreQueryFilters()
        .CountAsync(x => x.IsArchived),
    ArchivedThisMonth = await _db.ReliefGoods
        .IgnoreQueryFilters()
        .CountAsync(x => x.IsArchived && 
                         x.ArchivedAt >= DateTime.UtcNow.AddMonths(-1))
};
```

### Archive with Custom Reason
```csharp
public async Task<bool> ArchiveWithReasonAsync(int id, string reason)
{
    var item = await _db.ReliefGoods.FindAsync(id);
    if (item == null) return false;
    
    item.IsArchived = true;
    item.ArchivedAt = DateTime.UtcNow;
    item.ArchivedBy = _authState.UserId;
    item.ArchiveReason = reason; // Custom reason
    item.IsActive = false;
    
    await _db.SaveChangesAsync();
    return true;
}
```

## Related Documentation
- See `LOGIN_SECURITY_DOCUMENTATION.md` for login security features
- See `REPORTS_ENHANCEMENT_DOCUMENTATION.md` for report improvements
- See Entity Framework Core documentation for query filter details

---
**Version:** 1.0  
**Status:** Production Ready  
**Last Updated:** December 9, 2025  
**Compatibility:** .NET 9.0, Entity Framework Core 9.0, SQL Server 2017+

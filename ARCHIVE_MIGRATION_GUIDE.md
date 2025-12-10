# Archive System Migration Guide

## Overview
This guide explains the migration from individual archive fields on each table to a centralized `Archives` table for better database design and simplified archive management.

## Architecture Changes

### Before (Old System)
Each archivable entity had these fields:
- `IsArchived` (bit)
- `ArchivedAt` (datetime2)
- `ArchivedBy` (int)
- `ArchiveReason` (nvarchar)

**Problems:**
- Schema bloat - 4 extra columns per table
- Repeated code in every service
- Query filters needed on each entity
- Difficult to manage archives centrally
- Database migrations complex

### After (New System)
One centralized `Archives` table stores all archived records:

```sql
Archives
├── ArchiveId (PK)
├── EntityType (string) - "ReliefGood", "Category", etc.
├── EntityId (int) - Original record's primary key
├── ArchivedData (nvarchar(max)) - Full JSON of archived record
├── ArchivedAt (datetime2)
├── ArchivedBy (FK to Users)
├── ArchiveReason (string)
└── EntityName (string) - Display name
```

**Benefits:**
- Clean schema - no archive columns on business tables
- Centralized management via `ArchiveService`
- Easy cross-entity archive queries
- Simpler migrations
- Audit trail preserved in JSON

## New Components

### 1. Archive Entity (`Data/Entities/Archive.cs`)
Represents a single archived record with full context.

### 2. ArchiveService (`Services/ArchiveService.cs`)
Centralized service with methods:

```csharp
// Archive a record (removes from original table, stores in Archives)
Task<(bool success, string? error)> ArchiveAsync<T>(
    int entityId, 
    string? reason = null, 
    string? entityName = null)

// Restore archived record back to original table
Task<(bool success, string? error)> RestoreAsync<T>(int archiveId)

// Query archived records
Task<List<Archive>> GetArchivedRecordsAsync<T>()
Task<List<Archive>> GetAllArchivedRecordsAsync()
Task<Archive?> GetArchiveByIdAsync(int archiveId)
Task<List<Archive>> SearchArchivedRecordsAsync(string searchTerm)

// Permanently delete (cannot be undone)
Task<(bool success, string? error)> DeletePermanentlyAsync(int archiveId)

// Statistics
Task<Dictionary<string, int>> GetArchiveCountsByTypeAsync()
```

### 3. Database Migration (`Data/Migrations/Create_Archives_Table.sql`)
SQL script that:
1. Creates `Archives` table with indexes
2. Migrates existing archived data from entity tables
3. Optionally drops old archive columns (commented out)

## Migration Steps

### Step 1: Update Database Schema
Run the migration script:

```sql
-- On local database
sqlcmd -S localhost\SQLEXPRESS -d resqlink -i "Create_Archives_Table.sql"

-- On remote database  
-- Run via SQL Server Management Studio or database portal
```

This will:
- ✓ Create `Archives` table
- ✓ Migrate existing archived records
- ⚠ Keep old columns for now (safety)

### Step 2: Update Application Code

#### Service Constructor Updates
Services now need `ArchiveService` injected:

```csharp
// OLD
public CategoryService(AppDbContext db, AuditService audit, AuthState? auth = null)

// NEW
public CategoryService(AppDbContext db, AuditService audit, 
    ArchiveService archive, AuthState? auth = null)
{
    _archiveService = archive;
}
```

#### Delete Method Updates
Replace archive field manipulation with service call:

```csharp
// OLD - Setting archive fields
public async Task<(bool, string?)> DeleteAsync(int id)
{
    var entity = await _db.Categories.FindAsync(id);
    entity.IsArchived = true;
    entity.ArchivedAt = DateTime.UtcNow;
    entity.ArchivedBy = _authState?.UserId;
    entity.ArchiveReason = "User archived";
    await _db.SaveChangesAsync();
}

// NEW - Using ArchiveService
public async Task<(bool, string?)> DeleteAsync(int id)
{
    var category = await _db.Categories.FindAsync(id);
    var result = await _archiveService.ArchiveAsync<Category>(
        id, 
        "User archived", 
        category.CategoryName);
    return result;
}
```

#### Service Registration
Update `MauiProgram.cs`:

```csharp
// Register ArchiveService
builder.Services.AddScoped<ArchiveService>();

// Update service registrations to inject ArchiveService
builder.Services.AddScoped<CategoryService>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var audit = sp.GetRequiredService<AuditService>();
    var archive = sp.GetRequiredService<ArchiveService>();
    var auth = sp.GetService<AuthState>();
    return new CategoryService(db, audit, archive, auth);
});
```

### Step 3: Update Entity Classes (Optional)
Can remove `IArchivable` interface from entities:

```csharp
// OLD
public class Category : IArchivable
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
    
    // IArchivable properties (can be removed)
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public int? ArchivedBy { get; set; }
    public string? ArchiveReason { get; set; }
}

// NEW
public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
    // Archive fields removed - now in Archives table
}
```

### Step 4: Update AppDbContext
Remove query filters (optional):

```csharp
// Can remove these after removing IArchivable
modelBuilder.Entity<ReliefGood>().HasQueryFilter(e => !e.IsArchived);
modelBuilder.Entity<Category>().HasQueryFilter(e => !e.IsArchived);
// etc...
```

### Step 5: Drop Old Columns (After Testing)
Once everything works, uncomment Step 3 in migration script to drop old columns:

```sql
-- Uncomment these in Create_Archives_Table.sql
ALTER TABLE [dbo].[Relief_Goods] DROP COLUMN [IsArchived];
ALTER TABLE [dbo].[Relief_Goods] DROP COLUMN [ArchivedAt];
-- etc...
```

## Services to Update

Update these services to use `ArchiveService`:
- ✓ `CategoryService.cs` - Example updated
- ⚠ `InventoryService.cs` - Needs update
- ⚠ `DisasterService.cs` - Needs update
- ⚠ `SupplierService.cs` - Needs update
- ⚠ `StockService.cs` - Needs update
- ⚠ `BudgetService.cs` - Needs update
- ⚠ `ProcurementService.cs` - Needs update

## Testing Checklist

- [ ] Archive a record - verify it moves to Archives table
- [ ] Restore a record - verify it returns to original table
- [ ] View archives page - verify all archives display
- [ ] Filter archives by type - verify filtering works
- [ ] Search archives - verify search functionality
- [ ] View archive statistics - verify counts
- [ ] Delete archive permanently - verify deletion
- [ ] Check audit logs - verify archiving is logged
- [ ] Test with multiple entity types
- [ ] Verify remote sync works with new structure

## Rollback Plan

If issues occur:
1. Keep old archive columns (don't drop them)
2. Revert service code to use old fields
3. Archives table can coexist without issues

## Benefits Summary

✓ **Cleaner Schema** - Remove 4 columns × 7 tables = 28 fewer columns
✓ **Centralized Management** - One service, one table for all archives
✓ **Better Queries** - Easy to find all archives across types
✓ **Audit Trail** - Full JSON snapshot of archived state
✓ **Simpler Code** - Less repetition in services
✓ **Future-Proof** - Easy to add new archivable entities

## Example Usage

```csharp
// Archive a category
var result = await _archiveService.ArchiveAsync<Category>(
    entityId: 123,
    reason: "No longer needed",
    entityName: "Food Items"
);

// Get all archived categories
var archives = await _archiveService.GetArchivedRecordsAsync<Category>();

// Restore an archive
var result = await _archiveService.RestoreAsync<Category>(archiveId: 456);

// Search archives
var results = await _archiveService.SearchArchivedRecordsAsync("food");

// Get statistics
var counts = await _archiveService.GetArchiveCountsByTypeAsync();
// { "Category": 5, "ReliefGood": 12, "Disaster": 3 }
```

## Notes

- Archives are stored as JSON, preserving full entity state
- Original primary keys are maintained for reference
- Foreign key relationships are preserved in JSON
- Audit logging happens automatically
- Archives table grows over time - consider periodic cleanup
- Permanently deleted archives cannot be recovered

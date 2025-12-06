using Microsoft.EntityFrameworkCore;
namespace ResQLink.Services;

public class BudgetStateService
{
    private Dictionary<int, decimal> _budgetBalances = new();
    
    public event Action? OnBudgetChanged;

    /// <summary>
    /// Get the available balance for a specific budget
    /// </summary>
    public decimal? GetBudgetBalance(int budgetId)
    {
        return _budgetBalances.TryGetValue(budgetId, out var balance) ? balance : null;
    }

    /// <summary>
    /// Update the balance for a specific budget
    /// </summary>
    public void UpdateBudgetBalance(int budgetId, decimal availableBalance)
    {
        _budgetBalances[budgetId] = availableBalance;
        NotifyBudgetChanged();
    }

    /// <summary>
    /// Refresh all budget balances from the database
    /// </summary>
    public async Task RefreshBudgetsAsync(IDbContextFactory<ResQLink.Data.AppDbContext> dbFactory)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        
        var budgets = await db.BarangayBudgets
            .Where(b => b.Status == "Approved" || b.Status == "Draft")
            .AsNoTracking()
            .ToListAsync();

        _budgetBalances.Clear();
        
        foreach (var budget in budgets)
        {
            var spent = await db.BarangayBudgetItems
                .Where(i => i.BudgetId == budget.BudgetId)
                .SumAsync(i => i.Amount);
            
            _budgetBalances[budget.BudgetId] = budget.TotalAmount - spent;
        }
        
        NotifyBudgetChanged();
    }

    /// <summary>
    /// Notify all subscribed components that budget data has changed
    /// </summary>
    public void NotifyBudgetChanged()
    {
        OnBudgetChanged?.Invoke();
    }

    /// <summary>
    /// Clear all cached balances
    /// </summary>
    public void Clear()
    {
        _budgetBalances.Clear();
    }
}
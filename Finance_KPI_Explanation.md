# Finance Page KPI Card Explanation

## Issue
You have 1 approved procurement request, but it's not showing in the "Approved Requests" card (showing 0 instead).

## Root Cause
When you create a budget from an approved procurement request, the system **automatically changes the status** from "Approved" to "Ordered" (see line 320 in Finance.razor):

```csharp
// Update the linked procurement request status to "Ordered"
if (linkedProcurementRequestId.HasValue && linkedProcurementRequestId.Value > 0)
{
    await UpdateProcurementRequestStatusAsync(linkedProcurementRequestId.Value, "Ordered");
}
```

So your procurement request now has status **"Ordered"** instead of "Approved", which is why it's not counting in the "Approved Requests" card.

## Solution
Add a fifth KPI card to show "Ordered" requests. Here's the code to add after the "Pending Requests" card (around line 69):

```razor
<div class="kpi-card">
    <div class="kpi-icon-circle" style="background: #e0e7ff; color: #3730a3;">
        <i class="bi bi-cart-check"></i>
    </div>
    <div class="kpi-content">
        <div class="kpi-label">Ordered Requests</div>
        <div class="kpi-value" style="color: #3730a3;">@Requests.Count(r => r.Status == "Ordered")</div>
        <div class="kpi-detail">?@Requests.Where(r => r.Status == "Ordered").Sum(r => r.TotalAmount).ToString("N2")</div>
    </div>
</div>
```

This will show your 1 request in the "Ordered Requests" card, making it clear that the approved request has been processed and converted to a budget.

## Workflow
1. Create procurement request ? Status: "Draft"
2. Submit for approval ? Status: "Submitted"  
3. Approve the request ? Status: "Approved" ?
4. Create budget from approved request ? Status: "Ordered" ??

Your data is correct - the request just moved to the next stage!

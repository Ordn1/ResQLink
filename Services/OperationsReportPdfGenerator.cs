using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResQLink.Models.Reports;

// Explicitly qualify IContainer to avoid ambiguity
using QuestPdfContainer = QuestPDF.Infrastructure.IContainer;
using QuestColors = QuestPDF.Helpers.Colors;

namespace ResQLink.Services;

public class OperationsReportPdfGenerator
{
    private readonly string _logoPath;

    public OperationsReportPdfGenerator()
    {
        // Get the logo path relative to wwwroot
        _logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "ResQLinkLogo.png");
    }

    public byte[] Generate(OperationsReportData data)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c));
                page.Content().Element(c => ComposeContent(c, data));
                page.Footer().Element(c => ComposeFooter(c, data));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(QuestPdfContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("OPERATIONS REPORT")
                    .FontSize(24)
                    .Bold()
                    .FontColor("#07562c");

                column.Item().Text("Unified Format")
                    .FontSize(12)
                    .FontColor("#666");
            });

            // Logo in top right corner
            row.ConstantItem(120).Height(50).Element(logoContainer =>
            {
                if (File.Exists(_logoPath))
                {
                    logoContainer.AlignRight().AlignMiddle().Image(_logoPath).FitArea();
                }
                else
                {
                    // Fallback: Display text if logo not found
                    logoContainer.AlignRight().AlignMiddle()
                        .Text("ResQLink")
                        .FontSize(16)
                        .Bold()
                        .FontColor("#07562c");
                }
            });
        });
    }

    private void ComposeContent(QuestPdfContainer container, OperationsReportData data)
    {
        container.Column(column =>
        {
            // 1. Executive Summary
            column.Item().Element(c => ComposeExecutiveSummary(c, data.Summary));
            column.Item().PaddingVertical(10);

            // 2. Disaster Situation Overview
            column.Item().Element(c => ComposeDisasterOverview(c, data.ActiveDisasters));
            column.Item().PaddingVertical(10);

            // 3. Evacuee Statistics
            column.Item().Element(c => ComposeEvacueeStatistics(c, data.EvacueeStats));
            column.Item().PaddingVertical(10);

            // 4. Shelter Operations
            column.Item().Element(c => ComposeShelterOperations(c, data.ShelterOps));
            column.Item().PaddingVertical(10);

            // 5. Financial Summary (RENUMBERED from 4.5)
            column.Item().PageBreak();
            column.Item().Element(c => ComposeFinancialSummary(c, data.FinancialInfo));
            column.Item().PaddingVertical(10);

            // 6. Volunteer Deployment (RENUMBERED from 5)
            column.Item().Element(c => ComposeVolunteerDeployment(c, data.VolunteerInfo));
            column.Item().PaddingVertical(10);

            // 7. Inventory Report (RENUMBERED from 6)
            column.Item().PageBreak();
            column.Item().Element(c => ComposeInventoryReport(c, data.InventoryInfo));
            column.Item().PaddingVertical(10);

            // 8. Operational Issues (RENUMBERED from 7)
            column.Item().Element(c => ComposeOperationalIssues(c, data.Issues));
        });
    }

    private void ComposeExecutiveSummary(QuestPdfContainer container, ExecutiveSummary summary)
    {
        container.Column(column =>
        {
            column.Item().Text("1. EXECUTIVE SUMMARY")
                .FontSize(14)
                .Bold()
                .FontColor("#07562c");

            column.Item().PaddingVertical(5);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Active Disasters: {summary.ActiveDisasters}")
                        .FontSize(11);
                    col.Item().Text($"Total Evacuees: {summary.TotalEvacuees}")
                        .FontSize(11);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Active Shelters: {summary.ActiveShelters}")
                        .FontSize(11);
                    col.Item().Text($"Volunteers Deployed: {summary.TotalVolunteers}")
                        .FontSize(11);
                });
            });

            if (!string.IsNullOrEmpty(summary.CurrentDisasterOverview))
            {
                column.Item().PaddingTop(10).Text($"Current Disasters: {summary.CurrentDisasterOverview}")
                    .FontSize(10)
                    .Italic();
            }
        });
    }

    private void ComposeDisasterOverview(QuestPdfContainer container, List<DisasterInfo> disasters)
    {
        container.Column(column =>
        {
            column.Item().Text("2. DISASTER SITUATION OVERVIEW")
                .FontSize(14)
                .Bold()
                .FontColor("#07562c");

            column.Item().PaddingVertical(5);

            if (!disasters.Any())
            {
                column.Item().Text("No active disasters reported.")
                    .FontSize(10)
                    .Italic();
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background("#07562c").Padding(5).Text("Disaster").FontColor(QuestColors.White).Bold();
                    header.Cell().Background("#07562c").Padding(5).Text("Type").FontColor(QuestColors.White).Bold();
                    header.Cell().Background("#07562c").Padding(5).Text("Severity").FontColor(QuestColors.White).Bold();
                    header.Cell().Background("#07562c").Padding(5).Text("Location").FontColor(QuestColors.White).Bold();
                    header.Cell().Background("#07562c").Padding(5).Text("Days Active").FontColor(QuestColors.White).Bold();
                });

                // Data rows
                foreach (var disaster in disasters)
                {
                    table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                        .Text(disaster.Title).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                        .Text(disaster.DisasterType).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                        .Text(disaster.Severity).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                        .Text(disaster.Location).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                        .Text(disaster.DaysActive.ToString()).FontSize(9);
                }
            });
        });
    }

    private void ComposeEvacueeStatistics(QuestPdfContainer container, EvacueeStatistics stats)
    {
        container.Column(column =>
        {
            column.Item().Text("3. EVACUEE STATISTICS")
                .FontSize(14)
                .Bold()
                .FontColor("#07562c");

            column.Item().PaddingVertical(5);

            column.Item().Text($"Total Evacuees: {stats.TotalEvacuees}")
                .FontSize(11)
                .Bold();

            column.Item().PaddingVertical(5);

            // Status Breakdown
            column.Item().Text("Status Breakdown:")
                .FontSize(11)
                .Bold();

            foreach (var status in stats.ByStatus)
            {
                column.Item().Text($"  • {status.Key}: {status.Value}")
                    .FontSize(10);
            }

            column.Item().PaddingVertical(5);

            // Breakdown by Shelter
            column.Item().Text("Breakdown per Shelter:")
                .FontSize(11)
                .Bold();

            foreach (var shelter in stats.ByShelter.OrderByDescending(s => s.Value).Take(10))
            {
                column.Item().Text($"  • {shelter.Key}: {shelter.Value}")
                    .FontSize(10);
            }
        });
    }

    private void ComposeShelterOperations(QuestPdfContainer container, ShelterOperations shelterOps)
    {
        container.Column(column =>
        {
            column.Item().Text("4. SHELTER OPERATIONS REPORT")
                .FontSize(14)
                .Bold()
                .FontColor("#07562c");

            column.Item().PaddingVertical(5);

            if (!shelterOps.ActiveShelters.Any())
            {
                column.Item().Text("No active shelters reported.")
                    .FontSize(10)
                    .Italic();
                return;
            }

            // Active Shelter List
            column.Item().Text("Active Shelters:")
                .FontSize(11)
                .Bold();

            column.Item().PaddingVertical(3);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background("#07562c").Padding(5).Text("Shelter").FontColor(QuestColors.White).Bold();
                    header.Cell().Background("#07562c").Padding(5).Text("Location").FontColor(QuestColors.White).Bold();
                    header.Cell().Background("#07562c").Padding(5).Text("Capacity").FontColor(QuestColors.White).Bold();
                    header.Cell().Background("#07562c").Padding(5).Text("Occupancy").FontColor(QuestColors.White).Bold();
                    header.Cell().Background("#07562c").Padding(5).Text("Volunteers").FontColor(QuestColors.White).Bold();
                });

                foreach (var shelter in shelterOps.ActiveShelters)
                {
                    table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                        .Text(shelter.Name).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                        .Text(shelter.Location).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                        .Text(shelter.Capacity.ToString()).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                        .Text($"{shelter.CurrentOccupancy} ({shelter.OccupancyPercent:F1}%)").FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                        .Text(shelter.AssignedVolunteers.ToString()).FontSize(9);
                }
            });

            // Shelter Needs
            if (shelterOps.Needs.Any())
            {
                column.Item().PaddingTop(10);
                column.Item().Text("Shelter Needs Assessment:")
                    .FontSize(11)
                    .Bold();

                column.Item().PaddingVertical(3);

                foreach (var need in shelterOps.Needs.OrderBy(n => n.Priority))
                {
                    column.Item().Text($"  [{need.Priority}] {need.ShelterName}: {need.ItemName} (Current: {need.CurrentQuantity}, Need: {need.RequiredQuantity})")
                        .FontSize(9);
                }
            }
        });
    }

    // RENUMBERED from 4.5 to 5
    private void ComposeFinancialSummary(QuestPdfContainer container, FinancialSummary financial)
    {
        container.Column(column =>
        {
            column.Item().Text("5. FINANCIAL SUMMARY")
                .FontSize(14)
                .Bold()
                .FontColor("#07562c");

            column.Item().PaddingVertical(5);

            // Financial KPIs
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Funds Received (Procurement)")
                        .FontSize(10)
                        .FontColor("#666");
                    col.Item().Text($"₱{financial.TotalFundsReceived:N2}")
                        .FontSize(14)
                        .Bold()
                        .FontColor("#16a34a");
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Total Expenditures")
                        .FontSize(10)
                        .FontColor("#666");
                    col.Item().Text($"₱{financial.TotalExpenditures:N2}")
                        .FontSize(14)
                        .Bold()
                        .FontColor("#dc2626");
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Balance")
                        .FontSize(10)
                        .FontColor("#666");
                    col.Item().Text($"₱{financial.Balance:N2}")
                        .FontSize(14)
                        .Bold()
                        .FontColor(financial.Balance >= 0 ? "#16a34a" : "#dc2626");
                    col.Item().Text(financial.Balance >= 0 ? "Surplus" : "Deficit")
                        .FontSize(8)
                        .Italic()
                        .FontColor("#666");
                });
            });

            column.Item().PaddingVertical(10);

            // Procurement Breakdown
            if (financial.ProcurementBreakdown.Any())
            {
                column.Item().Text("Procurement Requests Breakdown")
                    .FontSize(11)
                    .Bold();

                column.Item().PaddingVertical(3);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(50);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.ConstantColumn(70);
                        columns.RelativeColumn(1);
                        columns.ConstantColumn(80);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background("#07562c").Padding(5).Text("Req ID").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Barangay").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Supplier").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Status").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).AlignRight().Text("Amount").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Date").FontColor(QuestColors.White).Bold();
                    });

                    // Data rows
                    foreach (var procurement in financial.ProcurementBreakdown.Take(20))
                    {
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text($"#{procurement.RequestId}").FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(procurement.BarangayName).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(procurement.Supplier).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(procurement.Status).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .AlignRight().Text($"₱{procurement.Amount:N2}").FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(procurement.RequestDate.ToString("MMM dd, yyyy")).FontSize(8);
                    }

                    // Total row
                    table.Cell().ColumnSpan(4).Background("#f9fafb").Padding(5)
                        .AlignRight().Text("Total Procurement:").FontSize(9).Bold();
                    table.Cell().Background("#f9fafb").Padding(5)
                        .AlignRight().Text($"₱{financial.TotalFundsReceived:N2}").FontSize(9).Bold().FontColor("#16a34a");
                    table.Cell().Background("#f9fafb").Padding(5);
                });
            }

            column.Item().PaddingVertical(10);

            // Expenditures Breakdown
            if (financial.ExpenditureBreakdown.Any())
            {
                column.Item().Text("Expenditures Breakdown")
                    .FontSize(11)
                    .Bold();

                column.Item().PaddingVertical(3);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.ConstantColumn(80);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background("#07562c").Padding(5).Text("Category").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Description").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Reference").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).AlignRight().Text("Amount").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Date").FontColor(QuestColors.White).Bold();
                    });

                    // Data rows
                    foreach (var expenditure in financial.ExpenditureBreakdown.Take(20))
                    {
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(expenditure.Category).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(expenditure.Description).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(expenditure.Reference).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .AlignRight().Text($"₱{expenditure.Amount:N2}").FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(expenditure.Date.ToString("MMM dd, yyyy")).FontSize(8);
                    }

                    // Total row
                    table.Cell().ColumnSpan(3).Background("#f9fafb").Padding(5)
                        .AlignRight().Text("Total Expenditures:").FontSize(9).Bold();
                    table.Cell().Background("#f9fafb").Padding(5)
                        .AlignRight().Text($"₱{financial.TotalExpenditures:N2}").FontSize(9).Bold().FontColor("#dc2626");
                    table.Cell().Background("#f9fafb").Padding(5);
                });
            }

            column.Item().PaddingVertical(10);

            // Balance Summary
            column.Item().Background("#f9fafb").Border(2).BorderColor("#e5e7eb").Padding(10)
                .Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("CURRENT BALANCE")
                            .FontSize(10)
                            .FontColor("#666")
                            .Bold();
                        col.Item().Text($"₱{financial.Balance:N2}")
                            .FontSize(18)
                            .Bold()
                            .FontColor(financial.Balance >= 0 ? "#16a34a" : "#dc2626");
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("Status")
                            .FontSize(9)
                            .FontColor("#666");
                        col.Item().Text(financial.Balance >= 0 ? "✓ Surplus" : "⚠ Deficit")
                            .FontSize(11)
                            .Bold()
                            .FontColor(financial.Balance >= 0 ? "#16a34a" : "#dc2626");
                    });
                });

            if (!financial.ProcurementBreakdown.Any() && !financial.ExpenditureBreakdown.Any())
            {
                column.Item().Text("No financial transactions recorded for this reporting period.")
                    .FontSize(10)
                    .Italic()
                    .FontColor("#666");
            }
        });
    }

    // RENUMBERED from 5 to 6
    private void ComposeVolunteerDeployment(QuestPdfContainer container, VolunteerDeployment volunteerInfo)
    {
        container.Column(column =>
        {
            column.Item().Text("6. VOLUNTEER DEPLOYMENT REPORT")
                .FontSize(14)
                .Bold()
                .FontColor("#07562c");

            column.Item().PaddingVertical(5);

            column.Item().Text($"Total Active Volunteers: {volunteerInfo.TotalActiveVolunteers}")
                .FontSize(11)
                .Bold();

            column.Item().PaddingVertical(5);

            if (!volunteerInfo.Assignments.Any())
            {
                column.Item().Text("No volunteers currently deployed.")
                    .FontSize(10)
                    .Italic();
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Background("#07562c").Padding(5).Text("Volunteer").FontColor(QuestColors.White).Bold();
                    header.Cell().Background("#07562c").Padding(5).Text("Assigned Shelter").FontColor(QuestColors.White).Bold();
                    header.Cell().Background("#07562c").Padding(5).Text("Skills").FontColor(QuestColors.White).Bold();
                });

                foreach (var assignment in volunteerInfo.Assignments.Take(50))
                {
                    table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                        .Text(assignment.VolunteerName).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                        .Text(assignment.AssignedShelter).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                        .Text(assignment.Skills).FontSize(9);
                }
            });
        });
    }

    // RENUMBERED from 6 to 7
    private void ComposeInventoryReport(QuestPdfContainer container, InventoryReport inventory)
    {
        container.Column(column =>
        {
            column.Item().Text("7. INVENTORY REPORT")
                .FontSize(14)
                .Bold()
                .FontColor("#07562c");

            column.Item().PaddingVertical(5);

            // Executive Summary
            column.Item().Text("Executive Summary")
                .FontSize(12)
                .Bold()
                .FontColor("#07562c");

            column.Item().PaddingVertical(3);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Total Items in Stock: {inventory.ExecutiveSummary.TotalItemsInStock}")
                        .FontSize(10);
                    col.Item().Text($"Stock Received (Period): {inventory.ExecutiveSummary.TotalStockReceived}")
                        .FontSize(10);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Stock Released (Period): {inventory.ExecutiveSummary.TotalStockReleased}")
                        .FontSize(10);
                    col.Item().Text($"Overall Condition: {inventory.ExecutiveSummary.OverallCondition}")
                        .FontSize(10);
                });
            });

            column.Item().PaddingVertical(3);

            column.Item().Text($"Critically Low Items: {inventory.ExecutiveSummary.CriticallyLowItems} | Low Stock Items: {inventory.ExecutiveSummary.LowStockItems}")
                .FontSize(10)
                .FontColor("#ef4444")
                .Bold();

            if (inventory.ExecutiveSummary.ImmediateProcurementNeeds.Any())
            {
                column.Item().PaddingVertical(3);
                column.Item().Text("Immediate Procurement Requirements:")
                    .FontSize(10)
                    .Bold();
                foreach (var need in inventory.ExecutiveSummary.ImmediateProcurementNeeds)
                {
                    column.Item().Text($"  • {need}")
                        .FontSize(9);
                }
            }

            column.Item().PaddingVertical(10);

            // Current Inventory Status
            column.Item().Text("Current Inventory Status")
                .FontSize(12)
                .Bold()
                .FontColor("#07562c");

            column.Item().PaddingVertical(3);

            if (!inventory.CurrentInventory.Any())
            {
                column.Item().Text("No inventory items found.")
                    .FontSize(10)
                    .Italic();
            }
            else
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.ConstantColumn(50);
                        columns.ConstantColumn(50);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background("#07562c").Padding(5).Text("Item").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Category").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Qty").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Unit").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Stock Level").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Location").FontColor(QuestColors.White).Bold();
                    });

                    // Data rows
                    foreach (var item in inventory.CurrentInventory.Take(20))
                    {
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(item.ItemName).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(item.Category).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(item.AvailableQuantity.ToString()).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(item.Unit).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(item.StockLevel).FontSize(8)
                            .FontColor(item.StockLevel == "Critical" ? "#ef4444" : item.StockLevel == "Low" ? "#f59e0b" : "#10b981");
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(item.StorageLocation).FontSize(8);
                    }
                });
            }

            column.Item().PaddingVertical(10);

            // Stock-In Summary
            column.Item().Text("Stock-In Summary (Recent Transactions)")
                .FontSize(12)
                .Bold()
                .FontColor("#07562c");

            column.Item().PaddingVertical(3);

            if (!inventory.StockInSummary.Any())
            {
                column.Item().Text("No stock-in transactions recorded.")
                    .FontSize(10)
                    .Italic();
            }
            else
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);
                        columns.RelativeColumn(2);
                        columns.ConstantColumn(60);
                        columns.RelativeColumn(1);
                        columns.ConstantColumn(80);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background("#07562c").Padding(5).Text("ID").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Item").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Quantity").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Supplier").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Date").FontColor(QuestColors.White).Bold();
                    });

                    // Data rows
                    foreach (var transaction in inventory.StockInSummary.Take(15))
                    {
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text($"#{transaction.TransactionId}").FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(transaction.ItemName).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text($"+{transaction.Quantity} {transaction.Unit}").FontSize(8).FontColor("#16a34a");
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(transaction.Supplier).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(transaction.ReceivedDate.ToString("MMM dd, yyyy")).FontSize(8);
                    }
                });
            }

            column.Item().PaddingVertical(10);

            // Stock-Out Summary
            column.Item().Text("Stock-Out Summary (Recent Transactions)")
                .FontSize(12)
                .Bold()
                .FontColor("#07562c");

            column.Item().PaddingVertical(3);

            if (!inventory.StockOutSummary.Any())
            {
                column.Item().Text("No stock-out transactions recorded.")
                    .FontSize(10)
                    .Italic();
            }
            else
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);
                        columns.RelativeColumn(2);
                        columns.ConstantColumn(60);
                        columns.RelativeColumn(1);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(60);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background("#07562c").Padding(5).Text("ID").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Item").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Quantity").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Destination").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Date").FontColor(QuestColors.White).Bold();
                        header.Cell().Background("#07562c").Padding(5).Text("Balance").FontColor(QuestColors.White).Bold();
                    });

                    // Data rows
                    foreach (var transaction in inventory.StockOutSummary.Take(15))
                    {
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text($"#{transaction.TransactionId}").FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(transaction.ItemName).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text($"-{transaction.QuantityReleased} {transaction.Unit}").FontSize(8).FontColor("#dc2626");
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(transaction.Destination).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(transaction.ReleaseDate.ToString("MMM dd, yyyy")).FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(QuestColors.Grey.Lighten3).Padding(5)
                            .Text(transaction.RemainingBalance.ToString()).FontSize(8);
                    }
                });
            }
        });
    }

    // RENUMBERED from 7 to 8
    private void ComposeOperationalIssues(QuestPdfContainer container, OperationalIssues issues)
    {
        container.Column(column =>
        {
            column.Item().Text("8. OPERATIONAL ISSUES & RECOMMENDATIONS")
                .FontSize(14)
                .Bold()
                .FontColor("#07562c");

            column.Item().PaddingVertical(5);

            column.Item().Text("(Editable by Operations Officer)")
                .FontSize(9)
                .Italic()
                .FontColor("#666");

            column.Item().PaddingVertical(5);

            if (!string.IsNullOrEmpty(issues.NeededSupport))
            {
                column.Item().Text("Needed Support:").FontSize(10).Bold();
                column.Item().Text(issues.NeededSupport).FontSize(9);
                column.Item().PaddingVertical(3);
            }

            if (!string.IsNullOrEmpty(issues.UrgentSupplies))
            {
                column.Item().Text("Urgent Supplies:").FontSize(10).Bold();
                column.Item().Text(issues.UrgentSupplies).FontSize(9);
                column.Item().PaddingVertical(3);
            }

            if (!string.IsNullOrEmpty(issues.FundingRequests))
            {
                column.Item().Text("Funding Requests:").FontSize(10).Bold();
                column.Item().Text(issues.FundingRequests).FontSize(9);
                column.Item().PaddingVertical(3);
            }

            if (!string.IsNullOrEmpty(issues.Concerns))
            {
                column.Item().Text("Concerns:").FontSize(10).Bold();
                column.Item().Text(issues.Concerns).FontSize(9);
                column.Item().PaddingVertical(3);
            }

            if (string.IsNullOrEmpty(issues.NeededSupport) && string.IsNullOrEmpty(issues.UrgentSupplies) &&
                string.IsNullOrEmpty(issues.FundingRequests) && string.IsNullOrEmpty(issues.Concerns))
            {
                column.Item().Text("No operational issues reported at this time.")
                    .FontSize(10)
                    .Italic();
            }
        });
    }

    private void ComposeFooter(QuestPdfContainer container, OperationsReportData data)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Generated: {data.GeneratedAt:MMM dd, yyyy HH:mm}")
                .FontSize(8)
                .FontColor("#666");

            row.RelativeItem().AlignCenter().Text($"By: {data.GeneratedBy}")
                .FontSize(8)
                .FontColor("#666");

            row.RelativeItem().AlignRight().Text(text =>
            {
                text.Span("Page ");
                text.CurrentPageNumber().FontSize(8);
                text.Span(" of ");
                text.TotalPages().FontSize(8);
            });
        });
    }
}
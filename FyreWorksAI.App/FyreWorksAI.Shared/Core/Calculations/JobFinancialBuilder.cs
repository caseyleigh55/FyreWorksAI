using System.Text;

namespace FyreWorksAI.Shared.Core.Calculations;

//******************************//
//****** Job Build Logic *******//
//******************************//
internal static class JobFinancialBuilder
{
    public static BaselineEstimate BuildBaselineFromBid(BidRecord bid, string jobNumber)
    {
        var installHours = JobFinancialMath.GetBidAllocatedPhaseHours(bid, line => line.InstallHours);
        var demoHours = JobFinancialMath.GetBidAllocatedPhaseHours(bid, line => line.DemoHours);
        var trimHours = JobFinancialMath.GetBidAllocatedPhaseHours(bid, line => line.TrimHours);
        var testHours = JobFinancialMath.GetBidAllocatedPhaseHours(bid, line => line.TestHours);
        var rawCategorySales = new Dictionary<string, decimal>
        {
            [JobCostCodes.Admin] = EstimateMath.GetBidAdministrativeTaskSale(bid),
            [JobCostCodes.Engineering] = EstimateMath.GetBidEngineeringTaskSale(bid),
            [JobCostCodes.Components] = EstimateMath.GetBidComponentSale(bid),
            [JobCostCodes.Wire] = EstimateMath.GetBidWireSale(bid),
            [JobCostCodes.Material] = EstimateMath.GetBidMaterialOnlySale(bid),
            [JobCostCodes.Install] = GetBidPhaseSale(bid, line => line.InstallHours),
            [JobCostCodes.Demo] = GetBidPhaseSale(bid, line => line.DemoHours),
            [JobCostCodes.Trim] = GetBidPhaseSale(bid, line => line.TrimHours),
            [JobCostCodes.Test] = GetBidPhaseSale(bid, line => line.TestHours)
        };

        var baseline = new BaselineEstimate
        {
            SourceBidNumber = bid.BidNumber,
            ScopeSummary = !string.IsNullOrWhiteSpace(bid.Site.ScopeOfWork) ? bid.Site.ScopeOfWork : bid.ScopeSummary,
            OriginalRevenue = EstimateMath.GetBidAdjustedRevenue(bid),
            EstimatedLaborCost = EstimateMath.GetBidLaborCost(bid),
            EstimatedFieldLaborSale = EstimateMath.RoundCurrency(
                rawCategorySales[JobCostCodes.Install] +
                rawCategorySales[JobCostCodes.Demo] +
                rawCategorySales[JobCostCodes.Trim] +
                rawCategorySales[JobCostCodes.Test]),
            EstimatedMaterialCost = EstimateMath.GetBidMaterialCost(bid),
            EstimatedMaterialSale = EstimateMath.RoundCurrency(
                rawCategorySales[JobCostCodes.Components] +
                rawCategorySales[JobCostCodes.Wire] +
                rawCategorySales[JobCostCodes.Material]),
            EstimatedTotalCost = EstimateMath.GetBidEstimatedCost(bid),
            EstimatedFieldHours = EstimateMath.GetBidAllocatedFieldHours(bid),
            EstimatedAdminHours = EstimateMath.GetBidAdminHours(bid),
            EstimatedEngineeringHours = EstimateMath.GetBidEngineeringHours(bid),
            EstimatedInstallHours = installHours,
            EstimatedDemoHours = demoHours,
            EstimatedTrimHours = trimHours,
            EstimatedTestHours = testHours,
            EstimatedInstallCost = GetBidPhaseCost(bid, line => line.InstallHours),
            EstimatedDemoCost = GetBidPhaseCost(bid, line => line.DemoHours),
            EstimatedTrimCost = GetBidPhaseCost(bid, line => line.TrimHours),
            EstimatedTestCost = GetBidPhaseCost(bid, line => line.TestHours),
            EstimatedInstallSale = rawCategorySales[JobCostCodes.Install],
            EstimatedDemoSale = rawCategorySales[JobCostCodes.Demo],
            EstimatedTrimSale = rawCategorySales[JobCostCodes.Trim],
            EstimatedTestSale = rawCategorySales[JobCostCodes.Test],
            EstimatedAdminCost = EstimateMath.GetBidAdministrativeTaskCost(bid),
            EstimatedAdminSale = rawCategorySales[JobCostCodes.Admin],
            EstimatedEngineeringCost = EstimateMath.GetBidEngineeringTaskCost(bid),
            EstimatedEngineeringSale = rawCategorySales[JobCostCodes.Engineering],
            EstimatedComponentCost = EstimateMath.GetBidComponentCost(bid),
            EstimatedComponentSale = rawCategorySales[JobCostCodes.Components],
            EstimatedWireCost = EstimateMath.GetBidWireCost(bid),
            EstimatedWireSale = rawCategorySales[JobCostCodes.Wire],
            EstimatedMaterialOnlyCost = EstimateMath.GetBidMaterialOnlyCost(bid),
            EstimatedMaterialOnlySale = rawCategorySales[JobCostCodes.Material],
            AdministrativeTasks = bid.AdministrativeTasks.Select(CloneTask).ToList(),
            EngineeringTasks = bid.EngineeringTasks.Select(CloneTask).ToList(),
            Components = bid.Components.Select(CloneComponent).ToList(),
            DemoItems = bid.DemoItems.Select(CloneDemoItem).ToList(),
            Materials = bid.Materials.Select(CloneMaterial).ToList()
        };

        baseline.LineItems = BuildBaselineLineItems(jobNumber, bid);
        return baseline;
    }

    public static void RefreshBaselineFromBid(JobRecord job, BidRecord bid)
    {
        var refreshed = BuildBaselineFromBid(bid, job.JobNumber);
        var existingItemsByReference = job.Baseline.LineItems
            .Where(item => !string.IsNullOrWhiteSpace(item.ReferenceNumber))
            .GroupBy(item => item.ReferenceNumber.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var item in refreshed.LineItems)
        {
            if (existingItemsByReference.TryGetValue(item.ReferenceNumber, out var existingItem))
            {
                item.Id = existingItem.Id;
                item.ActualUnitCost = existingItem.ActualUnitCost;
                item.InvoiceId = existingItem.InvoiceId;
                if (!string.IsNullOrWhiteSpace(existingItem.Notes))
                {
                    item.Notes = existingItem.Notes.Trim();
                }
            }
        }

        refreshed.LineItems ??= [];
        job.Baseline = refreshed;
    }

    public static void EnsureJobDerivedData(JobRecord job, LaborTemplate? laborTemplate = null)
    {
        job.Baseline ??= new BaselineEstimate();
        job.Baseline.AdministrativeTasks ??= [];
        job.Baseline.EngineeringTasks ??= [];
        job.Baseline.Components ??= [];
        job.Baseline.DemoItems ??= [];
        job.Baseline.Materials ??= [];
        job.Baseline.LineItems ??= [];
        job.JobDevices ??= [];
        job.Invoices ??= [];
        job.DailyLogs ??= [];
        job.TimeEntries ??= [];
        job.MaterialPurchases ??= [];
        job.ChangeOrders ??= [];
        job.ScheduleOfValues ??= [];
        job.Commitments ??= [];
        job.Attachments ??= [];

        EnsureBaselineBreakdown(job);
        EnsureBaselineLineItems(job);
        EnsureJobDevices(job);
        EnsureInvoices(job);
        EnsureChangeOrders(job, laborTemplate);
        EnsureDailyLogs(job);
        EnsureTimeEntries(job);
        MigrateLegacyMaterialPurchases(job);
        EnsureMaterialPurchases(job);
        EnsureScheduleOfValues(job);
        EnsureCommitments(job);
        SyncLinkedCommitmentScheduleValueSubLines(job);
        RollUpScheduleValues(job);
    }

    public static string BuildBidProposal(BidRecord bid, ClientRecord? client)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Proposal - {bid.BidNumber}");
        builder.AppendLine($"Project: {bid.ProjectName}");
        if (client is not null)
        {
            builder.AppendLine($"Client: {client.Name}");
            if (!string.IsNullOrWhiteSpace(client.PrimaryContact))
            {
                builder.AppendLine($"Attention: {client.PrimaryContact}");
            }
        }

        if (!string.IsNullOrWhiteSpace(bid.Site.SingleLineAddress))
        {
            builder.AppendLine($"Project Address: {bid.Site.SingleLineAddress}");
        }

        builder.AppendLine($"Date: {DateTime.Now:D}");
        builder.AppendLine();
        builder.AppendLine("Scope");
        builder.AppendLine(string.IsNullOrWhiteSpace(bid.ProposalSummary)
            ? (!string.IsNullOrWhiteSpace(bid.Site.ScopeOfWork) ? bid.Site.ScopeOfWork : "Scope to be finalized.")
            : bid.ProposalSummary.Trim());
        builder.AppendLine();
        builder.AppendLine($"Proposal Amount: {EstimateMath.GetCurrency(EstimateMath.GetBidAdjustedRevenue(bid))}");

        if (!string.IsNullOrWhiteSpace(bid.Exclusions))
        {
            builder.AppendLine();
            builder.AppendLine("Exclusions");
            builder.AppendLine(bid.Exclusions.Trim());
        }

        builder.AppendLine();
        builder.AppendLine(string.IsNullOrWhiteSpace(bid.ProposalClosing)
            ? "We appreciate the opportunity to provide this proposal and are ready to proceed upon approval."
            : bid.ProposalClosing.Trim());

        return builder.ToString();
    }

    public static string BuildJobCostReport(JobRecord job, ClientRecord? client)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Job Cost Export - {job.JobNumber}");
        builder.AppendLine($"Project: {job.ProjectName}");
        if (client is not null)
        {
            builder.AppendLine($"Client: {client.Name}");
        }

        builder.AppendLine($"Generated: {DateTime.Now:G}");
        builder.AppendLine();
        builder.AppendLine("Baseline");
        builder.AppendLine($"  Original revenue: {EstimateMath.GetCurrency(job.Baseline.OriginalRevenue)}");
        builder.AppendLine($"  Estimated total cost: {EstimateMath.GetCurrency(job.Baseline.EstimatedTotalCost)}");
        builder.AppendLine($"  Admin + engineering sale: {EstimateMath.GetCurrency(job.Baseline.EstimatedAdminSale + job.Baseline.EstimatedEngineeringSale)}");
        builder.AppendLine($"  Materials sale: {EstimateMath.GetCurrency(job.Baseline.EstimatedComponentSale + job.Baseline.EstimatedWireSale + job.Baseline.EstimatedMaterialOnlySale)}");
        builder.AppendLine($"  Install / Demo / Trim / Test hours: {EstimateMath.GetHours(job.Baseline.EstimatedInstallHours)} / {EstimateMath.GetHours(job.Baseline.EstimatedDemoHours)} / {EstimateMath.GetHours(job.Baseline.EstimatedTrimHours)} / {EstimateMath.GetHours(job.Baseline.EstimatedTestHours)}");
        builder.AppendLine();
        builder.AppendLine("Actuals");
        builder.AppendLine($"  Labor cost to date: {EstimateMath.GetCurrency(EstimateMath.GetJobActualLaborCost(job))}");
        builder.AppendLine($"  Purchases cost to date: {EstimateMath.GetCurrency(EstimateMath.GetJobActualMaterialCost(job))}");
        builder.AppendLine($"  Commitment billings: {EstimateMath.GetCurrency(EstimateMath.GetJobBilledCommitments(job))}");
        builder.AppendLine($"  Contract revenue incl. approved COs: {EstimateMath.GetCurrency(EstimateMath.GetJobRevenue(job))}");
        builder.AppendLine($"  Actual cost to date: {EstimateMath.GetCurrency(EstimateMath.GetJobActualCost(job))}");
        builder.AppendLine($"  Exposure including commitments: {EstimateMath.GetCurrency(EstimateMath.GetJobCommittedExposure(job))}");
        builder.AppendLine($"  Profit to date: {EstimateMath.GetCurrency(EstimateMath.GetJobProfit(job))}");
        builder.AppendLine($"  Margin to date: {EstimateMath.GetPercent(EstimateMath.GetJobMarginPercent(job))}");
        builder.AppendLine();
        builder.AppendLine("Hours");
        foreach (var costCode in JobCostCodes.TimeEntryCodes.Where(code => code != JobCostCodes.Other))
        {
            var planned = JobFinancialMath.GetBaselineHours(job.Baseline, costCode);
            var used = JobFinancialMath.GetJobActualHours(job, costCode);
            builder.AppendLine($"  {JobCostCodes.GetLabel(costCode)}: planned {EstimateMath.GetHours(planned)}, used {EstimateMath.GetHours(used)}, remaining {EstimateMath.GetHours(Math.Max(0m, planned - used))}");
        }

        builder.AppendLine();
        builder.AppendLine("Schedule of Values");
        foreach (var item in job.ScheduleOfValues.OrderBy(item => item.IsChangeOrderLine).ThenBy(item => item.Description))
        {
            builder.AppendLine($"  {item.Description}: scheduled {EstimateMath.GetCurrency(item.ScheduledValue)}, billed {EstimateMath.GetCurrency(item.BilledToDate)}, paid {EstimateMath.GetCurrency(item.PaidToDate)}");
        }

        builder.AppendLine();
        builder.AppendLine("Invoices");
        foreach (var invoice in job.Invoices.OrderBy(item => item.InvoiceDate).ThenBy(item => item.ReferenceNumber))
        {
            builder.AppendLine($"  {invoice.InvoiceDate:d} | {invoice.ReferenceNumber} | {invoice.Vendor} | total {EstimateMath.GetCurrency(invoice.InvoiceTotal)} | linked {EstimateMath.GetCurrency(JobFinancialMath.GetInvoiceLinkedActualCost(job, invoice.Id))} | tax/remainder {EstimateMath.GetCurrency(JobFinancialMath.GetInvoiceAutoRemainder(job, invoice))}");
        }

        return builder.ToString();
    }

    private static void EnsureBaselineBreakdown(JobRecord job)
    {
        var baseline = job.Baseline;

        if (baseline.EstimatedAdminCost <= 0m && baseline.AdministrativeTasks.Count > 0)
        {
            baseline.EstimatedAdminCost = EstimateMath.RoundCurrency(baseline.AdministrativeTasks.Sum(task => task.CostPrice));
        }

        if (baseline.EstimatedAdminSale <= 0m && baseline.AdministrativeTasks.Count > 0)
        {
            baseline.EstimatedAdminSale = EstimateMath.RoundCurrency(baseline.AdministrativeTasks.Sum(task => task.SalePrice));
        }

        if (baseline.EstimatedEngineeringCost <= 0m && baseline.EngineeringTasks.Count > 0)
        {
            baseline.EstimatedEngineeringCost = EstimateMath.RoundCurrency(baseline.EngineeringTasks.Sum(task => task.CostPrice));
        }

        if (baseline.EstimatedEngineeringSale <= 0m && baseline.EngineeringTasks.Count > 0)
        {
            baseline.EstimatedEngineeringSale = EstimateMath.RoundCurrency(baseline.EngineeringTasks.Sum(task => task.SalePrice));
        }

        if (baseline.EstimatedComponentCost <= 0m)
        {
            baseline.EstimatedComponentCost = EstimateMath.RoundCurrency(baseline.Components.Sum(component => component.TotalMaterialCost));
        }

        if (baseline.EstimatedComponentSale <= 0m)
        {
            baseline.EstimatedComponentSale = EstimateMath.RoundCurrency(baseline.Components.Sum(component => component.TotalMaterialSale));
        }

        if (baseline.EstimatedWireCost <= 0m)
        {
            baseline.EstimatedWireCost = EstimateMath.RoundCurrency(baseline.Materials.Where(item => item.Kind == BidMaterialKind.Wire).Sum(item => item.ExtendedCost));
        }

        if (baseline.EstimatedWireSale <= 0m)
        {
            baseline.EstimatedWireSale = EstimateMath.RoundCurrency(baseline.Materials.Where(item => item.Kind == BidMaterialKind.Wire).Sum(item => item.ExtendedSale));
        }

        if (baseline.EstimatedMaterialOnlyCost <= 0m)
        {
            baseline.EstimatedMaterialOnlyCost = EstimateMath.RoundCurrency(baseline.Materials.Where(item => item.Kind == BidMaterialKind.Material).Sum(item => item.ExtendedCost));
        }

        if (baseline.EstimatedMaterialOnlySale <= 0m)
        {
            baseline.EstimatedMaterialOnlySale = EstimateMath.RoundCurrency(baseline.Materials.Where(item => item.Kind == BidMaterialKind.Material).Sum(item => item.ExtendedSale));
        }

        baseline.EstimatedMaterialCost = EstimateMath.RoundCurrency(baseline.EstimatedComponentCost + baseline.EstimatedWireCost + baseline.EstimatedMaterialOnlyCost);
        baseline.EstimatedMaterialSale = EstimateMath.RoundCurrency(baseline.EstimatedComponentSale + baseline.EstimatedWireSale + baseline.EstimatedMaterialOnlySale);

        if (baseline.EstimatedFieldLaborSale <= 0m)
        {
            baseline.EstimatedFieldLaborSale = EstimateMath.RoundCurrency(
                baseline.EstimatedInstallSale +
                baseline.EstimatedDemoSale +
                baseline.EstimatedTrimSale +
                baseline.EstimatedTestSale);
        }

        if (baseline.EstimatedInstallHours <= 0m &&
            baseline.EstimatedDemoHours <= 0m &&
            baseline.EstimatedTrimHours <= 0m &&
            baseline.EstimatedTestHours <= 0m &&
            baseline.EstimatedFieldHours > 0m)
        {
            baseline.EstimatedInstallHours = EstimateMath.RoundHours(baseline.EstimatedFieldHours);
        }

        if (baseline.EstimatedInstallCost <= 0m &&
            baseline.EstimatedDemoCost <= 0m &&
            baseline.EstimatedTrimCost <= 0m &&
            baseline.EstimatedTestCost <= 0m &&
            baseline.EstimatedLaborCost > 0m)
        {
            baseline.EstimatedInstallCost = EstimateMath.RoundCurrency(Math.Max(0m, baseline.EstimatedLaborCost - baseline.EstimatedAdminCost - baseline.EstimatedEngineeringCost));
        }

        if (baseline.EstimatedInstallSale <= 0m &&
            baseline.EstimatedDemoSale <= 0m &&
            baseline.EstimatedTrimSale <= 0m &&
            baseline.EstimatedTestSale <= 0m &&
            baseline.OriginalRevenue > 0m)
        {
            baseline.EstimatedInstallSale = EstimateMath.RoundCurrency(Math.Max(0m, baseline.OriginalRevenue - baseline.EstimatedAdminSale - baseline.EstimatedEngineeringSale - baseline.EstimatedMaterialSale));
        }
    }

    private static void EnsureBaselineLineItems(JobRecord job)
    {
        if (job.Baseline.LineItems.Count > 0)
        {
            var sequenceByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in job.Baseline.LineItems)
            {
                var normalizedCategory = JobCostCodes.Normalize(item.CategoryCode);
                sequenceByCategory.TryGetValue(normalizedCategory, out var nextSequence);
                nextSequence++;
                sequenceByCategory[normalizedCategory] = nextSequence;
                item.CategoryCode = normalizedCategory;
                item.Quantity = item.Quantity <= 0m ? 1m : item.Quantity;
                item.UnitLabel = string.IsNullOrWhiteSpace(item.UnitLabel) ? "ea" : item.UnitLabel.Trim();
                item.EstimatedUnitCost = EstimateMath.RoundCurrency(Math.Max(0m, item.EstimatedUnitCost));
                item.EstimatedUnitSale = EstimateMath.RoundCurrency(Math.Max(0m, item.EstimatedUnitSale));
                item.ActualUnitCost = EstimateMath.RoundCurrency(Math.Max(0m, item.ActualUnitCost));
                item.ReferenceNumber = string.IsNullOrWhiteSpace(item.ReferenceNumber)
                    ? BuildReferenceNumber(job.JobNumber, normalizedCategory, nextSequence)
                    : item.ReferenceNumber.Trim();
            }

            return;
        }

        var sequenceByCategoryForNewItems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lineItems = new List<JobBaselineLineItem>();

        foreach (var task in job.Baseline.AdministrativeTasks)
        {
            var quantity = task.PricingMode == TaskPricingMode.Hourly ? Math.Max(1m, task.EstimatedHours) : 1m;
            var cost = quantity > 0m ? EstimateMath.RoundCurrency(task.CostPrice / quantity) : 0m;
            var sale = quantity > 0m ? EstimateMath.RoundCurrency(task.SalePrice / quantity) : 0m;
            lineItems.Add(CreateLineItem(job.JobNumber, sequenceByCategoryForNewItems, "Administrative Task", JobCostCodes.Admin, task.Title, quantity, task.PricingMode == TaskPricingMode.Hourly ? "hrs" : "lot", cost, sale, task.EstimatedHours, task.Notes));
        }

        foreach (var task in job.Baseline.EngineeringTasks)
        {
            var quantity = task.PricingMode == TaskPricingMode.Hourly ? Math.Max(1m, task.EstimatedHours) : 1m;
            var cost = quantity > 0m ? EstimateMath.RoundCurrency(task.CostPrice / quantity) : 0m;
            var sale = quantity > 0m ? EstimateMath.RoundCurrency(task.SalePrice / quantity) : 0m;
            lineItems.Add(CreateLineItem(job.JobNumber, sequenceByCategoryForNewItems, "Engineering Task", JobCostCodes.Engineering, task.Title, quantity, task.PricingMode == TaskPricingMode.Hourly ? "hrs" : "lot", cost, sale, task.EstimatedHours, task.Notes));
        }

        foreach (var component in job.Baseline.Components)
        {
            lineItems.Add(CreateLineItem(job.JobNumber, sequenceByCategoryForNewItems, "Component", JobCostCodes.Components, component.Name, component.Quantity, "ea", component.UnitCost, component.UnitSale, component.TotalMinutes / 60m, component.Notes));
        }

        foreach (var demoItem in job.Baseline.DemoItems)
        {
            lineItems.Add(CreateLineItem(job.JobNumber, sequenceByCategoryForNewItems, "Demo Item", JobCostCodes.Demo, demoItem.Name, demoItem.Quantity, "ea", 0m, 0m, demoItem.TotalHours, demoItem.Notes));
        }

        foreach (var material in job.Baseline.Materials.Where(item => item.Kind == BidMaterialKind.Wire))
        {
            lineItems.Add(CreateLineItem(job.JobNumber, sequenceByCategoryForNewItems, "Wire", JobCostCodes.Wire, material.Description, material.Quantity, "ea", material.UnitCost, material.UnitSale, 0m, material.Notes));
        }

        foreach (var material in job.Baseline.Materials.Where(item => item.Kind != BidMaterialKind.Wire))
        {
            lineItems.Add(CreateLineItem(job.JobNumber, sequenceByCategoryForNewItems, "Material", JobCostCodes.Material, material.Description, material.Quantity, "ea", material.UnitCost, material.UnitSale, 0m, material.Notes));
        }

        job.Baseline.LineItems = lineItems;
    }

    private static void EnsureJobDevices(JobRecord job)
    {
        foreach (var item in job.JobDevices)
        {
            NormalizeTrackedDeviceItem(
                item.CategoryCode,
                item.Description,
                item.Quantity,
                item.UnitLabel,
                item.EstimatedUnitCost,
                item.EstimatedUnitSale,
                item.ActualUnitCost,
                "Job Device",
                assignCategoryCode: value => item.CategoryCode = value,
                assignDescription: value => item.Description = value,
                assignQuantity: value => item.Quantity = value,
                assignUnitLabel: value => item.UnitLabel = value,
                assignEstimatedUnitCost: value => item.EstimatedUnitCost = value,
                assignEstimatedUnitSale: value => item.EstimatedUnitSale = value,
                assignActualUnitCost: value => item.ActualUnitCost = value);
        }
    }

    private static void EnsureInvoices(JobRecord job)
    {
        foreach (var invoice in job.Invoices)
        {
            invoice.ReferenceNumber = invoice.ReferenceNumber?.Trim() ?? string.Empty;
            invoice.Vendor = invoice.Vendor?.Trim() ?? string.Empty;
            invoice.InvoiceNumber = invoice.InvoiceNumber?.Trim() ?? string.Empty;
            invoice.Notes = invoice.Notes?.Trim() ?? string.Empty;
            invoice.InvoiceTotal = EstimateMath.RoundCurrency(Math.Max(0m, invoice.InvoiceTotal));
            invoice.Attachments ??= [];
        }
    }

    private static void EnsureChangeOrders(JobRecord job, LaborTemplate? laborTemplate)
    {
        foreach (var changeOrder in job.ChangeOrders)
        {
            changeOrder.Title = string.IsNullOrWhiteSpace(changeOrder.Title) ? "Change Order" : changeOrder.Title.Trim();
            changeOrder.RevenueAmount = EstimateMath.RoundCurrency(Math.Max(0m, changeOrder.RevenueAmount));
            changeOrder.AdditionalEstimatedCost = EstimateMath.RoundCurrency(Math.Max(0m, changeOrder.AdditionalEstimatedCost));
            changeOrder.EstimatedLaborHours = EstimateMath.RoundHours(Math.Max(0m, changeOrder.EstimatedLaborHours));
            changeOrder.DirectLaborRate = EstimateMath.RoundCurrency(Math.Max(0m, changeOrder.DirectLaborRate));
            changeOrder.BilledLaborRate = EstimateMath.RoundCurrency(Math.Max(0m, changeOrder.BilledLaborRate));
            changeOrder.EstimatedLaborRate = EstimateMath.RoundCurrency(Math.Max(0m, changeOrder.EstimatedLaborRate));
            changeOrder.Notes = changeOrder.Notes?.Trim() ?? string.Empty;
            changeOrder.DeviceItems ??= [];
            changeOrder.Attachments ??= [];

            ApplyChangeOrderLaborDefaults(changeOrder, laborTemplate);

            if (changeOrder.AdditionalEstimatedCost <= 0m &&
                changeOrder.EstimatedCostImpact > 0m &&
                changeOrder.DeviceItems.Count == 0 &&
                changeOrder.EstimatedLaborHours <= 0m)
            {
                changeOrder.AdditionalEstimatedCost = EstimateMath.RoundCurrency(changeOrder.EstimatedCostImpact);
            }

            EnsureChangeOrderDeviceItems(job, changeOrder);
            if (changeOrder.UseAutoCalculatedSale)
            {
                changeOrder.RevenueAmount = JobFinancialMath.GetChangeOrderCalculatedSale(changeOrder);
            }

            changeOrder.EstimatedCostImpact = JobFinancialMath.GetChangeOrderEstimatedCost(changeOrder);
        }
    }

    private static void ApplyChangeOrderLaborDefaults(ChangeOrderRecord changeOrder, LaborTemplate? laborTemplate)
    {
        var legacyLaborRate = EstimateMath.RoundCurrency(Math.Max(0m, changeOrder.EstimatedLaborRate));
        var defaultDirectRate = EstimateMath.RoundCurrency(Math.Max(0m, laborTemplate?.JourneymanRegularDirectRate ?? legacyLaborRate));
        var defaultBilledRate = EstimateMath.RoundCurrency(Math.Max(0m, laborTemplate?.JourneymanRegularBilledRate ?? legacyLaborRate));

        if (changeOrder.DirectLaborRate <= 0m)
        {
            changeOrder.DirectLaborRate = defaultDirectRate > 0m ? defaultDirectRate : legacyLaborRate;
        }

        if (changeOrder.BilledLaborRate <= 0m)
        {
            changeOrder.BilledLaborRate = defaultBilledRate > 0m ? defaultBilledRate : changeOrder.DirectLaborRate;
        }
    }

    private static void EnsureChangeOrderDeviceItems(JobRecord job, ChangeOrderRecord changeOrder)
    {
        foreach (var item in changeOrder.DeviceItems)
        {
            NormalizeTrackedDeviceItem(
                item.CategoryCode,
                item.Description,
                item.Quantity,
                item.UnitLabel,
                item.EstimatedUnitCost,
                item.EstimatedUnitSale,
                item.ActualUnitCost,
                "Change Order Device",
                assignCategoryCode: value => item.CategoryCode = value,
                assignDescription: value => item.Description = value,
                assignQuantity: value => item.Quantity = value,
                assignUnitLabel: value => item.UnitLabel = value,
                assignEstimatedUnitCost: value => item.EstimatedUnitCost = value,
                assignEstimatedUnitSale: value => item.EstimatedUnitSale = value,
                assignActualUnitCost: value => item.ActualUnitCost = value);

            item.InvoiceId = item.InvoiceId is not null && job.Invoices.All(invoice => invoice.Id != item.InvoiceId.Value)
                ? null
                : item.InvoiceId;
            item.Notes = item.Notes?.Trim() ?? string.Empty;
        }
    }

    private static void EnsureDailyLogs(JobRecord job)
    {
        var logsById = new Dictionary<Guid, JobDailyLogRecord>();

        foreach (var dailyLog in job.DailyLogs)
        {
            dailyLog.WorkDate = dailyLog.WorkDate.Date;
            dailyLog.Description = string.IsNullOrWhiteSpace(dailyLog.Description) ? "Daily Log" : dailyLog.Description.Trim();
            dailyLog.Attachments ??= [];

            if (!logsById.ContainsKey(dailyLog.Id))
            {
                logsById[dailyLog.Id] = dailyLog;
            }
        }

        foreach (var entry in job.TimeEntries)
        {
            JobDailyLogRecord? linkedDailyLog = null;
            if (entry.DailyLogId is not null)
            {
                logsById.TryGetValue(entry.DailyLogId.Value, out linkedDailyLog);
            }

            if (linkedDailyLog is null)
            {
                var workDate = entry.WorkDate.Date;
                linkedDailyLog = job.DailyLogs.FirstOrDefault(item => item.WorkDate.Date == workDate);
                if (linkedDailyLog is null)
                {
                    linkedDailyLog = new JobDailyLogRecord
                    {
                        WorkDate = workDate,
                        Description = "Daily Log"
                    };

                    job.DailyLogs.Add(linkedDailyLog);
                    logsById[linkedDailyLog.Id] = linkedDailyLog;
                }

                entry.DailyLogId = linkedDailyLog.Id;
            }

            entry.WorkDate = linkedDailyLog.WorkDate.Date;
        }
    }

    private static void EnsureTimeEntries(JobRecord job)
    {
        var defaultChangeOrderId = job.ChangeOrders.FirstOrDefault()?.Id;
        var validChangeOrderIds = job.ChangeOrders.Select(changeOrder => changeOrder.Id).ToHashSet();
        var validDailyLogIds = job.DailyLogs.Select(dailyLog => dailyLog.Id).ToHashSet();

        foreach (var entry in job.TimeEntries)
        {
            entry.WorkDate = entry.WorkDate.Date;
            entry.CostCode = JobCostCodes.Normalize(entry.CostCode);
            entry.CrewMember = entry.CrewMember?.Trim() ?? string.Empty;
            var laborClassCandidate = IsLegacyLaborClassValue(entry.CrewMember)
                ? entry.CrewMember
                : entry.LaborClass;
            entry.LaborClass = NormalizeTimeEntryLaborClass(laborClassCandidate, entry.CrewMember, entry.CostCode);
            if (IsLegacyLaborClassValue(entry.CrewMember))
            {
                entry.CrewMember = string.Empty;
            }

            entry.Hours = EstimateMath.RoundHours(Math.Max(0m, entry.Hours));
            entry.HourlyRate = EstimateMath.RoundCurrency(Math.Max(0m, entry.HourlyRate));
            entry.Notes = entry.Notes?.Trim() ?? string.Empty;

            if (entry.DailyLogId is null || !validDailyLogIds.Contains(entry.DailyLogId.Value))
            {
                entry.DailyLogId = job.DailyLogs.FirstOrDefault(item => item.WorkDate.Date == entry.WorkDate.Date)?.Id
                    ?? job.DailyLogs.FirstOrDefault()?.Id;
            }

            if (entry.ChangeOrderId is not null && !validChangeOrderIds.Contains(entry.ChangeOrderId.Value))
            {
                entry.ChangeOrderId = null;
            }

            if (entry.CostCode == JobCostCodes.ChangeOrder)
            {
                entry.ChangeOrderId ??= defaultChangeOrderId;
                if (entry.ChangeOrderId is null)
                {
                    entry.CostCode = JobCostCodes.Other;
                }
            }
            else
            {
                entry.ChangeOrderId = null;
            }
        }
    }

    private static string NormalizeTimeEntryLaborClass(string? laborClass, string? crewMember, string? costCode)
    {
        var candidate = string.IsNullOrWhiteSpace(laborClass) ? crewMember : laborClass;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return JobCostCodes.Normalize(costCode) switch
            {
                JobCostCodes.Admin => JobCostCodes.Admin,
                JobCostCodes.Engineering => JobCostCodes.Engineering,
                _ => nameof(PersonnelType.Journeyman)
            };
        }

        return candidate.Trim().ToLowerInvariant() switch
        {
            "journeyman" or "technician" => nameof(PersonnelType.Journeyman),
            "apprentice" => nameof(PersonnelType.Apprentice),
            "admin" or "administrative" => JobCostCodes.Admin,
            "engineering" => JobCostCodes.Engineering,
            _ => JobCostCodes.Normalize(costCode) switch
            {
                JobCostCodes.Admin => JobCostCodes.Admin,
                JobCostCodes.Engineering => JobCostCodes.Engineering,
                _ => nameof(PersonnelType.Journeyman)
            }
        };
    }

    private static bool IsLegacyLaborClassValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim().ToLowerInvariant() is
            "journeyman" or
            "technician" or
            "apprentice" or
            "admin" or
            "administrative" or
            "engineering";
    }

    private static void NormalizeTrackedDeviceItem(
        string? categoryCode,
        string? description,
        decimal quantity,
        string? unitLabel,
        decimal estimatedUnitCost,
        decimal estimatedUnitSale,
        decimal actualUnitCost,
        string defaultDescription,
        Action<string> assignCategoryCode,
        Action<string> assignDescription,
        Action<decimal> assignQuantity,
        Action<string> assignUnitLabel,
        Action<decimal> assignEstimatedUnitCost,
        Action<decimal> assignEstimatedUnitSale,
        Action<decimal> assignActualUnitCost)
    {
        assignCategoryCode(JobCostCodes.Normalize(categoryCode));
        assignDescription(string.IsNullOrWhiteSpace(description) ? defaultDescription : description.Trim());
        assignQuantity(quantity <= 0m ? 1m : quantity);
        assignUnitLabel(string.IsNullOrWhiteSpace(unitLabel) ? "ea" : unitLabel.Trim());
        assignEstimatedUnitCost(EstimateMath.RoundCurrency(Math.Max(0m, estimatedUnitCost)));
        assignEstimatedUnitSale(EstimateMath.RoundCurrency(Math.Max(0m, estimatedUnitSale)));
        assignActualUnitCost(EstimateMath.RoundCurrency(Math.Max(0m, actualUnitCost)));
    }

    private static void MigrateLegacyMaterialPurchases(JobRecord job)
    {
        if (job.MaterialPurchases.Count == 0)
        {
            return;
        }

        var alreadyUsingNewTracking =
            job.Invoices.Count > 0 ||
            job.JobDevices.Count > 0 ||
            job.Baseline.LineItems.Any(item => item.ActualUnitCost > 0m || item.InvoiceId is not null);
        if (alreadyUsingNewTracking)
        {
            return;
        }

        foreach (var purchase in job.MaterialPurchases.OrderBy(item => item.PurchaseDate))
        {
            var invoiceReference = !string.IsNullOrWhiteSpace(purchase.ReceiptNumber)
                ? purchase.ReceiptNumber.Trim()
                : purchase.ReferenceNumber.Trim();
            var invoice = new JobInvoiceRecord
            {
                ReferenceNumber = invoiceReference,
                InvoiceDate = purchase.PurchaseDate,
                Vendor = purchase.Vendor?.Trim() ?? string.Empty,
                InvoiceNumber = purchase.ReceiptNumber?.Trim() ?? string.Empty,
                InvoiceTotal = JobFinancialMath.GetMaterialPurchaseTotal(purchase),
                Notes = purchase.Notes?.Trim() ?? string.Empty,
                Attachments = purchase.Attachments ?? []
            };

            job.Invoices.Add(invoice);

            var quantity = purchase.Quantity <= 0m ? 1m : purchase.Quantity;
            var unitCost = EstimateMath.RoundCurrency(JobFinancialMath.GetMaterialPurchaseSubtotal(purchase) / quantity);
            var lineItem = purchase.BaselineLineItemId is null
                ? null
                : job.Baseline.LineItems.FirstOrDefault(item => item.Id == purchase.BaselineLineItemId.Value);
            if (lineItem is not null)
            {
                lineItem.ActualUnitCost = unitCost;
                lineItem.InvoiceId = invoice.Id;
                if (string.IsNullOrWhiteSpace(lineItem.Notes) && !string.IsNullOrWhiteSpace(purchase.Notes))
                {
                    lineItem.Notes = purchase.Notes.Trim();
                }

                continue;
            }

            job.JobDevices.Add(new JobDeviceItem
            {
                CategoryCode = GetLegacyPurchaseCategory(purchase),
                Description = string.IsNullOrWhiteSpace(purchase.Description) ? "Legacy Purchase" : purchase.Description.Trim(),
                Quantity = quantity,
                UnitLabel = "ea",
                ActualUnitCost = unitCost,
                InvoiceId = invoice.Id,
                Notes = purchase.Notes?.Trim() ?? string.Empty
            });
        }

        job.MaterialPurchases.Clear();
    }

    private static void EnsureMaterialPurchases(JobRecord job)
    {
        foreach (var purchase in job.MaterialPurchases)
        {
            purchase.Attachments ??= [];
            if (purchase.Quantity <= 0m)
            {
                purchase.Quantity = 1m;
            }

            if (purchase.UnitCost <= 0m && purchase.ActualCost > 0m)
            {
                purchase.UnitCost = EstimateMath.RoundCurrency(purchase.ActualCost);
            }

            purchase.UnitCost = EstimateMath.RoundCurrency(Math.Max(0m, purchase.UnitCost));
            purchase.SalesTax = EstimateMath.RoundCurrency(Math.Max(0m, purchase.SalesTax));
            purchase.ActualCost = JobFinancialMath.GetMaterialPurchaseTotal(purchase);

            if (purchase.BaselineLineItemId is not null)
            {
                var lineItem = job.Baseline.LineItems.FirstOrDefault(item => item.Id == purchase.BaselineLineItemId.Value);
                if (lineItem is not null)
                {
                    if (string.IsNullOrWhiteSpace(purchase.ReferenceNumber))
                    {
                        purchase.ReferenceNumber = lineItem.ReferenceNumber;
                    }

                    if (string.IsNullOrWhiteSpace(purchase.Description))
                    {
                        purchase.Description = lineItem.Description;
                    }
                }
            }
        }
    }

    private static string GetLegacyPurchaseCategory(JobMaterialPurchase purchase)
    {
        var reference = purchase.ReferenceNumber?.Trim() ?? string.Empty;
        if (reference.Contains("WIRE", StringComparison.OrdinalIgnoreCase))
        {
            return JobCostCodes.Wire;
        }

        if (reference.Contains("COMP", StringComparison.OrdinalIgnoreCase))
        {
            return JobCostCodes.Components;
        }

        return JobCostCodes.Material;
    }

    private static void EnsureCommitments(JobRecord job)
    {
        var counter = 1;
        foreach (var commitment in job.Commitments)
        {
            commitment.CommitmentNumber = string.IsNullOrWhiteSpace(commitment.CommitmentNumber)
                ? $"{job.JobNumber}-COM-{counter:000}"
                : commitment.CommitmentNumber.Trim();
            commitment.Vendor = commitment.Vendor?.Trim() ?? string.Empty;
            commitment.Description = commitment.Description?.Trim() ?? string.Empty;
            commitment.InvoiceNumber = commitment.InvoiceNumber?.Trim() ?? string.Empty;
            commitment.Notes = commitment.Notes?.Trim() ?? string.Empty;
            commitment.BilledAmount = EstimateMath.RoundCurrency(Math.Max(0m, commitment.BilledAmount));
            commitment.PaidAmount = EstimateMath.RoundCurrency(Math.Max(0m, commitment.PaidAmount));

            if (commitment.ScheduleValueItemId is not null)
            {
                var linkedItem = job.ScheduleOfValues.FirstOrDefault(item => item.Id == commitment.ScheduleValueItemId.Value);
                if (linkedItem is null)
                {
                    commitment.ScheduleValueItemId = null;
                    commitment.CommittedAmount = 0m;
                }
                else
                {
                    commitment.CommittedAmount = EstimateMath.RoundCurrency(Math.Max(0m, linkedItem.ScheduledValue));
                    if (string.IsNullOrWhiteSpace(commitment.Description))
                    {
                        commitment.Description = linkedItem.Description;
                    }
                }
            }
            else
            {
                commitment.CommittedAmount = EstimateMath.RoundCurrency(Math.Max(0m, commitment.CommittedAmount));
            }

            commitment.CommittedAmount = EstimateMath.RoundCurrency(Math.Max(0m, commitment.CommittedAmount));
            counter++;
        }
    }

    private static void EnsureScheduleOfValues(JobRecord job)
    {
        if (job.ScheduleOfValues.Count == 0)
        {
            job.ScheduleOfValues = BuildBaseScheduleOfValues(job.Baseline);
        }

        MigrateLegacyAdminEngineeringScheduleValues(job);
        EnsureScheduleValueChildCollections(job);
        SyncBaseScheduleValues(job);
        SyncChangeOrderScheduleValues(job);
        RollUpScheduleValues(job);
    }

    private static List<ScheduleValueItem> BuildBaseScheduleOfValues(BaselineEstimate baseline)
    {
        var items = new List<ScheduleValueItem>();
        AddScheduleItem(items, JobCostCodes.Admin, JobFinancialMath.GetBaselineScheduledRevenue(baseline, JobCostCodes.Admin));
        AddScheduleItem(items, JobCostCodes.Engineering, JobFinancialMath.GetBaselineScheduledRevenue(baseline, JobCostCodes.Engineering));
        AddScheduleItem(items, JobCostCodes.Materials, JobFinancialMath.GetBaselineScheduledRevenue(baseline, JobCostCodes.Materials));
        AddScheduleItem(items, JobCostCodes.Install, JobFinancialMath.GetBaselineScheduledRevenue(baseline, JobCostCodes.Install));
        AddScheduleItem(items, JobCostCodes.Demo, JobFinancialMath.GetBaselineScheduledRevenue(baseline, JobCostCodes.Demo));
        AddScheduleItem(items, JobCostCodes.Trim, JobFinancialMath.GetBaselineScheduledRevenue(baseline, JobCostCodes.Trim));
        AddScheduleItem(items, JobCostCodes.Test, JobFinancialMath.GetBaselineScheduledRevenue(baseline, JobCostCodes.Test));
        return items;
    }

    private static void AddScheduleItem(List<ScheduleValueItem> items, string categoryCode, decimal amount)
    {
        if (amount <= 0m)
        {
            return;
        }

        items.Add(new ScheduleValueItem
        {
            Description = JobCostCodes.GetLabel(categoryCode),
            CategoryCode = categoryCode,
            ReferenceValue = EstimateMath.RoundCurrency(amount),
            ScheduledValue = EstimateMath.RoundCurrency(amount),
            IsAutoGenerated = true
        });
    }

    private static void EnsureScheduleValueChildCollections(JobRecord job)
    {
        foreach (var item in job.ScheduleOfValues)
        {
            item.SubLines ??= [];
            item.CategoryCode = JobCostCodes.Normalize(item.CategoryCode);
            item.Description = string.IsNullOrWhiteSpace(item.Description)
                ? JobCostCodes.GetLabel(item.CategoryCode)
                : item.Description.Trim();
            item.Notes = item.Notes?.Trim() ?? string.Empty;
            item.ReferenceValue = item.IsChangeOrderLine
                ? 0m
                : EstimateMath.RoundCurrency(Math.Max(0m, item.ReferenceValue > 0m ? item.ReferenceValue : item.ScheduledValue));
            item.ScheduledValue = EstimateMath.RoundCurrency(Math.Max(0m, item.ScheduledValue));
            item.PercentageOfTotal = EstimateMath.RoundCurrency(Math.Max(0m, item.PercentageOfTotal));
            item.BilledToDate = EstimateMath.RoundCurrency(Math.Max(0m, item.BilledToDate));
            item.PaidToDate = EstimateMath.RoundCurrency(Math.Max(0m, item.PaidToDate));

            var hasLegacyProgressValues = item.BilledToDate > 0m || item.PaidToDate > 0m;
            var shouldCreateEstimatedSaleSubLine = !item.IsChangeOrderLine && item.ReferenceValue > 0m;
            if (item.SubLines.Count == 0 && (hasLegacyProgressValues || shouldCreateEstimatedSaleSubLine))
            {
                item.SubLines.Add(new ScheduleValueSubLine
                {
                    Description = shouldCreateEstimatedSaleSubLine ? "Estimated Sale" : $"{item.Description} Progress",
                    LineValue = shouldCreateEstimatedSaleSubLine
                        ? EstimateMath.RoundCurrency(item.ReferenceValue)
                        : GetScheduleValueStartingValue(item),
                    BilledAmount = item.BilledToDate,
                    PaidAmount = item.PaidToDate,
                    IsAutoGenerated = shouldCreateEstimatedSaleSubLine,
                    Notes = item.Notes
                });
            }

            foreach (var subLine in item.SubLines)
            {
                NormalizeScheduleValueSubLine(subLine);
            }

            SeedFirstScheduleValueSubLine(item);
        }
    }

    private static void SyncChangeOrderScheduleValues(JobRecord job)
    {
        var activeChangeOrders = job.ChangeOrders
            .OrderBy(changeOrder => changeOrder.ApprovedOn)
            .ThenBy(changeOrder => changeOrder.Title)
            .ToList();

        foreach (var existing in job.ScheduleOfValues.Where(item => item.LinkedChangeOrderId is not null).ToList())
        {
            if (activeChangeOrders.All(changeOrder => changeOrder.Id != existing.LinkedChangeOrderId))
            {
                job.ScheduleOfValues.Remove(existing);
            }
        }

        foreach (var changeOrder in activeChangeOrders)
        {
            var item = job.ScheduleOfValues.FirstOrDefault(existing => existing.LinkedChangeOrderId == changeOrder.Id);
            if (item is null)
            {
                item = new ScheduleValueItem
                {
                    CategoryCode = JobCostCodes.ChangeOrder,
                    LinkedChangeOrderId = changeOrder.Id,
                    IsAutoGenerated = true
                };

                job.ScheduleOfValues.Add(item);
            }

            item.Description = $"CO - {changeOrder.Title}";
            item.ReferenceValue = 0m;
            item.ScheduledValue = EstimateMath.RoundCurrency(changeOrder.RevenueAmount);
        }
    }

    private static void SyncBaseScheduleValues(JobRecord job)
    {
        foreach (var categoryCode in new[]
                 {
                     JobCostCodes.Admin,
                     JobCostCodes.Engineering,
                     JobCostCodes.Materials,
                     JobCostCodes.Install,
                     JobCostCodes.Demo,
                     JobCostCodes.Trim,
                     JobCostCodes.Test
                 })
        {
            var baselineAmount = JobFinancialMath.GetBaselineScheduledRevenue(job.Baseline, categoryCode);
            var existingAutoGenerated = job.ScheduleOfValues.FirstOrDefault(item =>
                item.LinkedChangeOrderId is null &&
                item.IsAutoGenerated &&
                JobCostCodes.Normalize(item.CategoryCode) == categoryCode);

            if (baselineAmount <= 0m)
            {
                if (existingAutoGenerated is not null &&
                    existingAutoGenerated.BilledToDate <= 0m &&
                    existingAutoGenerated.PaidToDate <= 0m &&
                    existingAutoGenerated.SubLines.Count == 0)
                {
                    job.ScheduleOfValues.Remove(existingAutoGenerated);
                }

                continue;
            }

            if (existingAutoGenerated is null)
            {
                if (job.ScheduleOfValues.Any(item =>
                        item.LinkedChangeOrderId is null &&
                        !item.IsAutoGenerated &&
                        JobCostCodes.Normalize(item.CategoryCode) == categoryCode))
                {
                    continue;
                }

                existingAutoGenerated = new ScheduleValueItem
                {
                    IsAutoGenerated = true
                };

                job.ScheduleOfValues.Add(existingAutoGenerated);
            }

            existingAutoGenerated.CategoryCode = categoryCode;
            existingAutoGenerated.Description = JobCostCodes.GetLabel(categoryCode);
            existingAutoGenerated.ReferenceValue = baselineAmount;
        }
    }

    private static void MigrateLegacyAdminEngineeringScheduleValues(JobRecord job)
    {
        if (job.ScheduleOfValues.Any(item => JobCostCodes.Normalize(item.CategoryCode) == JobCostCodes.Admin) ||
            job.ScheduleOfValues.Any(item => JobCostCodes.Normalize(item.CategoryCode) == JobCostCodes.Engineering))
        {
            return;
        }

        var legacyItem = job.ScheduleOfValues.FirstOrDefault(item =>
            item.LinkedChangeOrderId is null &&
            JobCostCodes.Normalize(item.CategoryCode) == JobCostCodes.AdminEngineering);
        if (legacyItem is null)
        {
            return;
        }

        var adminReferenceAmount = JobFinancialMath.GetBaselineScheduledRevenue(job.Baseline, JobCostCodes.Admin);
        var engineeringReferenceAmount = JobFinancialMath.GetBaselineScheduledRevenue(job.Baseline, JobCostCodes.Engineering);
        var totalReferenceAmount = adminReferenceAmount + engineeringReferenceAmount;
        if (totalReferenceAmount <= 0m)
        {
            adminReferenceAmount = EstimateMath.RoundCurrency(legacyItem.ScheduledValue / 2m);
            engineeringReferenceAmount = EstimateMath.RoundCurrency(legacyItem.ScheduledValue - adminReferenceAmount);
            totalReferenceAmount = adminReferenceAmount + engineeringReferenceAmount;
        }

        var adminRatio = totalReferenceAmount <= 0m ? 0.5m : adminReferenceAmount / totalReferenceAmount;
        var adminScheduledValue = EstimateMath.RoundCurrency(legacyItem.ScheduledValue * adminRatio);
        var adminBilledToDate = EstimateMath.RoundCurrency(legacyItem.BilledToDate * adminRatio);
        var adminPaidToDate = EstimateMath.RoundCurrency(legacyItem.PaidToDate * adminRatio);

        job.ScheduleOfValues.Add(new ScheduleValueItem
        {
            Description = JobCostCodes.GetLabel(JobCostCodes.Admin),
            CategoryCode = JobCostCodes.Admin,
            ReferenceValue = adminReferenceAmount,
            ScheduledValue = adminScheduledValue,
            PercentageOfTotal = 0m,
            BilledToDate = adminBilledToDate,
            PaidToDate = adminPaidToDate,
            IsAutoGenerated = legacyItem.IsAutoGenerated,
            IsPercentageManual = true,
            Notes = legacyItem.Notes
        });

        job.ScheduleOfValues.Add(new ScheduleValueItem
        {
            Description = JobCostCodes.GetLabel(JobCostCodes.Engineering),
            CategoryCode = JobCostCodes.Engineering,
            ReferenceValue = engineeringReferenceAmount,
            ScheduledValue = EstimateMath.RoundCurrency(Math.Max(0m, legacyItem.ScheduledValue - adminScheduledValue)),
            PercentageOfTotal = 0m,
            BilledToDate = EstimateMath.RoundCurrency(Math.Max(0m, legacyItem.BilledToDate - adminBilledToDate)),
            PaidToDate = EstimateMath.RoundCurrency(Math.Max(0m, legacyItem.PaidToDate - adminPaidToDate)),
            IsAutoGenerated = legacyItem.IsAutoGenerated,
            IsPercentageManual = true,
            Notes = legacyItem.Notes
        });

        job.ScheduleOfValues.Remove(legacyItem);
    }

    private static void SyncLinkedCommitmentScheduleValueSubLines(JobRecord job)
    {
        var linkedCommitments = job.Commitments
            .Where(commitment => commitment.ScheduleValueItemId is not null)
            .ToList();
        var commitmentIds = linkedCommitments
            .Select(commitment => commitment.Id)
            .ToHashSet();
        var linkedSubLines = job.ScheduleOfValues
            .SelectMany(item => item.SubLines.Select(subLine => (Item: item, SubLine: subLine)))
            .Where(entry => entry.SubLine.LinkedCommitmentId is not null)
            .GroupBy(entry => entry.SubLine.LinkedCommitmentId!.Value)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var commitment in linkedCommitments)
        {
            var targetItem = job.ScheduleOfValues.FirstOrDefault(item => item.Id == commitment.ScheduleValueItemId!.Value);
            if (targetItem is null)
            {
                continue;
            }

            if (!linkedSubLines.TryGetValue(commitment.Id, out var linkedEntry))
            {
                var newSubLine = new ScheduleValueSubLine
                {
                    Description = BuildCommitmentSubLineDescription(commitment),
                    LineValue = targetItem.SubLines.Count == 0 ? GetScheduleValueStartingValue(targetItem) : 0m,
                    LinkedCommitmentId = commitment.Id,
                    IsAutoGenerated = true,
                    Notes = commitment.Notes
                };

                targetItem.SubLines.Add(newSubLine);
                linkedSubLines[commitment.Id] = (targetItem, newSubLine);
                continue;
            }

            if (linkedEntry.Item.Id != targetItem.Id)
            {
                linkedEntry.Item.SubLines.Remove(linkedEntry.SubLine);
                targetItem.SubLines.Add(linkedEntry.SubLine);
            }
        }

        foreach (var item in job.ScheduleOfValues)
        {
            foreach (var subLine in item.SubLines.Where(subLine => subLine.LinkedCommitmentId is not null).ToList())
            {
                if (subLine.LinkedCommitmentId is null)
                {
                    continue;
                }

                var matchingCommitment = linkedCommitments.FirstOrDefault(commitment =>
                    commitment.Id == subLine.LinkedCommitmentId.Value &&
                    commitment.ScheduleValueItemId == item.Id);
                if (matchingCommitment is not null)
                {
                    continue;
                }

                if (subLine.IsAutoGenerated &&
                    subLine.BilledAmount <= 0m &&
                    subLine.PaidAmount <= 0m &&
                    string.IsNullOrWhiteSpace(subLine.Notes))
                {
                    item.SubLines.Remove(subLine);
                }
                else
                {
                    subLine.LinkedCommitmentId = null;
                    subLine.IsAutoGenerated = false;
                }
            }
        }
    }

    internal static void RollUpScheduleValues(JobRecord job)
    {
        var totalRevenue = EstimateMath.GetJobRevenue(job);
        foreach (var item in job.ScheduleOfValues)
        {
            item.CategoryCode = JobCostCodes.Normalize(item.CategoryCode);
            item.Description = string.IsNullOrWhiteSpace(item.Description)
                ? JobCostCodes.GetLabel(item.CategoryCode)
                : item.Description.Trim();
            item.Notes = item.Notes?.Trim() ?? string.Empty;
            item.ReferenceValue = EstimateMath.RoundCurrency(Math.Max(0m, item.ReferenceValue));
            item.PercentageOfTotal = EstimateMath.RoundCurrency(Math.Max(0m, item.PercentageOfTotal));

            item.SubLines ??= [];
            foreach (var subLine in item.SubLines)
            {
                NormalizeScheduleValueSubLine(subLine);
            }

            SeedFirstScheduleValueSubLine(item);
            item.ScheduledValue = item.SubLines.Count == 0
                ? GetScheduleValueStartingValue(item)
                : EstimateMath.RoundCurrency(item.SubLines.Sum(subLine => subLine.LineValue));
            item.BilledToDate = EstimateMath.RoundCurrency(item.SubLines.Sum(subLine => subLine.BilledAmount));
            item.PaidToDate = EstimateMath.RoundCurrency(item.SubLines.Sum(subLine => subLine.PaidAmount));
            item.PercentageOfTotal = totalRevenue <= 0m
                ? 0m
                : EstimateMath.RoundCurrency((item.ScheduledValue / totalRevenue) * 100m);
            item.IsPercentageManual = false;
        }
    }

    private static string BuildCommitmentSubLineDescription(CommitmentRecord commitment)
    {
        var vendor = commitment.Vendor?.Trim() ?? string.Empty;
        var description = commitment.Description?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(vendor) && !string.IsNullOrWhiteSpace(description))
        {
            return $"{vendor} - {description}";
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        if (!string.IsNullOrWhiteSpace(vendor))
        {
            return vendor;
        }

        return commitment.CommitmentNumber?.Trim() ?? "Commitment";
    }

    private static void NormalizeScheduleValueSubLine(ScheduleValueSubLine subLine)
    {
        subLine.Description = string.IsNullOrWhiteSpace(subLine.Description)
            ? "Progress Entry"
            : subLine.Description.Trim();
        subLine.LineValue = EstimateMath.RoundCurrency(Math.Max(0m, subLine.LineValue));
        subLine.BilledAmount = EstimateMath.RoundCurrency(Math.Max(0m, subLine.BilledAmount));
        subLine.PaidAmount = EstimateMath.RoundCurrency(Math.Max(0m, subLine.PaidAmount));
        subLine.Notes = subLine.Notes?.Trim() ?? string.Empty;
    }

    private static decimal GetScheduleValueStartingValue(ScheduleValueItem item) =>
        EstimateMath.RoundCurrency(Math.Max(0m, item.ScheduledValue > 0m ? item.ScheduledValue : item.ReferenceValue));

    private static void SeedFirstScheduleValueSubLine(ScheduleValueItem item)
    {
        var startingValue = GetScheduleValueStartingValue(item);
        if (startingValue <= 0m || item.SubLines.Count == 0 || item.SubLines.Any(subLine => subLine.LineValue > 0m))
        {
            return;
        }

        item.SubLines[0].LineValue = startingValue;
    }

    private static decimal GetBidPhaseCost(BidRecord bid, Func<BidLaborDistributionLine, decimal> selector)
    {
        decimal total = 0m;
        foreach (var line in bid.LaborDistribution)
        {
            var hours = EstimateMath.RoundHours(selector(line));
            total += hours * ResolveFieldRate(bid, line.PersonnelType, line.HourType, useBilledRate: false);
        }

        return EstimateMath.RoundCurrency(total);
    }

    private static decimal GetBidPhaseSale(BidRecord bid, Func<BidLaborDistributionLine, decimal> selector)
    {
        decimal total = 0m;
        foreach (var line in bid.LaborDistribution)
        {
            var hours = EstimateMath.RoundHours(selector(line));
            total += hours * ResolveFieldRate(bid, line.PersonnelType, line.HourType, useBilledRate: true);
        }

        return EstimateMath.RoundCurrency(total);
    }

    private static decimal ResolveFieldRate(BidRecord bid, PersonnelType personnelType, HourType hourType, bool useBilledRate) =>
        (personnelType, hourType, useBilledRate) switch
        {
            (PersonnelType.Journeyman, HourType.Regular, false) => bid.JourneymanRegularDirectRate,
            (PersonnelType.Journeyman, HourType.Regular, true) => bid.JourneymanRegularBilledRate,
            (PersonnelType.Journeyman, HourType.Overnight, false) => bid.JourneymanOvernightDirectRate,
            (PersonnelType.Journeyman, HourType.Overnight, true) => bid.JourneymanOvernightBilledRate,
            (PersonnelType.Apprentice, HourType.Regular, false) => bid.ApprenticeRegularDirectRate,
            (PersonnelType.Apprentice, HourType.Regular, true) => bid.ApprenticeRegularBilledRate,
            (PersonnelType.Apprentice, HourType.Overnight, false) => bid.ApprenticeOvernightDirectRate,
            (PersonnelType.Apprentice, HourType.Overnight, true) => bid.ApprenticeOvernightBilledRate,
            _ => 0m
        };

    private static List<JobBaselineLineItem> BuildBaselineLineItems(string jobNumber, BidRecord bid)
    {
        var lineItems = new List<JobBaselineLineItem>();
        var sequenceByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var task in bid.AdministrativeTasks)
        {
            var quantity = task.PricingMode == TaskPricingMode.Hourly ? Math.Max(1m, task.EstimatedHours) : 1m;
            var unitCost = quantity <= 0m ? 0m : EstimateMath.RoundCurrency(EstimateMath.GetWorkTaskCost(task, bid.AdminDirectRate) / quantity);
            var unitSale = quantity <= 0m ? 0m : EstimateMath.RoundCurrency(EstimateMath.GetWorkTaskSale(task, bid.AdminDirectRate, bid.AdminBilledRate, bid.MarkupPercent) / quantity);
            lineItems.Add(CreateLineItem(jobNumber, sequenceByCategory, "Administrative Task", JobCostCodes.Admin, task.Title, quantity, task.PricingMode == TaskPricingMode.Hourly ? "hrs" : "lot", unitCost, unitSale, task.EstimatedHours, task.Notes));
        }

        foreach (var task in bid.EngineeringTasks)
        {
            var quantity = task.PricingMode == TaskPricingMode.Hourly ? Math.Max(1m, task.EstimatedHours) : 1m;
            var unitCost = quantity <= 0m ? 0m : EstimateMath.RoundCurrency(EstimateMath.GetWorkTaskCost(task, bid.EngineeringDirectRate) / quantity);
            var unitSale = quantity <= 0m ? 0m : EstimateMath.RoundCurrency(EstimateMath.GetWorkTaskSale(task, bid.EngineeringDirectRate, bid.EngineeringBilledRate, bid.MarkupPercent) / quantity);
            lineItems.Add(CreateLineItem(jobNumber, sequenceByCategory, "Engineering Task", JobCostCodes.Engineering, task.Title, quantity, task.PricingMode == TaskPricingMode.Hourly ? "hrs" : "lot", unitCost, unitSale, task.EstimatedHours, task.Notes));
        }

        foreach (var component in bid.Components)
        {
            var unitSale = component.UnitSale > 0m
                ? EstimateMath.RoundCurrency(component.UnitSale)
                : EstimateMath.GetDefaultSaleFromMarkup(component.UnitCost, bid.MarkupPercent);
            lineItems.Add(CreateLineItem(jobNumber, sequenceByCategory, "Component", JobCostCodes.Components, component.Name, component.Quantity, "ea", component.UnitCost, unitSale, component.TotalMinutes / 60m, component.Notes));
        }

        foreach (var demoItem in bid.DemoItems)
        {
            lineItems.Add(CreateLineItem(jobNumber, sequenceByCategory, "Demo Item", JobCostCodes.Demo, demoItem.Name, demoItem.Quantity, "ea", 0m, 0m, demoItem.TotalHours, demoItem.Notes));
        }

        foreach (var material in bid.Materials.Where(item => item.Kind == BidMaterialKind.Wire))
        {
            var unitSale = material.UnitSale > 0m
                ? EstimateMath.RoundCurrency(material.UnitSale)
                : EstimateMath.GetDefaultSaleFromMarkup(material.UnitCost, bid.MarkupPercent);
            lineItems.Add(CreateLineItem(jobNumber, sequenceByCategory, "Wire", JobCostCodes.Wire, material.Description, material.Quantity, "ea", material.UnitCost, unitSale, 0m, material.Notes));
        }

        foreach (var material in bid.Materials.Where(item => item.Kind != BidMaterialKind.Wire))
        {
            var unitSale = material.UnitSale > 0m
                ? EstimateMath.RoundCurrency(material.UnitSale)
                : EstimateMath.GetDefaultSaleFromMarkup(material.UnitCost, bid.MarkupPercent);
            lineItems.Add(CreateLineItem(jobNumber, sequenceByCategory, "Material", JobCostCodes.Material, material.Description, material.Quantity, "ea", material.UnitCost, unitSale, 0m, material.Notes));
        }

        return lineItems;
    }

    private static JobBaselineLineItem CreateLineItem(
        string jobNumber,
        Dictionary<string, int> sequenceByCategory,
        string sourceSection,
        string categoryCode,
        string description,
        decimal quantity,
        string unitLabel,
        decimal estimatedUnitCost,
        decimal estimatedUnitSale,
        decimal estimatedHours,
        string notes)
    {
        var normalizedCategory = JobCostCodes.Normalize(categoryCode);
        sequenceByCategory.TryGetValue(normalizedCategory, out var sequence);
        sequence++;
        sequenceByCategory[normalizedCategory] = sequence;

        return new JobBaselineLineItem
        {
            ReferenceNumber = BuildReferenceNumber(jobNumber, normalizedCategory, sequence),
            SourceSection = sourceSection,
            CategoryCode = normalizedCategory,
            Description = string.IsNullOrWhiteSpace(description) ? sourceSection : description.Trim(),
            Quantity = quantity <= 0m ? 1m : quantity,
            UnitLabel = string.IsNullOrWhiteSpace(unitLabel) ? "ea" : unitLabel,
            EstimatedUnitCost = EstimateMath.RoundCurrency(Math.Max(0m, estimatedUnitCost)),
            EstimatedUnitSale = EstimateMath.RoundCurrency(Math.Max(0m, estimatedUnitSale)),
            EstimatedHours = EstimateMath.RoundHours(Math.Max(0m, estimatedHours)),
            Notes = notes ?? string.Empty
        };
    }

    private static string BuildReferenceNumber(string jobNumber, string categoryCode, int sequence)
    {
        var prefix = string.IsNullOrWhiteSpace(jobNumber) ? "JOB-REF" : jobNumber.Trim();
        var suffix = JobCostCodes.Normalize(categoryCode) switch
        {
            JobCostCodes.Admin => "ADMIN",
            JobCostCodes.Engineering => "ENG",
            JobCostCodes.Components => "COMP",
            JobCostCodes.Wire => "WIRE",
            JobCostCodes.Material => "MAT",
            JobCostCodes.Demo => "DEMO",
            _ => "ITEM"
        };

        return $"{prefix}-{suffix}-{sequence:000}";
    }

    private static WorkTask CloneTask(WorkTask task) =>
        new()
        {
            Id = task.Id,
            Title = task.Title,
            PricingMode = task.PricingMode,
            EstimatedHours = task.EstimatedHours,
            CostPrice = task.CostPrice,
            SalePrice = task.SalePrice,
            Complete = task.Complete,
            Notes = task.Notes
        };

    private static BidComponent CloneComponent(BidComponent component) =>
        new()
        {
            Id = component.Id,
            Name = component.Name,
            Quantity = component.Quantity,
            LocationProfile = component.LocationProfile,
            InstallType = component.InstallType,
            MaterialCostEach = component.MaterialCostEach,
            UnitSale = component.UnitSale,
            IncludeInstall = component.IncludeInstall,
            IncludeTrim = component.IncludeTrim,
            IncludeTest = component.IncludeTest,
            InstallMinutes = component.InstallMinutes,
            DemoMinutes = component.DemoMinutes,
            TrimMinutes = component.TrimMinutes,
            TestMinutes = component.TestMinutes,
            Notes = component.Notes
        };

    private static BidDemoItem CloneDemoItem(BidDemoItem demoItem) =>
        new()
        {
            Id = demoItem.Id,
            Name = demoItem.Name,
            Quantity = demoItem.Quantity,
            LocationProfile = demoItem.LocationProfile,
            InstallType = demoItem.InstallType,
            DemoHoursEach = demoItem.DemoHoursEach,
            Notes = demoItem.Notes
        };

    private static BidMaterialItem CloneMaterial(BidMaterialItem material) =>
        new()
        {
            Id = material.Id,
            Kind = material.Kind,
            Category = material.Category,
            Description = material.Description,
            Quantity = material.Quantity,
            UnitCost = material.UnitCost,
            UnitSale = material.UnitSale,
            Notes = material.Notes
        };
}


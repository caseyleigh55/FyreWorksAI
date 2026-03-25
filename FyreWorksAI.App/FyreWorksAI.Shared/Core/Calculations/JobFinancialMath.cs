namespace FyreWorksAI.Shared.Core.Calculations;

//******************************//
//****** Job Math Rules ********//
//******************************//

public static class JobFinancialMath
{
    public static decimal GetMaterialPurchaseSubtotal(JobMaterialPurchase purchase)
    {
        var quantity = purchase.Quantity <= 0m ? 1m : purchase.Quantity;
        var unitCost = Math.Max(0m, purchase.UnitCost);
        return EstimateMath.RoundCurrency(quantity * unitCost);
    }

    public static decimal GetMaterialPurchaseTotal(JobMaterialPurchase purchase)
    {
        var subtotal = GetMaterialPurchaseSubtotal(purchase);
        var salesTax = EstimateMath.RoundCurrency(Math.Max(0m, purchase.SalesTax));
        var calculated = EstimateMath.RoundCurrency(subtotal + salesTax);
        return calculated > 0m
            ? calculated
            : EstimateMath.RoundCurrency(Math.Max(0m, purchase.ActualCost));
    }

    public static decimal GetJobActualCost(JobRecord job, string costCode) =>
        EstimateMath.RoundCurrency(job.TimeEntries
            .Where(entry => JobCostCodes.Normalize(entry.CostCode) == JobCostCodes.Normalize(costCode))
            .Sum(entry => entry.TotalCost));

    public static decimal GetJobActualHours(JobRecord job, string costCode) =>
        EstimateMath.RoundHours(job.TimeEntries
            .Where(entry => JobCostCodes.Normalize(entry.CostCode) == JobCostCodes.Normalize(costCode))
            .Sum(entry => entry.Hours));

    public static decimal GetTrackedBidDeviceActualCost(JobRecord job) =>
        EstimateMath.RoundCurrency(job.Baseline.LineItems.Sum(item => item.ActualCost));

    public static decimal GetTrackedJobDeviceActualCost(JobRecord job) =>
        EstimateMath.RoundCurrency(job.JobDevices.Sum(item => item.ActualCost));

    public static decimal GetInvoiceLinkedActualCost(JobRecord job, Guid invoiceId) =>
        EstimateMath.RoundCurrency(
            job.Baseline.LineItems
                .Where(item => item.InvoiceId == invoiceId)
                .Sum(item => item.ActualCost) +
            job.JobDevices
                .Where(item => item.InvoiceId == invoiceId)
                .Sum(item => item.ActualCost));

    public static decimal GetInvoiceAutoRemainder(JobRecord job, JobInvoiceRecord invoice) =>
        EstimateMath.RoundCurrency(invoice.InvoiceTotal - GetInvoiceLinkedActualCost(job, invoice.Id));

    public static decimal GetBaselineHours(BaselineEstimate baseline, string costCode) =>
        JobCostCodes.Normalize(costCode) switch
        {
            JobCostCodes.Admin => EstimateMath.RoundHours(baseline.EstimatedAdminHours),
            JobCostCodes.Engineering => EstimateMath.RoundHours(baseline.EstimatedEngineeringHours),
            JobCostCodes.Install => EstimateMath.RoundHours(baseline.EstimatedInstallHours),
            JobCostCodes.Demo => EstimateMath.RoundHours(baseline.EstimatedDemoHours),
            JobCostCodes.Trim => EstimateMath.RoundHours(baseline.EstimatedTrimHours),
            JobCostCodes.Test => EstimateMath.RoundHours(baseline.EstimatedTestHours),
            _ => 0m
        };

    public static decimal GetBaselineScheduledRevenue(BaselineEstimate baseline, string scheduleCode) =>
        JobCostCodes.Normalize(scheduleCode) switch
        {
            JobCostCodes.Admin => EstimateMath.RoundCurrency(baseline.EstimatedAdminSale),
            JobCostCodes.Engineering => EstimateMath.RoundCurrency(baseline.EstimatedEngineeringSale),
            JobCostCodes.AdminEngineering => EstimateMath.RoundCurrency(baseline.EstimatedAdminSale + baseline.EstimatedEngineeringSale),
            JobCostCodes.Materials => EstimateMath.RoundCurrency(baseline.EstimatedComponentSale + baseline.EstimatedWireSale + baseline.EstimatedMaterialOnlySale),
            JobCostCodes.Install => EstimateMath.RoundCurrency(baseline.EstimatedInstallSale),
            JobCostCodes.Demo => EstimateMath.RoundCurrency(baseline.EstimatedDemoSale),
            JobCostCodes.Trim => EstimateMath.RoundCurrency(baseline.EstimatedTrimSale),
            JobCostCodes.Test => EstimateMath.RoundCurrency(baseline.EstimatedTestSale),
            _ => 0m
        };

    public static decimal GetBaselineScheduledCost(BaselineEstimate baseline, string scheduleCode) =>
        JobCostCodes.Normalize(scheduleCode) switch
        {
            JobCostCodes.Admin => EstimateMath.RoundCurrency(baseline.EstimatedAdminCost),
            JobCostCodes.Engineering => EstimateMath.RoundCurrency(baseline.EstimatedEngineeringCost),
            JobCostCodes.AdminEngineering => EstimateMath.RoundCurrency(baseline.EstimatedAdminCost + baseline.EstimatedEngineeringCost),
            JobCostCodes.Materials => EstimateMath.RoundCurrency(baseline.EstimatedComponentCost + baseline.EstimatedWireCost + baseline.EstimatedMaterialOnlyCost),
            JobCostCodes.Install => EstimateMath.RoundCurrency(baseline.EstimatedInstallCost),
            JobCostCodes.Demo => EstimateMath.RoundCurrency(baseline.EstimatedDemoCost),
            JobCostCodes.Trim => EstimateMath.RoundCurrency(baseline.EstimatedTrimCost),
            JobCostCodes.Test => EstimateMath.RoundCurrency(baseline.EstimatedTestCost),
            _ => 0m
        };

    public static decimal GetLinkedCommitmentCommitted(JobRecord job, Guid scheduleValueItemId) =>
        EstimateMath.RoundCurrency(job.Commitments
            .Where(commitment => commitment.ScheduleValueItemId == scheduleValueItemId)
            .Sum(commitment => commitment.CommittedAmount));

    public static decimal GetLinkedCommitmentBilled(JobRecord job, Guid scheduleValueItemId) =>
        EstimateMath.RoundCurrency(job.Commitments
            .Where(commitment => commitment.ScheduleValueItemId == scheduleValueItemId)
            .Sum(commitment => commitment.BilledAmount));

    public static decimal GetLinkedCommitmentPaid(JobRecord job, Guid scheduleValueItemId) =>
        EstimateMath.RoundCurrency(job.Commitments
            .Where(commitment => commitment.ScheduleValueItemId == scheduleValueItemId)
            .Sum(commitment => commitment.PaidAmount));

    public static decimal GetScheduleValuePaidPercent(ScheduleValueItem item)
    {
        var scheduledValue = EstimateMath.RoundCurrency(Math.Max(0m, item.ScheduledValue));
        return scheduledValue <= 0m
            ? 0m
            : EstimateMath.RoundCurrency((EstimateMath.RoundCurrency(Math.Max(0m, item.PaidToDate)) / scheduledValue) * 100m);
    }

    public static decimal GetLinkedCommitmentPaidPercent(JobRecord job, Guid scheduleValueItemId, decimal scheduledValue)
    {
        var safeScheduledValue = EstimateMath.RoundCurrency(Math.Max(0m, scheduledValue));
        return safeScheduledValue <= 0m
            ? 0m
            : EstimateMath.RoundCurrency((GetLinkedCommitmentPaid(job, scheduleValueItemId) / safeScheduledValue) * 100m);
    }

    public static decimal GetBidAllocatedPhaseHours(BidRecord bid, Func<BidLaborDistributionLine, decimal> selector) =>
        EstimateMath.RoundHours(bid.LaborDistribution.Sum(selector));
}

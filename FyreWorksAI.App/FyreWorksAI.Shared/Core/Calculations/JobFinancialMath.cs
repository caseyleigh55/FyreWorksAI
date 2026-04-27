namespace FyreWorksAI.Shared.Core.Calculations;

//******************************//
//****** Job Math Rules ********//
//******************************//

public static class JobFinancialMath
{
    private static readonly IReadOnlyList<string> BaseScheduleReferenceCodes =
    [
        JobCostCodes.Admin,
        JobCostCodes.Engineering,
        JobCostCodes.Materials,
        JobCostCodes.Install,
        JobCostCodes.Demo,
        JobCostCodes.Trim,
        JobCostCodes.Test
    ];

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

    public static decimal GetTrackedChangeOrderDeviceActualCost(JobRecord job) =>
        EstimateMath.RoundCurrency(job.ChangeOrders.Sum(changeOrder => changeOrder.DeviceItems.Sum(item => item.ActualCost)));

    public static decimal GetChangeOrderEstimatedMaterialCost(ChangeOrderRecord changeOrder) =>
        EstimateMath.RoundCurrency(changeOrder.DeviceItems.Sum(item => item.EstimatedCost));

    public static decimal GetChangeOrderEstimatedMaterialSale(ChangeOrderRecord changeOrder) =>
        EstimateMath.RoundCurrency(changeOrder.DeviceItems.Sum(item => item.EstimatedSale));

    public static decimal GetChangeOrderActualMaterialCost(ChangeOrderRecord changeOrder) =>
        EstimateMath.RoundCurrency(changeOrder.DeviceItems.Sum(item => item.ActualCost));

    public static decimal GetChangeOrderEstimatedDirectLaborCost(ChangeOrderRecord changeOrder) =>
        EstimateMath.RoundCurrency(changeOrder.EstimatedLaborHours * changeOrder.DirectLaborRate);

    public static decimal GetChangeOrderEstimatedBilledLaborAmount(ChangeOrderRecord changeOrder) =>
        EstimateMath.RoundCurrency(changeOrder.EstimatedLaborHours * changeOrder.BilledLaborRate);

    public static decimal GetChangeOrderCalculatedSale(ChangeOrderRecord changeOrder) =>
        EstimateMath.RoundCurrency(
            Math.Max(0m, changeOrder.AdditionalEstimatedCost) +
            GetChangeOrderEstimatedBilledLaborAmount(changeOrder) +
            GetChangeOrderEstimatedMaterialSale(changeOrder));

    public static decimal GetChangeOrderActualLaborHours(JobRecord job, Guid changeOrderId) =>
        EstimateMath.RoundHours(job.TimeEntries
            .Where(entry => entry.ChangeOrderId == changeOrderId)
            .Sum(entry => entry.Hours));

    public static decimal GetChangeOrderActualLaborCost(JobRecord job, Guid changeOrderId) =>
        EstimateMath.RoundCurrency(job.TimeEntries
            .Where(entry => entry.ChangeOrderId == changeOrderId)
            .Sum(entry => entry.TotalCost));

    public static decimal GetTrackedChangeOrderActualCost(JobRecord job, Guid changeOrderId)
    {
        var changeOrder = job.ChangeOrders.FirstOrDefault(item => item.Id == changeOrderId);
        if (changeOrder is null)
        {
            return 0m;
        }

        return EstimateMath.RoundCurrency(
            GetChangeOrderActualLaborCost(job, changeOrderId) +
            GetChangeOrderActualMaterialCost(changeOrder));
    }

    public static decimal GetChangeOrderEstimatedCost(ChangeOrderRecord changeOrder) =>
        EstimateMath.RoundCurrency(
            Math.Max(0m, changeOrder.AdditionalEstimatedCost) +
            GetChangeOrderEstimatedDirectLaborCost(changeOrder) +
            GetChangeOrderEstimatedMaterialCost(changeOrder));

    public static decimal GetInvoiceLinkedActualCost(JobRecord job, Guid invoiceId) =>
        EstimateMath.RoundCurrency(
            job.Baseline.LineItems
                .Where(item => item.InvoiceId == invoiceId)
                .Sum(item => item.ActualCost) +
            job.JobDevices
                .Where(item => item.InvoiceId == invoiceId)
                .Sum(item => item.ActualCost) +
            job.ChangeOrders
                .SelectMany(changeOrder => changeOrder.DeviceItems)
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

    public static decimal GetBaselineBidSaleReference(BaselineEstimate baseline, string scheduleCode)
    {
        var rawReferences = GetRawBaseScheduleReferences(baseline);
        return JobCostCodes.Normalize(scheduleCode) switch
        {
            JobCostCodes.Admin => rawReferences[JobCostCodes.Admin],
            JobCostCodes.Engineering => rawReferences[JobCostCodes.Engineering],
            JobCostCodes.AdminEngineering => EstimateMath.RoundCurrency(rawReferences[JobCostCodes.Admin] + rawReferences[JobCostCodes.Engineering]),
            JobCostCodes.Materials => rawReferences[JobCostCodes.Materials],
            JobCostCodes.Install => rawReferences[JobCostCodes.Install],
            JobCostCodes.Demo => rawReferences[JobCostCodes.Demo],
            JobCostCodes.Trim => rawReferences[JobCostCodes.Trim],
            JobCostCodes.Test => rawReferences[JobCostCodes.Test],
            _ => 0m
        };
    }

    public static decimal GetBaselineScheduledRevenue(BaselineEstimate baseline, string scheduleCode)
    {
        var adjustedReferences = GetAdjustedBaseScheduleReferences(baseline);
        return JobCostCodes.Normalize(scheduleCode) switch
        {
            JobCostCodes.Admin => adjustedReferences[JobCostCodes.Admin],
            JobCostCodes.Engineering => adjustedReferences[JobCostCodes.Engineering],
            JobCostCodes.AdminEngineering => EstimateMath.RoundCurrency(adjustedReferences[JobCostCodes.Admin] + adjustedReferences[JobCostCodes.Engineering]),
            JobCostCodes.Materials => adjustedReferences[JobCostCodes.Materials],
            JobCostCodes.Install => adjustedReferences[JobCostCodes.Install],
            JobCostCodes.Demo => adjustedReferences[JobCostCodes.Demo],
            JobCostCodes.Trim => adjustedReferences[JobCostCodes.Trim],
            JobCostCodes.Test => adjustedReferences[JobCostCodes.Test],
            _ => 0m
        };
    }

    public static decimal GetBaselineScheduledRevenueTotal(BaselineEstimate baseline)
    {
        var rawTotal = EstimateMath.RoundCurrency(
            baseline.EstimatedAdminSale +
            baseline.EstimatedEngineeringSale +
            baseline.EstimatedComponentSale +
            baseline.EstimatedWireSale +
            baseline.EstimatedMaterialOnlySale +
            baseline.EstimatedInstallSale +
            baseline.EstimatedDemoSale +
            baseline.EstimatedTrimSale +
            baseline.EstimatedTestSale);
        return EstimateMath.RoundCurrency(baseline.OriginalRevenue > 0m ? baseline.OriginalRevenue : rawTotal);
    }

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

    private static Dictionary<string, decimal> GetRawBaseScheduleReferences(BaselineEstimate baseline) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [JobCostCodes.Admin] = EstimateMath.RoundCurrency(baseline.EstimatedAdminSale),
            [JobCostCodes.Engineering] = EstimateMath.RoundCurrency(baseline.EstimatedEngineeringSale),
            [JobCostCodes.Materials] = EstimateMath.RoundCurrency(baseline.EstimatedComponentSale + baseline.EstimatedWireSale + baseline.EstimatedMaterialOnlySale),
            [JobCostCodes.Install] = EstimateMath.RoundCurrency(baseline.EstimatedInstallSale),
            [JobCostCodes.Demo] = EstimateMath.RoundCurrency(baseline.EstimatedDemoSale),
            [JobCostCodes.Trim] = EstimateMath.RoundCurrency(baseline.EstimatedTrimSale),
            [JobCostCodes.Test] = EstimateMath.RoundCurrency(baseline.EstimatedTestSale)
        };

    private static Dictionary<string, decimal> GetAdjustedBaseScheduleReferences(BaselineEstimate baseline)
    {
        var rawReferences = GetRawBaseScheduleReferences(baseline);
        var rawTotal = EstimateMath.RoundCurrency(rawReferences.Values.Sum());
        var targetTotal = GetBaselineScheduledRevenueTotal(baseline);
        if (rawTotal <= 0m || targetTotal <= 0m || Math.Abs(rawTotal - targetTotal) < 0.01m)
        {
            return rawReferences;
        }

        var adjustedReferences = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var codesWithValue = BaseScheduleReferenceCodes
            .Where(code => rawReferences[code] > 0m)
            .ToList();
        if (codesWithValue.Count == 0)
        {
            return rawReferences;
        }

        var lastCode = codesWithValue[^1];
        var remainingRawTotal = rawTotal;
        var remainingTargetTotal = targetTotal;

        foreach (var code in BaseScheduleReferenceCodes)
        {
            var rawValue = rawReferences[code];
            if (rawValue <= 0m)
            {
                adjustedReferences[code] = 0m;
                continue;
            }

            decimal adjustedValue;
            if (code == lastCode)
            {
                adjustedValue = EstimateMath.RoundCurrency(Math.Max(0m, remainingTargetTotal));
            }
            else
            {
                adjustedValue = remainingRawTotal <= 0m
                    ? 0m
                    : EstimateMath.RoundCurrency((rawValue / remainingRawTotal) * remainingTargetTotal);
                adjustedValue = EstimateMath.RoundCurrency(Math.Min(Math.Max(0m, adjustedValue), remainingTargetTotal));
            }

            adjustedReferences[code] = adjustedValue;
            remainingRawTotal = EstimateMath.RoundCurrency(Math.Max(0m, remainingRawTotal - rawValue));
            remainingTargetTotal = EstimateMath.RoundCurrency(Math.Max(0m, remainingTargetTotal - adjustedValue));
        }

        return adjustedReferences;
    }
}

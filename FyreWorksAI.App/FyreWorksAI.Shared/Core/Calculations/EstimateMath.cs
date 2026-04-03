using System.Globalization;

namespace FyreWorksAI.Shared.Core.Calculations;

//******************************//
//******** Estimates ***********//
//******************************//

public static class EstimateMath
{
    public static decimal RoundCurrency(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal RoundHours(decimal value) =>
        Math.Round(value * 4m, 0, MidpointRounding.AwayFromZero) / 4m;

    public static decimal GetBidInstallHours(BidRecord bid) =>
        RoundHours(bid.Components.Sum(component => component.InstallHours));

    public static decimal GetBidDemoHours(BidRecord bid) =>
        RoundHours(bid.DemoItems.Sum(item => item.TotalHours));

    public static decimal GetBidTrimHours(BidRecord bid) =>
        RoundHours(bid.Components.Sum(component => component.TrimHours));

    public static decimal GetBidTestHours(BidRecord bid) =>
        RoundHours(bid.Components.Sum(component => component.TestHours));

    public static decimal GetBidCalculatedFieldHours(BidRecord bid) =>
        RoundHours(GetBidInstallHours(bid) + GetBidDemoHours(bid) + GetBidTrimHours(bid) + GetBidTestHours(bid));

    public static decimal GetBidAllocatedFieldHours(BidRecord bid) =>
        RoundHours(bid.LaborDistribution.Sum(line => line.TotalHours));

    public static decimal GetBidAllocatedHours(BidRecord bid, PersonnelType personnelType, HourType hourType) =>
        RoundHours(bid.LaborDistribution
            .Where(line => line.PersonnelType == personnelType && line.HourType == hourType)
            .Sum(line => line.TotalHours));

    public static decimal GetBidAdminHours(BidRecord bid) =>
        RoundHours(bid.AdministrativeTasks.Where(task => task.PricingMode == TaskPricingMode.Hourly).Sum(task => task.EstimatedHours));

    public static decimal GetBidEngineeringHours(BidRecord bid) =>
        RoundHours(bid.EngineeringTasks.Where(task => task.PricingMode == TaskPricingMode.Hourly).Sum(task => task.EstimatedHours));

    public static decimal GetDefaultSaleFromMarkup(decimal cost, decimal markupPercent) =>
        RoundCurrency(cost * (1m + (Math.Max(0m, markupPercent) / 100m)));

    public static decimal GetWorkTaskCost(WorkTask task, decimal directRate) =>
        task.PricingMode == TaskPricingMode.Hourly
            ? RoundCurrency(RoundHours(task.EstimatedHours) * directRate)
            : RoundCurrency(task.CostPrice);

    public static decimal GetWorkTaskSale(WorkTask task, decimal directRate, decimal billedRate, decimal markupPercent)
    {
        if (task.PricingMode == TaskPricingMode.Hourly)
        {
            return RoundCurrency(RoundHours(task.EstimatedHours) * billedRate);
        }

        return RoundCurrency(task.SalePrice > 0m
            ? task.SalePrice
            : GetDefaultSaleFromMarkup(task.CostPrice, markupPercent));
    }

    public static decimal GetBidAdministrativeTaskCost(BidRecord bid) =>
        RoundCurrency(bid.AdministrativeTasks.Sum(task => GetWorkTaskCost(task, bid.AdminDirectRate)));

    public static decimal GetBidAdministrativeTaskSale(BidRecord bid) =>
        RoundCurrency(bid.AdministrativeTasks.Sum(task => GetWorkTaskSale(task, bid.AdminDirectRate, bid.AdminBilledRate, bid.MarkupPercent)));

    public static decimal GetBidEngineeringTaskCost(BidRecord bid) =>
        RoundCurrency(bid.EngineeringTasks.Sum(task => GetWorkTaskCost(task, bid.EngineeringDirectRate)));

    public static decimal GetBidEngineeringTaskSale(BidRecord bid) =>
        RoundCurrency(bid.EngineeringTasks.Sum(task => GetWorkTaskSale(task, bid.EngineeringDirectRate, bid.EngineeringBilledRate, bid.MarkupPercent)));

    public static decimal GetBidFieldLaborCost(BidRecord bid) =>
        RoundCurrency(bid.LaborDistribution.Sum(line => GetDistributionCost(bid, line)));

    public static decimal GetBidFieldLaborSale(BidRecord bid) =>
        RoundCurrency(bid.LaborDistribution.Sum(line => GetDistributionSale(bid, line)));

    public static decimal GetBidComponentCost(BidRecord bid) =>
        RoundCurrency(bid.Components.Sum(component => component.TotalMaterialCost));

    public static decimal GetBidComponentSale(BidRecord bid) =>
        RoundCurrency(bid.Components.Sum(component => component.UnitSale > 0m ? component.TotalMaterialSale : GetDefaultSaleFromMarkup(component.TotalMaterialCost, bid.MarkupPercent)));

    public static decimal GetBidWireCost(BidRecord bid) =>
        RoundCurrency(bid.Materials.Where(item => item.Kind == BidMaterialKind.Wire).Sum(item => item.ExtendedCost));

    public static decimal GetBidWireSale(BidRecord bid) =>
        RoundCurrency(bid.Materials.Where(item => item.Kind == BidMaterialKind.Wire).Sum(item => item.UnitSale > 0m ? item.ExtendedSale : GetDefaultSaleFromMarkup(item.ExtendedCost, bid.MarkupPercent)));

    public static decimal GetBidMaterialOnlyCost(BidRecord bid) =>
        RoundCurrency(bid.Materials.Where(item => item.Kind == BidMaterialKind.Material).Sum(item => item.ExtendedCost));

    public static decimal GetBidMaterialOnlySale(BidRecord bid) =>
        RoundCurrency(bid.Materials.Where(item => item.Kind == BidMaterialKind.Material).Sum(item => item.UnitSale > 0m ? item.ExtendedSale : GetDefaultSaleFromMarkup(item.ExtendedCost, bid.MarkupPercent)));

    public static decimal GetBidLaborCost(BidRecord bid) =>
        RoundCurrency(GetBidFieldLaborCost(bid) + GetBidAdministrativeTaskCost(bid) + GetBidEngineeringTaskCost(bid));

    public static decimal GetBidLaborSale(BidRecord bid) =>
        RoundCurrency(GetBidFieldLaborSale(bid) + GetBidAdministrativeTaskSale(bid) + GetBidEngineeringTaskSale(bid));

    public static decimal GetBidMaterialCost(BidRecord bid) =>
        RoundCurrency(GetBidComponentCost(bid) + GetBidWireCost(bid) + GetBidMaterialOnlyCost(bid));

    public static decimal GetBidMaterialSale(BidRecord bid) =>
        RoundCurrency(GetBidComponentSale(bid) + GetBidWireSale(bid) + GetBidMaterialOnlySale(bid));

    public static decimal GetBidEstimatedCost(BidRecord bid) =>
        RoundCurrency(GetBidLaborCost(bid) + GetBidMaterialCost(bid));

    public static decimal GetBidCalculatedSale(BidRecord bid) =>
        RoundCurrency(GetBidLaborSale(bid) + GetBidMaterialSale(bid));

    public static decimal GetBidAdjustedRevenue(BidRecord bid) =>
        RoundCurrency(bid.AcceptedSalePrice > 0m ? bid.AcceptedSalePrice : GetBidCalculatedSale(bid));

    public static decimal GetBidCalculatedProfit(BidRecord bid) =>
        RoundCurrency(GetBidCalculatedSale(bid) - GetBidEstimatedCost(bid));

    public static decimal GetBidCalculatedMarginPercent(BidRecord bid)
    {
        var sale = GetBidCalculatedSale(bid);
        return sale <= 0m ? 0m : GetBidCalculatedProfit(bid) / sale;
    }

    public static decimal GetBidAdjustedProfit(BidRecord bid) =>
        RoundCurrency(GetBidAdjustedRevenue(bid) - GetBidEstimatedCost(bid));

    public static decimal GetBidAdjustedMarginPercent(BidRecord bid)
    {
        var sale = GetBidAdjustedRevenue(bid);
        return sale <= 0m ? 0m : GetBidAdjustedProfit(bid) / sale;
    }

    public static decimal GetBidRevenue(BidRecord bid) =>
        GetBidAdjustedRevenue(bid);

    public static decimal GetBidMargin(BidRecord bid) =>
        GetBidAdjustedProfit(bid);

    public static decimal GetBidMarginPercent(BidRecord bid) =>
        GetBidAdjustedMarginPercent(bid);

    public static decimal GetJobActualLaborHours(JobRecord job) =>
        job.TimeEntries.Sum(entry => entry.Hours);

    public static decimal GetJobActualLaborCost(JobRecord job) =>
        RoundCurrency(job.TimeEntries.Sum(entry => entry.TotalCost));

    public static decimal GetJobActualMaterialCost(JobRecord job) =>
        RoundCurrency(
            JobFinancialMath.GetTrackedBidDeviceActualCost(job) +
            JobFinancialMath.GetTrackedJobDeviceActualCost(job) +
            JobFinancialMath.GetTrackedChangeOrderDeviceActualCost(job) +
            job.MaterialPurchases.Sum(JobFinancialMath.GetMaterialPurchaseTotal));

    public static decimal GetJobEstimatedCost(JobRecord job) =>
        RoundCurrency(job.Baseline.EstimatedTotalCost + GetJobApprovedChangeOrderCost(job));

    public static decimal GetJobApprovedChangeOrderRevenue(JobRecord job) =>
        RoundCurrency(job.ChangeOrders.Where(changeOrder => changeOrder.Approved).Sum(changeOrder => changeOrder.RevenueAmount));

    public static decimal GetJobApprovedChangeOrderCost(JobRecord job) =>
        RoundCurrency(job.ChangeOrders.Where(changeOrder => changeOrder.Approved).Sum(changeOrder => changeOrder.EstimatedCostImpact));

    public static decimal GetJobRevenue(JobRecord job) =>
        RoundCurrency(job.Baseline.OriginalRevenue + GetJobApprovedChangeOrderRevenue(job));

    public static decimal GetJobBilledCommitments(JobRecord job) =>
        RoundCurrency(job.Commitments.Sum(commitment => commitment.BilledAmount));

    public static decimal GetJobCommittedExposure(JobRecord job) =>
        RoundCurrency(
            GetJobActualLaborCost(job) +
            GetJobActualMaterialCost(job) +
            GetJobApprovedChangeOrderRemainingCost(job) +
            job.Commitments.Sum(commitment => Math.Max(commitment.CommittedAmount, commitment.BilledAmount)));

    public static decimal GetJobActualCost(JobRecord job) =>
        RoundCurrency(
            GetJobActualLaborCost(job) +
            GetJobActualMaterialCost(job) +
            GetJobBilledCommitments(job));

    public static decimal GetJobProfit(JobRecord job) =>
        RoundCurrency(GetJobRevenue(job) - GetJobActualCost(job));

    public static decimal GetJobMarginPercent(JobRecord job)
    {
        var revenue = GetJobRevenue(job);
        return revenue <= 0m ? 0m : GetJobProfit(job) / revenue;
    }

    public static decimal GetJobApprovedChangeOrderRemainingCost(JobRecord job) =>
        RoundCurrency(job.ChangeOrders
            .Where(changeOrder => changeOrder.Approved)
            .Sum(changeOrder => Math.Max(
                0m,
                changeOrder.EstimatedCostImpact - JobFinancialMath.GetTrackedChangeOrderActualCost(job, changeOrder.Id))));

    public static decimal GetJobBilledRevenue(JobRecord job) =>
        RoundCurrency(job.ScheduleOfValues.Sum(item => item.BilledToDate));

    public static decimal GetJobCollectedRevenue(JobRecord job) =>
        RoundCurrency(job.ScheduleOfValues.Sum(item => item.PaidToDate));

    public static decimal GetServiceContractValue(ServiceAgreement agreement) =>
        agreement.MonthlyMonitoringAmount * agreement.ContractMonths;

    public static decimal GetServicePaid(ServiceAgreement agreement) =>
        agreement.MonitoringPayments.Where(payment => payment.IsPaid).Sum(payment => payment.Amount);

    public static decimal GetServiceOutstanding(ServiceAgreement agreement) =>
        GetServiceContractValue(agreement) - GetServicePaid(agreement);

    public static DateTime GetNextBillingDate(ServiceAgreement agreement)
    {
        var nextPayment = agreement.MonitoringPayments
            .Where(payment => !payment.IsPaid)
            .OrderBy(payment => payment.DueDate)
            .FirstOrDefault();

        return nextPayment?.DueDate ?? agreement.ContractStart.AddMonths(agreement.ContractMonths);
    }

    public static decimal GetServiceQuoteRevenue(ServiceQuoteRecord quote) =>
        RoundCurrency(quote.Items.Sum(item => item.TotalPrice));

    public static decimal GetServiceQuoteCost(ServiceQuoteRecord quote) =>
        RoundCurrency(quote.Items.Sum(item => item.TotalCost));

    public static string GetCurrency(decimal value) =>
        RoundCurrency(value).ToString("C2", CultureInfo.CurrentCulture);

    public static string GetHours(decimal value) =>
        RoundHours(value).ToString("N2", CultureInfo.CurrentCulture);

    public static string GetPercent(decimal value) =>
        value.ToString("P1", CultureInfo.CurrentCulture);

    private static decimal GetDistributionCost(BidRecord bid, BidLaborDistributionLine line)
    {
        var directRate = ResolveFieldRate(bid, line.PersonnelType, line.HourType, useBilledRate: false);
        return RoundCurrency(RoundHours(line.TotalHours) * directRate);
    }

    private static decimal GetDistributionSale(BidRecord bid, BidLaborDistributionLine line)
    {
        var billedRate = ResolveFieldRate(bid, line.PersonnelType, line.HourType, useBilledRate: true);
        return RoundCurrency(RoundHours(line.TotalHours) * billedRate);
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
}

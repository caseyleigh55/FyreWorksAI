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
            job.MaterialPurchases.Sum(JobFinancialMath.GetMaterialPurchaseTotal) +
            JobFinancialMath.GetInvoiceAutoRemainderTotal(job));

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

    public static decimal GetJobActualCost(JobRecord job) =>
        RoundCurrency(
            GetJobActualLaborCost(job) +
            GetJobActualMaterialCost(job) +
            GetJobBilledCommitments(job));

    public static decimal GetJobCostVariance(JobRecord job) =>
        RoundCurrency(GetJobActualCost(job) - GetJobEstimatedCost(job));

    public static decimal GetJobProfit(JobRecord job) =>
        RoundCurrency(GetJobRevenue(job) - GetJobActualCost(job));

    public static decimal GetJobMarginPercent(JobRecord job)
    {
        var revenue = GetJobRevenue(job);
        return revenue <= 0m ? 0m : GetJobProfit(job) / revenue;
    }

    public static decimal GetJobBilledRevenue(JobRecord job) =>
        RoundCurrency(job.ScheduleOfValues.Sum(item => item.BilledToDate));

    public static decimal GetJobCollectedRevenue(JobRecord job) =>
        RoundCurrency(job.ScheduleOfValues.Sum(item => item.PaidToDate));

    public static decimal GetServiceContractValue(ServiceAgreement agreement) =>
        RoundCurrency(agreement.MonitoringPayments.Sum(payment => payment.Amount));

    public static decimal GetMonitoringPaymentBilledAmount(MonitoringPayment payment)
    {
        var billedAmount = payment.AmountBilled > 0m
            ? payment.AmountBilled
            : payment.IsPaid
                ? payment.Amount
                : 0m;

        return RoundCurrency(Math.Max(0m, billedAmount));
    }

    public static decimal GetMonitoringPaymentReceivedAmount(MonitoringPayment payment)
    {
        var receivedAmount = payment.ReceivedAmount > 0m
            ? payment.ReceivedAmount
            : payment.IsPaid
                ? payment.Amount
                : 0m;

        return RoundCurrency(Math.Max(0m, receivedAmount));
    }

    public static bool IsMonitoringPaymentSettled(MonitoringPayment payment)
    {
        if (payment.IsPaid)
        {
            return true;
        }

        var receivedAmount = GetMonitoringPaymentReceivedAmount(payment);
        return payment.Amount <= 0m
            ? receivedAmount > 0m
            : receivedAmount >= RoundCurrency(payment.Amount);
    }

    public static decimal GetServiceBilled(ServiceAgreement agreement) =>
        RoundCurrency(agreement.MonitoringPayments.Sum(GetMonitoringPaymentBilledAmount));

    public static decimal GetServicePaid(ServiceAgreement agreement) =>
        RoundCurrency(agreement.MonitoringPayments.Sum(GetMonitoringPaymentReceivedAmount));

    public static decimal GetServiceOutstanding(ServiceAgreement agreement) =>
        RoundCurrency(Math.Max(0m, GetServiceContractValue(agreement) - GetServicePaid(agreement)));

    public static int GetServicePaidInstallmentCount(ServiceAgreement agreement) =>
        agreement.MonitoringPayments.Count(IsMonitoringPaymentSettled);

    public static MonitoringPayment? GetCurrentServiceInstallment(ServiceAgreement agreement) =>
        agreement.MonitoringPayments
            .OrderBy(payment => payment.DueDate)
            .FirstOrDefault(payment => !IsMonitoringPaymentSettled(payment));

    public static int GetCurrentServiceInstallmentNumber(ServiceAgreement agreement)
    {
        var orderedPayments = agreement.MonitoringPayments
            .OrderBy(payment => payment.DueDate)
            .ToList();
        var currentInstallment = orderedPayments.FindIndex(payment => !IsMonitoringPaymentSettled(payment));
        return currentInstallment < 0 ? orderedPayments.Count : currentInstallment + 1;
    }

    public static decimal GetServiceReceivedPercent(ServiceAgreement agreement)
    {
        var contractValue = GetServiceContractValue(agreement);
        return contractValue <= 0m
            ? 0m
            : RoundCurrency((GetServicePaid(agreement) / contractValue) * 100m);
    }

    public static DateTime GetNextBillingDate(ServiceAgreement agreement)
    {
        var nextPayment = GetCurrentServiceInstallment(agreement);

        return nextPayment?.DueDate ?? agreement.ContractStart.AddMonths(agreement.ContractMonths);
    }

    public static decimal GetServiceCallLaborHours(ServiceCallRecord serviceCall) =>
        RoundHours(serviceCall.Billing.LaborHours);

    public static decimal GetServiceCallLaborAmount(ServiceCallRecord serviceCall) =>
        RoundCurrency(serviceCall.Billing.LaborAmount);

    public static decimal GetServiceCallMaterialAmount(ServiceCallRecord serviceCall) =>
        RoundCurrency(serviceCall.Billing.MaterialAmount);

    public static decimal GetServiceCallInvoiceAmount(ServiceCallRecord serviceCall) =>
        RoundCurrency(serviceCall.Billing.InvoiceAmount > 0m
            ? serviceCall.Billing.InvoiceAmount
            : serviceCall.Billing.LaborAmount + serviceCall.Billing.MaterialAmount);

    public static decimal GetServiceCallBilledAmount(ServiceCallRecord serviceCall) =>
        RoundCurrency(serviceCall.Billing.BilledAmount);

    public static decimal GetServiceCallPaidAmount(ServiceCallRecord serviceCall) =>
        RoundCurrency(serviceCall.Billing.PaidAmount);

    public static decimal GetServiceCallOutstandingAmount(ServiceCallRecord serviceCall) =>
        RoundCurrency(Math.Max(0m, GetServiceCallBilledAmount(serviceCall) - GetServiceCallPaidAmount(serviceCall)));

    public static decimal GetServiceQuoteLaborHours(ServiceQuoteRecord quote) =>
        quote.LaborLines.Count > 0
            ? RoundHours(quote.LaborLines.Sum(line => line.Hours))
            : RoundHours(quote.ServiceLaborHours);

    public static decimal GetServiceQuoteLaborCost(ServiceQuoteRecord quote) =>
        quote.LaborLines.Count > 0
            ? RoundCurrency(quote.LaborLines.Sum(line => line.TotalCost))
            : RoundCurrency(GetServiceQuoteLaborHours(quote) * quote.ServiceLaborCostRate);

    public static decimal GetServiceQuoteLaborRevenue(ServiceQuoteRecord quote) =>
        quote.LaborLines.Count > 0
            ? RoundCurrency(quote.LaborLines.Sum(line => line.TotalSale))
            : RoundCurrency(GetServiceQuoteLaborHours(quote) * quote.ServiceLaborSaleRate);

    public static decimal GetServiceQuoteMaterialRevenue(ServiceQuoteRecord quote) =>
        RoundCurrency(quote.Items.Sum(item => item.TotalPrice));

    public static decimal GetServiceQuoteMaterialCost(ServiceQuoteRecord quote) =>
        RoundCurrency(quote.Items.Sum(item => item.TotalCost));

    public static decimal GetServiceQuoteCalculatedRevenue(ServiceQuoteRecord quote) =>
        RoundCurrency(GetServiceQuoteLaborRevenue(quote) + GetServiceQuoteMaterialRevenue(quote));

    public static decimal GetServiceQuoteAdjustedRevenue(ServiceQuoteRecord quote) =>
        RoundCurrency(quote.AdjustedSalePrice > 0m ? quote.AdjustedSalePrice : GetServiceQuoteCalculatedRevenue(quote));

    public static decimal GetServiceQuoteCost(ServiceQuoteRecord quote) =>
        RoundCurrency(GetServiceQuoteLaborCost(quote) + GetServiceQuoteMaterialCost(quote));

    public static decimal GetServiceQuoteCalculatedProfit(ServiceQuoteRecord quote) =>
        RoundCurrency(GetServiceQuoteCalculatedRevenue(quote) - GetServiceQuoteCost(quote));

    public static decimal GetServiceQuoteAdjustedProfit(ServiceQuoteRecord quote) =>
        RoundCurrency(GetServiceQuoteAdjustedRevenue(quote) - GetServiceQuoteCost(quote));

    public static decimal GetServiceQuoteCalculatedMarginPercent(ServiceQuoteRecord quote)
    {
        var revenue = GetServiceQuoteCalculatedRevenue(quote);
        return revenue <= 0m ? 0m : GetServiceQuoteCalculatedProfit(quote) / revenue;
    }

    public static decimal GetServiceQuoteAdjustedMarginPercent(ServiceQuoteRecord quote)
    {
        var revenue = GetServiceQuoteAdjustedRevenue(quote);
        return revenue <= 0m ? 0m : GetServiceQuoteAdjustedProfit(quote) / revenue;
    }

    public static decimal GetServiceQuoteRevenue(ServiceQuoteRecord quote) =>
        GetServiceQuoteAdjustedRevenue(quote);

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

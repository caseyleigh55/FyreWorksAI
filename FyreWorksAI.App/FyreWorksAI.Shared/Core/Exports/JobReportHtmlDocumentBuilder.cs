using System.Net;
using System.Text;

namespace FyreWorksAI.Shared.Core.Exports;

//******************************//
//***** Job Report Document ****//
//******************************//
internal static class JobReportHtmlDocumentBuilder
{
    //******************************//
    //******** HTML Export *********//
    //******************************//

    public static string BuildDocument(JobRecord job, ClientRecord? client, DocumentBrandingProfile brandingProfile)
    {
        var documentTitle = string.IsNullOrWhiteSpace(job.ProjectName)
            ? $"Job Operations Report - {GetDisplayJobNumber(job)}"
            : $"{job.ProjectName.Trim()} - Job Operations Report";
        var footerReference = $"{GetDisplayJobNumber(job)} | {GetDisplayProjectName(job)}";
        var notesPageMarkup = BuildNotesPageMarkup(job, footerReference);
        var changeOrdersPageMarkup = BuildChangeOrdersPageMarkup(job, footerReference);

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>{{Encode(documentTitle)}}</title>
    <style>
        {{BuildStyles()}}
    </style>
</head>
<body>
    <main class="report-document">
        {{BuildOverviewPageMarkup(job, client, brandingProfile, footerReference)}}
        {{BuildSectionPageMarkup(
            "financial-overview",
            "Financial Summary",
            "Profit And Loss Statement",
            "Bird's-eye view of contract revenue, estimated cost, actual cost, and exposure.",
            BuildProfitAndLossMarkup(job),
            footerReference)}}
        {{BuildSectionPageMarkup(
            "schedule-of-values",
            "Billing Snapshot",
            "Schedule Of Values",
            "Contract allocation, billing, and payment progress rolled into one section.",
            BuildScheduleOfValuesMarkup(job),
            footerReference)}}
        {{BuildSectionPageMarkup(
            "hours",
            "Labor Snapshot",
            "Hours And Labor Tracking",
            "Planned versus used hours across baseline scope and change order work.",
            BuildHoursMarkup(job),
            footerReference)}}
        {{BuildSectionPageMarkup(
            "comparison",
            "Scope Comparison",
            "Bid To Job Comparison",
            "Original bid scope, field substitutions, added job scope, and change order material detail.",
            BuildComparisonMarkup(job),
            footerReference)}}
        {{BuildSectionPageMarkup(
            "materials-and-invoices",
            "Purchase Register",
            "Material And Invoice Tracking",
            "Tracked material lines cross-referenced to invoice records and invoice-level rollups.",
            BuildMaterialAndInvoiceMarkup(job),
            footerReference)}}
        {{BuildSectionPageMarkup(
            "commitments",
            "Commitment Snapshot",
            "Commitments",
            "Vendor commitments, linked SOV references, billing progress, and payment status.",
            BuildCommitmentsMarkup(job),
            footerReference)}}
        {{changeOrdersPageMarkup}}
        {{notesPageMarkup}}
    </main>
</body>
</html>
""";
    }

    //******************************//
    //******** Page Layout *********//
    //******************************//

    private static string BuildOverviewPageMarkup(
        JobRecord job,
        ClientRecord? client,
        DocumentBrandingProfile brandingProfile,
        string footerReference)
    {
        var projectSummaryMarkup = BuildProjectSummaryMarkup(job, client);
        var scopeSnapshotMarkup = BuildOptionalNarrativeCardMarkup(
            "Scope Snapshot",
            !string.IsNullOrWhiteSpace(job.ProposalSummary)
                ? job.ProposalSummary
                : !string.IsNullOrWhiteSpace(job.Site.ScopeOfWork)
                    ? job.Site.ScopeOfWork
                    : job.Baseline.ScopeSummary);
        var notesSnapshotMarkup = BuildOptionalNarrativeCardMarkup("Project Notes", job.Notes);

        return $$"""
<section id="overview" class="report-page report-cover-page">
    <div class="report-page-shell">
        {{BuildBrandingMarkup(brandingProfile)}}
        <header class="report-cover-header">
            <div>
                <p class="report-eyebrow">Job Operations Report</p>
                <h1 class="report-cover-title">{{Encode(GetDisplayProjectName(job))}}</h1>
                <p class="report-reference">{{Encode(GetDisplayJobNumber(job))}} | {{Encode(GetDisplayStatus(job.Status))}} | Generated {{Encode(DateTime.Now.ToString("f"))}}</p>
            </div>
            <div class="report-emphasis-card">
                <span class="report-emphasis-label">Contract Revenue</span>
                <strong class="report-emphasis-value">{{EstimateMath.GetCurrency(EstimateMath.GetJobRevenue(job))}}</strong>
                <span class="report-emphasis-detail">Includes approved change orders</span>
            </div>
        </header>

        {{BuildMetricGridMarkup(
            [
                ("Estimated Cost", EstimateMath.GetCurrency(EstimateMath.GetJobEstimatedCost(job)), "Baseline plus approved CO estimated cost"),
                ("Actual Cost", EstimateMath.GetCurrency(EstimateMath.GetJobActualCost(job)), "Labor, material, and billed commitments to date"),
                ("Cost Variance", EstimateMath.GetCurrency(EstimateMath.GetJobCostVariance(job)), "Actual cost minus estimated cost. Negative is under budget"),
                ("Profit To Date", EstimateMath.GetCurrency(EstimateMath.GetJobProfit(job)), "Revenue minus actual cost"),
                ("Margin", EstimateMath.GetPercent(EstimateMath.GetJobMarginPercent(job)), "Current gross margin"),
                ("Actual Labor Hours", EstimateMath.GetHours(EstimateMath.GetJobActualLaborHours(job)), "Tracked time entries on the job")
            ])}}

        <div class="info-grid info-grid-overview">
            {{BuildInfoCardMarkup("Project Overview", projectSummaryMarkup)}}
            {{BuildInfoCardMarkup("How To Read This Report", BuildTableOfContentsMarkup(job))}}
            {{scopeSnapshotMarkup}}
            {{notesSnapshotMarkup}}
        </div>

        <footer class="report-footer">{{Encode(footerReference)}}</footer>
    </div>
</section>
""";
    }

    private static string BuildSectionPageMarkup(
        string sectionId,
        string eyebrow,
        string title,
        string reference,
        string bodyMarkup,
        string footerReference)
    {
        if (string.IsNullOrWhiteSpace(bodyMarkup))
        {
            return string.Empty;
        }

        return $$"""
<section id="{{sectionId}}" class="report-page report-page-break">
    <div class="report-page-shell">
        <header class="report-page-header">
            <div>
                <p class="report-eyebrow">{{Encode(eyebrow)}}</p>
                <h2 class="report-page-title">{{Encode(title)}}</h2>
                <p class="report-reference">{{Encode(reference)}}</p>
            </div>
        </header>

        {{bodyMarkup}}

        <footer class="report-footer">{{Encode(footerReference)}}</footer>
    </div>
</section>
""";
    }

    //******************************//
    //******* Page Content *********//
    //******************************//

    private static string BuildProfitAndLossMarkup(JobRecord job)
    {
        var baselineRevenue = EstimateMath.RoundCurrency(job.Baseline.OriginalRevenue);
        var approvedChangeOrderRevenue = EstimateMath.GetJobApprovedChangeOrderRevenue(job);
        var totalRevenue = EstimateMath.GetJobRevenue(job);
        var baselineEstimatedCost = EstimateMath.RoundCurrency(job.Baseline.EstimatedTotalCost);
        var approvedChangeOrderEstimatedCost = EstimateMath.GetJobApprovedChangeOrderCost(job);
        var totalEstimatedCost = EstimateMath.GetJobEstimatedCost(job);
        var actualLaborCost = EstimateMath.GetJobActualLaborCost(job);
        var actualMaterialCost = EstimateMath.GetJobActualMaterialCost(job);
        var commitmentBillings = EstimateMath.GetJobBilledCommitments(job);
        var totalActualCost = EstimateMath.GetJobActualCost(job);
        var costVariance = EstimateMath.GetJobCostVariance(job);
        var profitToDate = EstimateMath.GetJobProfit(job);
        var marginToDate = EstimateMath.GetJobMarginPercent(job);

        var builder = new StringBuilder();
        builder.Append(BuildMetricGridMarkup(
            [
                ("Revenue", EstimateMath.GetCurrency(totalRevenue), "Base contract plus approved change orders"),
                ("Estimated Cost", EstimateMath.GetCurrency(totalEstimatedCost), "Budgeted job cost for current approved scope"),
                ("Actual Cost", EstimateMath.GetCurrency(totalActualCost), "Tracked actual job cost to date"),
                ("Cost Variance", EstimateMath.GetCurrency(costVariance), "Actual cost minus estimated cost. Negative is under budget"),
                ("Profit", EstimateMath.GetCurrency(profitToDate), "Revenue less actual cost"),
                ("Margin", EstimateMath.GetPercent(marginToDate), "Profit divided by revenue")
            ]));

        builder.AppendLine("""
<section class="report-section">
    <h3 class="report-section-title">Statement</h3>
    <table class="report-table">
        <thead>
            <tr>
                <th>Line Item</th>
                <th class="align-right">Amount</th>
                <th>Context</th>
            </tr>
        </thead>
        <tbody>
""");

        builder.AppendLine(BuildStatementRowMarkup("Base Contract Revenue", EstimateMath.GetCurrency(baselineRevenue), "Accepted bid value carried into the job baseline."));
        builder.AppendLine(BuildStatementRowMarkup("Approved Change Order Revenue", EstimateMath.GetCurrency(approvedChangeOrderRevenue), "Only approved change orders are included in contract revenue."));
        builder.AppendLine(BuildStatementRowMarkup("Total Revenue", EstimateMath.GetCurrency(totalRevenue), "Current contract value for the project.", isEmphasized: true));
        builder.AppendLine(BuildStatementRowMarkup("Base Estimated Cost", EstimateMath.GetCurrency(baselineEstimatedCost), "Original baseline estimated job cost."));
        builder.AppendLine(BuildStatementRowMarkup("Approved Change Order Estimated Cost", EstimateMath.GetCurrency(approvedChangeOrderEstimatedCost), "Estimated cost impact of approved change orders."));
        builder.AppendLine(BuildStatementRowMarkup("Total Estimated Cost", EstimateMath.GetCurrency(totalEstimatedCost), "Current approved-scope budget.", isEmphasized: true));
        builder.AppendLine(BuildStatementRowMarkup("Actual Labor Cost", EstimateMath.GetCurrency(actualLaborCost), "Tracked from job time entries."));
        builder.AppendLine(BuildStatementRowMarkup("Actual Material And Purchase Cost", EstimateMath.GetCurrency(actualMaterialCost), "Tracked base scope, job scope, change order material, standalone purchases, and invoice tax / remainder."));
        builder.AppendLine(BuildStatementRowMarkup("Commitment Billings", EstimateMath.GetCurrency(commitmentBillings), "Billed vendor commitment values recognized as actual cost."));
        builder.AppendLine(BuildStatementRowMarkup("Total Actual Cost", EstimateMath.GetCurrency(totalActualCost), "Current job cost to date.", isEmphasized: true));
        builder.AppendLine(BuildStatementRowMarkup("Cost Variance To Estimate", EstimateMath.GetCurrency(costVariance), "Actual cost minus the current approved-scope estimate. Negative is under budget.", isEmphasized: true));
        builder.AppendLine(BuildStatementRowMarkup("Profit To Date", EstimateMath.GetCurrency(profitToDate), "Revenue minus actual cost.", isEmphasized: true));
        builder.AppendLine(BuildStatementRowMarkup("Margin To Date", EstimateMath.GetPercent(marginToDate), "Gross margin based on current actual cost."));

        builder.AppendLine("""
        </tbody>
    </table>
</section>
""");

        return builder.ToString();
    }

    private static string BuildScheduleOfValuesMarkup(JobRecord job)
    {
        var scheduledTotal = EstimateMath.RoundCurrency(job.ScheduleOfValues.Sum(item => item.ScheduledValue));
        var billedTotal = EstimateMath.RoundCurrency(job.ScheduleOfValues.Sum(item => item.BilledToDate));
        var paidTotal = EstimateMath.RoundCurrency(job.ScheduleOfValues.Sum(item => item.PaidToDate));
        var referenceTotal = EstimateMath.RoundCurrency(job.ScheduleOfValues.Sum(item => item.ReferenceValue));
        var percentTotal = EstimateMath.RoundCurrency(job.ScheduleOfValues.Sum(item => item.PercentageOfTotal));

        var builder = new StringBuilder();
        builder.Append(BuildMetricGridMarkup(
            [
                ("Bid Sale Reference", EstimateMath.GetCurrency(referenceTotal), "Total underlying bid-sale reference values"),
                ("Scheduled Value", EstimateMath.GetCurrency(scheduledTotal), "Current contract allocation"),
                ("Billed To Date", EstimateMath.GetCurrency(billedTotal), "Invoice progress against the SOV"),
                ("Paid To Date", EstimateMath.GetCurrency(paidTotal), "Cash progress recorded against the SOV"),
                ("% Of Contract", EstimateMath.GetPercent(percentTotal / 100m), "Total scheduled allocation percentage"),
                ("Remaining To Bill", EstimateMath.GetCurrency(EstimateMath.RoundCurrency(scheduledTotal - billedTotal)), "Scheduled value not yet billed")
            ]));

        if (job.ScheduleOfValues.Count == 0)
        {
            builder.Append("""<p class="empty-state-copy">No schedule of values lines are currently defined.</p>""");
            return builder.ToString();
        }

        builder.AppendLine("""
<section class="report-section">
    <h3 class="report-section-title">SOV Lines</h3>
    <table class="report-table report-table-dense">
        <thead>
            <tr>
                <th>Description</th>
                <th>Category</th>
                <th class="align-right">Bid Sale Ref</th>
                <th class="align-right">% Contract</th>
                <th class="align-right">Scheduled</th>
                <th class="align-right">Billed</th>
                <th class="align-right">Paid</th>
                <th class="align-right">% Paid</th>
                <th>Scope</th>
            </tr>
        </thead>
        <tbody>
""");

        foreach (var item in job.ScheduleOfValues
                     .OrderBy(scheduleItem => scheduleItem.IsChangeOrderLine)
                     .ThenBy(scheduleItem => GetScheduleValueSortOrder(scheduleItem)))
        {
            builder.AppendLine($$"""
            <tr>
                <td>{{Encode(item.Description)}}</td>
                <td>{{Encode(JobCostCodes.GetLabel(item.CategoryCode))}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(item.ReferenceValue)}}</td>
                <td class="align-right">{{EstimateMath.GetPercent(item.PercentageOfTotal / 100m)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(item.ScheduledValue)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(item.BilledToDate)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(item.PaidToDate)}}</td>
                <td class="align-right">{{EstimateMath.GetPercent(JobFinancialMath.GetScheduleValuePaidPercent(item) / 100m)}}</td>
                <td>{{Encode(GetScheduleValueScopeLabel(job, item))}}</td>
            </tr>
""");

            foreach (var subLine in item.SubLines)
            {
                builder.AppendLine($$"""
            <tr class="report-subrow">
                <td class="subrow-label">&bull; {{Encode(subLine.Description)}}</td>
                <td>{{Encode(GetLinkedCommitmentLabel(job, subLine.LinkedCommitmentId))}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(subLine.LineValue)}}</td>
                <td class="align-right"></td>
                <td class="align-right">{{EstimateMath.GetCurrency(subLine.LineValue)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(subLine.BilledAmount)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(subLine.PaidAmount)}}</td>
                <td class="align-right">{{EstimateMath.GetPercent(GetPercentOfValue(subLine.PaidAmount, subLine.LineValue))}}</td>
                <td>{{Encode(subLine.Notes)}}</td>
            </tr>
""");
            }
        }

        builder.AppendLine($$"""
        </tbody>
        <tfoot>
            <tr>
                <th colspan="2">Totals</th>
                <th class="align-right">{{EstimateMath.GetCurrency(referenceTotal)}}</th>
                <th class="align-right">{{EstimateMath.GetPercent(percentTotal / 100m)}}</th>
                <th class="align-right">{{EstimateMath.GetCurrency(scheduledTotal)}}</th>
                <th class="align-right">{{EstimateMath.GetCurrency(billedTotal)}}</th>
                <th class="align-right">{{EstimateMath.GetCurrency(paidTotal)}}</th>
                <th class="align-right">{{EstimateMath.GetPercent(GetPercentOfValue(paidTotal, scheduledTotal))}}</th>
                <th></th>
            </tr>
        </tfoot>
    </table>
</section>
""");

        return builder.ToString();
    }

    private static string BuildHoursMarkup(JobRecord job)
    {
        var plannedBaseHours = EstimateMath.RoundHours(
            job.Baseline.EstimatedAdminHours +
            job.Baseline.EstimatedEngineeringHours +
            job.Baseline.EstimatedInstallHours +
            job.Baseline.EstimatedDemoHours +
            job.Baseline.EstimatedTrimHours +
            job.Baseline.EstimatedTestHours);
        var plannedChangeOrderHours = EstimateMath.RoundHours(job.ChangeOrders.Sum(changeOrder => changeOrder.EstimatedLaborHours));
        var plannedTotalHours = EstimateMath.RoundHours(plannedBaseHours + plannedChangeOrderHours);
        var usedTotalHours = EstimateMath.GetJobActualLaborHours(job);

        var builder = new StringBuilder();
        builder.Append(BuildMetricGridMarkup(
            [
                ("Planned Base Hours", EstimateMath.GetHours(plannedBaseHours), "Baseline labor plan before change order labor"),
                ("Planned CO Hours", EstimateMath.GetHours(plannedChangeOrderHours), "Estimated labor across change orders"),
                ("Planned Total Hours", EstimateMath.GetHours(plannedTotalHours), "Baseline plus change order labor plan"),
                ("Used Total Hours", EstimateMath.GetHours(usedTotalHours), "Tracked labor hours to date"),
                ("Remaining Planned Hours", EstimateMath.GetHours(EstimateMath.RoundHours(plannedTotalHours - usedTotalHours)), "Difference between planned and used"),
                ("Labor Cost To Date", EstimateMath.GetCurrency(EstimateMath.GetJobActualLaborCost(job)), "Actual labor cost from time entries")
            ]));

        builder.AppendLine("""
<section class="report-section">
    <h3 class="report-section-title">Phase Hours</h3>
    <table class="report-table report-table-dense">
        <thead>
            <tr>
                <th>Scope</th>
                <th class="align-right">Planned Hours</th>
                <th class="align-right">Used Hours</th>
                <th class="align-right">Remaining Hours</th>
                <th class="align-right">Actual Cost</th>
            </tr>
        </thead>
        <tbody>
""");

        foreach (var costCode in JobCostCodes.TimeEntryCodes.Where(code => code != JobCostCodes.ChangeOrder && code != JobCostCodes.Other))
        {
            var planned = JobFinancialMath.GetBaselineHours(job.Baseline, costCode);
            var used = JobFinancialMath.GetJobActualHours(job, costCode);
            var remaining = EstimateMath.RoundHours(planned - used);
            var actualCost = JobFinancialMath.GetJobActualCost(job, costCode);
            builder.AppendLine($$"""
            <tr>
                <td>{{Encode(JobCostCodes.GetLabel(costCode))}}</td>
                <td class="align-right">{{EstimateMath.GetHours(planned)}}</td>
                <td class="align-right">{{EstimateMath.GetHours(used)}}</td>
                <td class="align-right">{{EstimateMath.GetHours(remaining)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(actualCost)}}</td>
            </tr>
""");
        }

        var changeOrderUsedHours = JobFinancialMath.GetJobActualHours(job, JobCostCodes.ChangeOrder);
        var changeOrderActualCost = JobFinancialMath.GetJobActualCost(job, JobCostCodes.ChangeOrder);
        builder.AppendLine($$"""
            <tr>
                <td>{{Encode(JobCostCodes.GetLabel(JobCostCodes.ChangeOrder))}}</td>
                <td class="align-right">{{EstimateMath.GetHours(plannedChangeOrderHours)}}</td>
                <td class="align-right">{{EstimateMath.GetHours(changeOrderUsedHours)}}</td>
                <td class="align-right">{{EstimateMath.GetHours(EstimateMath.RoundHours(plannedChangeOrderHours - changeOrderUsedHours))}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(changeOrderActualCost)}}</td>
            </tr>
            <tr class="totals-row">
                <td>Total</td>
                <td class="align-right">{{EstimateMath.GetHours(plannedTotalHours)}}</td>
                <td class="align-right">{{EstimateMath.GetHours(usedTotalHours)}}</td>
                <td class="align-right">{{EstimateMath.GetHours(EstimateMath.RoundHours(plannedTotalHours - usedTotalHours))}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(EstimateMath.GetJobActualLaborCost(job))}}</td>
            </tr>
""");

        builder.AppendLine("""
        </tbody>
    </table>
</section>
""");

        var laborClassGroups = job.TimeEntries
            .GroupBy(entry => string.IsNullOrWhiteSpace(entry.LaborClass) ? "Unspecified" : entry.LaborClass.Trim())
            .OrderByDescending(group => group.Sum(entry => entry.Hours))
            .ToList();

        if (laborClassGroups.Count > 0)
        {
            builder.AppendLine("""
<section class="report-section">
    <h3 class="report-section-title">Labor Class Summary</h3>
    <table class="report-table report-table-dense">
        <thead>
            <tr>
                <th>Labor Class</th>
                <th class="align-right">Hours</th>
                <th class="align-right">Avg. Cost Rate</th>
                <th class="align-right">Actual Cost</th>
            </tr>
        </thead>
        <tbody>
""");

            foreach (var group in laborClassGroups)
            {
                var hours = EstimateMath.RoundHours(group.Sum(entry => entry.Hours));
                var actualCost = EstimateMath.RoundCurrency(group.Sum(entry => entry.TotalCost));
                var averageRate = hours <= 0m ? 0m : EstimateMath.RoundCurrency(actualCost / hours);
                builder.AppendLine($$"""
            <tr>
                <td>{{Encode(group.Key)}}</td>
                <td class="align-right">{{EstimateMath.GetHours(hours)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(averageRate)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(actualCost)}}</td>
            </tr>
""");
            }

            builder.AppendLine("""
        </tbody>
    </table>
</section>
""");
        }

        return builder.ToString();
    }

    private static string BuildComparisonMarkup(JobRecord job)
    {
        var baseScopeEstimatedCost = EstimateMath.RoundCurrency(job.Baseline.LineItems.Sum(item => item.EstimatedCost));
        var baseScopeActualCost = EstimateMath.RoundCurrency(job.Baseline.LineItems.Sum(item => item.ActualCost));
        var jobScopeActualCost = EstimateMath.RoundCurrency(job.JobDevices.Sum(item => item.ActualCost));
        var changeOrderMaterialActualCost = EstimateMath.RoundCurrency(job.ChangeOrders.Sum(changeOrder => changeOrder.DeviceItems.Sum(item => item.ActualCost)));

        var builder = new StringBuilder();
        builder.Append(BuildMetricGridMarkup(
            [
                ("Base Scope Estimated", EstimateMath.GetCurrency(baseScopeEstimatedCost), "Estimated cost carried from the original bid"),
                ("Base Scope Actual", EstimateMath.GetCurrency(baseScopeActualCost), "Tracked actual cost on original bid-scope items"),
                ("Added Job Scope Actual", EstimateMath.GetCurrency(jobScopeActualCost), "Non-bid items added in Job Devices"),
                ("CO Material Actual", EstimateMath.GetCurrency(changeOrderMaterialActualCost), "Material actuals tracked inside change orders"),
                ("Base Qty Specified", FormatQuantity(job.Baseline.LineItems.Sum(item => item.Quantity)), "Original bid quantity total"),
                ("Base Qty Purchased", FormatQuantity(job.Baseline.LineItems.Sum(item => item.EffectiveActualQuantity)), "Actual purchased quantity total on bid-scope lines")
            ]));

        builder.Append(BuildBaseScopeComparisonSectionMarkup(job));
        builder.Append(BuildJobScopeComparisonSectionMarkup(job));
        builder.Append(BuildChangeOrderComparisonSectionMarkup(job));
        return builder.ToString();
    }

    private static string BuildMaterialAndInvoiceMarkup(JobRecord job)
    {
        var invoiceTotal = EstimateMath.RoundCurrency(job.Invoices.Sum(invoice => invoice.InvoiceTotal));
        var linkedTotal = EstimateMath.RoundCurrency(job.Invoices.Sum(invoice => JobFinancialMath.GetInvoiceLinkedActualCost(job, invoice.Id)));
        var remainderTotal = JobFinancialMath.GetInvoiceAutoRemainderTotal(job);

        var builder = new StringBuilder();
        builder.Append(BuildMetricGridMarkup(
            [
                ("Invoices", job.Invoices.Count.ToString(), "Tracked invoice records on this job"),
                ("Invoice Total", EstimateMath.GetCurrency(invoiceTotal), "Recorded invoice totals"),
                ("Linked Item Total", EstimateMath.GetCurrency(linkedTotal), "Tracked lines linked to invoices"),
                ("Tax / Remainder", EstimateMath.GetCurrency(remainderTotal), "Sales tax and any invoice value not directly assigned to tracked items; included in actual material cost"),
                ("Tracked Purchase Lines", GetTrackedMaterialLineCount(job).ToString(), "Base scope, job scope, change order, and standalone purchase lines"),
                ("Standalone Purchases", job.MaterialPurchases.Count.ToString(), "Legacy or direct purchase records still on the job")
            ]));

        builder.Append(BuildMaterialTrackingSectionMarkup(job));
        builder.Append(BuildInvoiceRegisterSectionMarkup(job));
        builder.Append(BuildStandalonePurchaseSectionMarkup(job));
        return builder.ToString();
    }

    private static string BuildCommitmentsMarkup(JobRecord job)
    {
        var committedTotal = EstimateMath.RoundCurrency(job.Commitments.Sum(item => item.CommittedAmount));
        var billedTotal = EstimateMath.RoundCurrency(job.Commitments.Sum(item => item.BilledAmount));
        var paidTotal = EstimateMath.RoundCurrency(job.Commitments.Sum(item => item.PaidAmount));
        var remainingTotal = EstimateMath.RoundCurrency(job.Commitments.Sum(item => Math.Max(item.CommittedAmount, item.BilledAmount) - item.PaidAmount));

        var builder = new StringBuilder();
        builder.Append(BuildMetricGridMarkup(
            [
                ("Commitments", job.Commitments.Count.ToString(), "Open and historical vendor commitments"),
                ("Committed", EstimateMath.GetCurrency(committedTotal), "Committed vendor value"),
                ("Billed", EstimateMath.GetCurrency(billedTotal), "Vendor billings recorded"),
                ("Paid", EstimateMath.GetCurrency(paidTotal), "Vendor payments recorded"),
                ("Remaining To Pay", EstimateMath.GetCurrency(remainingTotal), "Committed or billed vendor cost still to pay")
            ]));

        if (job.Commitments.Count == 0)
        {
            builder.Append("""<p class="empty-state-copy">No commitments are currently tracked on this job.</p>""");
            return builder.ToString();
        }

        builder.AppendLine("""
<section class="report-section">
    <h3 class="report-section-title">Commitment Register</h3>
    <table class="report-table report-table-dense">
        <thead>
            <tr>
                <th>Commitment</th>
                <th>Vendor</th>
                <th>Description</th>
                <th>SOV Link</th>
                <th class="align-right">Committed</th>
                <th class="align-right">Billed</th>
                <th class="align-right">Paid</th>
                <th class="align-right">Remaining</th>
                <th>Invoice</th>
            </tr>
        </thead>
        <tbody>
""");

        foreach (var commitment in job.Commitments
                     .OrderBy(commitment => commitment.InvoiceDate)
                     .ThenBy(commitment => commitment.CommitmentNumber)
                     .ThenBy(commitment => commitment.Vendor))
        {
            builder.AppendLine($$"""
            <tr>
                <td>{{Encode(string.IsNullOrWhiteSpace(commitment.CommitmentNumber) ? "Commitment" : commitment.CommitmentNumber.Trim())}}</td>
                <td>{{Encode(commitment.Vendor)}}</td>
                <td>{{Encode(commitment.Description)}}</td>
                <td>{{Encode(GetLinkedScheduleValueLabel(job, commitment.ScheduleValueItemId))}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(commitment.CommittedAmount)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(commitment.BilledAmount)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(commitment.PaidAmount)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(Math.Max(commitment.CommittedAmount, commitment.BilledAmount) - commitment.PaidAmount)}}</td>
                <td>{{Encode(BuildCommitmentInvoiceLabel(commitment))}}</td>
            </tr>
""");
        }

        builder.AppendLine($$"""
        </tbody>
        <tfoot>
            <tr>
                <th colspan="4">Totals</th>
                <th class="align-right">{{EstimateMath.GetCurrency(committedTotal)}}</th>
                <th class="align-right">{{EstimateMath.GetCurrency(billedTotal)}}</th>
                <th class="align-right">{{EstimateMath.GetCurrency(paidTotal)}}</th>
                <th class="align-right">{{EstimateMath.GetCurrency(remainingTotal)}}</th>
                <th></th>
            </tr>
        </tfoot>
    </table>
</section>
""");

        return builder.ToString();
    }

    private static string BuildChangeOrdersPageMarkup(JobRecord job, string footerReference)
    {
        if (job.ChangeOrders.Count == 0)
        {
            return string.Empty;
        }

        var bodyBuilder = new StringBuilder();
        bodyBuilder.Append(BuildMetricGridMarkup(
            [
                ("Change Orders", job.ChangeOrders.Count.ToString(), "Approved and pending change orders"),
                ("Approved CO Revenue", EstimateMath.GetCurrency(EstimateMath.GetJobApprovedChangeOrderRevenue(job)), "Approved revenue added to contract"),
                ("Approved CO Estimated Cost", EstimateMath.GetCurrency(EstimateMath.GetJobApprovedChangeOrderCost(job)), "Approved cost impact"),
                ("Tracked CO Actual", EstimateMath.GetCurrency(job.ChangeOrders.Sum(changeOrder => GetChangeOrderActualCost(job, changeOrder))), "Actual cost tracked across all change orders"),
                ("CO Planned Hours", EstimateMath.GetHours(job.ChangeOrders.Sum(changeOrder => changeOrder.EstimatedLaborHours)), "Estimated change order labor"),
                ("CO Used Hours", EstimateMath.GetHours(job.TimeEntries.Where(entry => entry.ChangeOrderId is not null).Sum(entry => entry.Hours)), "Tracked change order labor hours")
            ]));

        bodyBuilder.AppendLine("""
<section class="report-section">
    <h3 class="report-section-title">Change Order Register</h3>
    <table class="report-table report-table-dense">
        <thead>
            <tr>
                <th>Change Order</th>
                <th>Status</th>
                <th class="align-right">Revenue</th>
                <th class="align-right">Est. Cost</th>
                <th class="align-right">Actual Cost</th>
                <th class="align-right">Est. Hours</th>
                <th class="align-right">Used Hours</th>
                <th>Notes</th>
            </tr>
        </thead>
        <tbody>
""");

        foreach (var changeOrder in job.ChangeOrders
                     .OrderBy(changeOrder => changeOrder.Approved ? 0 : 1)
                     .ThenBy(changeOrder => changeOrder.ApprovedOn)
                     .ThenBy(changeOrder => changeOrder.Title))
        {
            var usedHours = JobFinancialMath.GetChangeOrderActualLaborHours(job, changeOrder.Id);
            bodyBuilder.AppendLine($$"""
            <tr>
                <td>{{Encode(GetChangeOrderLabel(changeOrder))}}</td>
                <td>{{Encode(changeOrder.Approved ? "Approved" : "Pending")}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(changeOrder.RevenueAmount)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(changeOrder.EstimatedCostImpact)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(GetChangeOrderActualCost(job, changeOrder))}}</td>
                <td class="align-right">{{EstimateMath.GetHours(changeOrder.EstimatedLaborHours)}}</td>
                <td class="align-right">{{EstimateMath.GetHours(usedHours)}}</td>
                <td>{{Encode(changeOrder.Notes)}}</td>
            </tr>
""");
        }

        bodyBuilder.AppendLine("""
        </tbody>
    </table>
</section>
""");

        return BuildSectionPageMarkup(
            "change-orders",
            "Scope Adjustments",
            "Change Orders",
            "Revenue, cost, and labor impacts that modified the original job scope.",
            bodyBuilder.ToString(),
            footerReference);
    }

    private static string BuildNotesPageMarkup(JobRecord job, string footerReference)
    {
        var notesMarkup = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(job.ProposalSummary) ||
            !string.IsNullOrWhiteSpace(job.Site.ScopeOfWork) ||
            !string.IsNullOrWhiteSpace(job.Baseline.ScopeSummary))
        {
            notesMarkup.Append(BuildNarrativeSectionMarkup(
                "Project Narrative",
                !string.IsNullOrWhiteSpace(job.ProposalSummary)
                    ? job.ProposalSummary
                    : !string.IsNullOrWhiteSpace(job.Site.ScopeOfWork)
                        ? job.Site.ScopeOfWork
                        : job.Baseline.ScopeSummary));
        }

        if (!string.IsNullOrWhiteSpace(job.Exclusions))
        {
            notesMarkup.Append(BuildNarrativeSectionMarkup("Exclusions", job.Exclusions));
        }

        if (!string.IsNullOrWhiteSpace(job.ProposalClosing))
        {
            notesMarkup.Append(BuildNarrativeSectionMarkup("Proposal Closing", job.ProposalClosing));
        }

        if (!string.IsNullOrWhiteSpace(job.Site.Notes))
        {
            notesMarkup.Append(BuildNarrativeSectionMarkup("Site Notes", job.Site.Notes));
        }

        if (!string.IsNullOrWhiteSpace(job.Notes))
        {
            notesMarkup.Append(BuildNarrativeSectionMarkup("Job Notes", job.Notes));
        }

        return notesMarkup.Length == 0
            ? string.Empty
            : BuildSectionPageMarkup(
                "notes",
                "Narrative",
                "Notes And Scope Context",
                "Supporting narrative pulled together so the report can stand on its own outside the app.",
                notesMarkup.ToString(),
                footerReference);
    }

    //******************************//
    //******** Sections ************//
    //******************************//

    private static string BuildBaseScopeComparisonSectionMarkup(JobRecord job)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""
<section class="report-section">
    <h3 class="report-section-title">Base Scope Comparison</h3>
""");

        if (job.Baseline.LineItems.Count == 0)
        {
            builder.Append("""<p class="empty-state-copy">No base scope line items are available to compare.</p></section>""");
            return builder.ToString();
        }

        builder.AppendLine("""
    <table class="report-table report-table-dense">
        <thead>
            <tr>
                <th>Type</th>
                <th>Description</th>
                <th class="align-right">Bid Qty</th>
                <th class="align-right">Actual Qty</th>
                <th class="align-right">Est. Cost</th>
                <th class="align-right">Actual Cost</th>
                <th class="align-right">Variance</th>
                <th>Invoice Ref</th>
            </tr>
        </thead>
        <tbody>
""");

        foreach (var item in job.Baseline.LineItems
                     .OrderBy(lineItem => GetTrackedDeviceSortOrder(lineItem.CategoryCode))
                     .ThenBy(lineItem => lineItem.SourceSection)
                     .ThenBy(lineItem => lineItem.Description))
        {
            builder.AppendLine($$"""
            <tr>
                <td>{{Encode(GetTrackedDeviceTypeLabel(item.SourceSection, item.CategoryCode))}}</td>
                <td>{{Encode(item.Description)}}</td>
                <td class="align-right">{{FormatQuantity(item.Quantity)}}</td>
                <td class="align-right">{{FormatQuantity(item.EffectiveActualQuantity)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(item.EstimatedCost)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(item.ActualCost)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(item.ActualCost - item.EstimatedCost)}}</td>
                <td>{{Encode(GetBaseScopeInvoiceReference(job, item))}}</td>
            </tr>
""");

            foreach (var purchaseLine in item.ActualPurchaseLines)
            {
                builder.AppendLine($$"""
            <tr class="report-subrow">
                <td>Purchase</td>
                <td class="subrow-label">&bull; {{Encode(purchaseLine.Description)}}</td>
                <td class="align-right"></td>
                <td class="align-right">{{FormatQuantity(purchaseLine.Quantity)}}</td>
                <td class="align-right"></td>
                <td class="align-right">{{EstimateMath.GetCurrency(purchaseLine.ActualCost)}}</td>
                <td class="align-right"></td>
                <td>{{Encode(GetInvoiceReference(job, purchaseLine.InvoiceId))}}</td>
            </tr>
""");
            }
        }

        builder.AppendLine($$"""
        </tbody>
        <tfoot>
            <tr>
                <th colspan="2">Totals</th>
                <th class="align-right">{{FormatQuantity(job.Baseline.LineItems.Sum(item => item.Quantity))}}</th>
                <th class="align-right">{{FormatQuantity(job.Baseline.LineItems.Sum(item => item.EffectiveActualQuantity))}}</th>
                <th class="align-right">{{EstimateMath.GetCurrency(job.Baseline.LineItems.Sum(item => item.EstimatedCost))}}</th>
                <th class="align-right">{{EstimateMath.GetCurrency(job.Baseline.LineItems.Sum(item => item.ActualCost))}}</th>
                <th class="align-right">{{EstimateMath.GetCurrency(job.Baseline.LineItems.Sum(item => item.ActualCost - item.EstimatedCost))}}</th>
                <th></th>
            </tr>
        </tfoot>
    </table>
</section>
""");

        return builder.ToString();
    }

    private static string BuildJobScopeComparisonSectionMarkup(JobRecord job)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""
<section class="report-section">
    <h3 class="report-section-title">Job Scope Additions</h3>
""");

        if (job.JobDevices.Count == 0)
        {
            builder.Append("""<p class="empty-state-copy">No added job-scope devices or material lines are currently tracked.</p></section>""");
            return builder.ToString();
        }

        builder.AppendLine("""
    <table class="report-table report-table-dense">
        <thead>
            <tr>
                <th>Type</th>
                <th>Description</th>
                <th class="align-right">Qty</th>
                <th class="align-right">Est. Cost</th>
                <th class="align-right">Actual Cost</th>
                <th class="align-right">Variance</th>
                <th>Invoice Ref</th>
            </tr>
        </thead>
        <tbody>
""");

        foreach (var item in job.JobDevices
                     .OrderBy(device => GetTrackedDeviceSortOrder(device.CategoryCode))
                     .ThenBy(device => device.Description))
        {
            builder.AppendLine($$"""
            <tr>
                <td>{{Encode(JobCostCodes.GetLabel(item.CategoryCode))}}</td>
                <td>{{Encode(item.Description)}}</td>
                <td class="align-right">{{FormatQuantity(item.Quantity)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(item.EstimatedCost)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(item.ActualCost)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(item.ActualCost - item.EstimatedCost)}}</td>
                <td>{{Encode(GetInvoiceReference(job, item.InvoiceId))}}</td>
            </tr>
""");
        }

        builder.AppendLine("""
        </tbody>
    </table>
</section>
""");

        return builder.ToString();
    }

    private static string BuildChangeOrderComparisonSectionMarkup(JobRecord job)
    {
        var changeOrderDeviceRows = job.ChangeOrders
            .SelectMany(changeOrder => changeOrder.DeviceItems.Select(item => new { ChangeOrder = changeOrder, Item = item }))
            .OrderBy(entry => entry.ChangeOrder.Approved ? 0 : 1)
            .ThenBy(entry => entry.ChangeOrder.Title)
            .ThenBy(entry => entry.Item.Description)
            .ToList();

        if (changeOrderDeviceRows.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine("""
<section class="report-section">
    <h3 class="report-section-title">Change Order Device And Material Lines</h3>
    <table class="report-table report-table-dense">
        <thead>
            <tr>
                <th>Change Order</th>
                <th>Type</th>
                <th>Description</th>
                <th class="align-right">Qty</th>
                <th class="align-right">Est. Cost</th>
                <th class="align-right">Actual Cost</th>
                <th>Invoice Ref</th>
            </tr>
        </thead>
        <tbody>
""");

        foreach (var entry in changeOrderDeviceRows)
        {
            builder.AppendLine($$"""
            <tr>
                <td>{{Encode(GetChangeOrderLabel(entry.ChangeOrder))}}</td>
                <td>{{Encode(JobCostCodes.GetLabel(entry.Item.CategoryCode))}}</td>
                <td>{{Encode(entry.Item.Description)}}</td>
                <td class="align-right">{{FormatQuantity(entry.Item.Quantity)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(entry.Item.EstimatedCost)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(entry.Item.ActualCost)}}</td>
                <td>{{Encode(GetInvoiceReference(job, entry.Item.InvoiceId))}}</td>
            </tr>
""");
        }

        builder.AppendLine("""
        </tbody>
    </table>
</section>
""");

        return builder.ToString();
    }

    private static string BuildMaterialTrackingSectionMarkup(JobRecord job)
    {
        var trackingLines = BuildTrackedMaterialLines(job);
        var builder = new StringBuilder();
        builder.AppendLine("""
<section class="report-section">
    <h3 class="report-section-title">Tracked Material Lines</h3>
""");

        if (trackingLines.Count == 0)
        {
            builder.Append("""<p class="empty-state-copy">No tracked material or device lines currently have actual purchase detail.</p></section>""");
            return builder.ToString();
        }

        builder.AppendLine("""
    <table class="report-table report-table-dense">
        <thead>
            <tr>
                <th>Scope</th>
                <th>Type</th>
                <th>Description</th>
                <th class="align-right">Qty</th>
                <th>Invoice Ref</th>
                <th class="align-right">Actual Cost</th>
                <th>Notes</th>
            </tr>
        </thead>
        <tbody>
""");

        foreach (var line in trackingLines)
        {
            builder.AppendLine($$"""
            <tr>
                <td>{{Encode(line.Scope)}}</td>
                <td>{{Encode(line.Type)}}</td>
                <td>{{Encode(line.Description)}}</td>
                <td class="align-right">{{FormatQuantity(line.Quantity)}}</td>
                <td>{{Encode(line.InvoiceReference)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(line.ActualCost)}}</td>
                <td>{{Encode(line.Notes)}}</td>
            </tr>
""");
        }

        builder.AppendLine("""
        </tbody>
    </table>
</section>
""");

        return builder.ToString();
    }

    private static string BuildInvoiceRegisterSectionMarkup(JobRecord job)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""
<section class="report-section">
    <h3 class="report-section-title">Invoice Register</h3>
""");

        if (job.Invoices.Count == 0)
        {
            builder.Append("""<p class="empty-state-copy">No invoice records are currently attached to this job.</p></section>""");
            return builder.ToString();
        }

        foreach (var invoice in job.Invoices
                     .OrderBy(invoice => invoice.InvoiceDate)
                     .ThenBy(invoice => invoice.ReferenceNumber)
                     .ThenBy(invoice => invoice.Vendor))
        {
            var linkedLines = BuildInvoiceLinkedLines(job, invoice.Id);
            builder.AppendLine($$"""
    <article class="invoice-card break-avoid">
        <header class="invoice-card-header">
            <div>
                <h4>{{Encode(GetInvoiceLabel(invoice))}}</h4>
                <p class="invoice-card-meta">{{Encode(invoice.Vendor)}} | {{Encode(invoice.InvoiceDate.ToString("d"))}} | {{Encode(invoice.InvoiceNumber)}}</p>
            </div>
            <div class="invoice-card-totals">
                <span>Total {{EstimateMath.GetCurrency(invoice.InvoiceTotal)}}</span>
                <span>Linked {{EstimateMath.GetCurrency(JobFinancialMath.GetInvoiceLinkedActualCost(job, invoice.Id))}}</span>
                <span>Tax / Remainder {{EstimateMath.GetCurrency(JobFinancialMath.GetInvoiceAutoRemainder(job, invoice))}}</span>
            </div>
        </header>
""");

            if (linkedLines.Count == 0)
            {
                builder.Append("""        <p class="empty-state-copy compact-copy">No tracked base-scope, job-scope, or change-order lines are currently linked to this invoice.</p>""");
            }
            else
            {
                builder.AppendLine("""
        <table class="report-table report-table-dense invoice-linked-table">
            <thead>
                <tr>
                    <th>Scope</th>
                    <th>Type</th>
                    <th>Description</th>
                    <th class="align-right">Qty</th>
                    <th class="align-right">Actual Cost</th>
                </tr>
            </thead>
            <tbody>
""");

                foreach (var linkedLine in linkedLines)
                {
                    builder.AppendLine($$"""
                <tr>
                    <td>{{Encode(linkedLine.Scope)}}</td>
                    <td>{{Encode(linkedLine.Type)}}</td>
                    <td>{{Encode(linkedLine.Description)}}</td>
                    <td class="align-right">{{FormatQuantity(linkedLine.Quantity)}}</td>
                    <td class="align-right">{{EstimateMath.GetCurrency(linkedLine.ActualCost)}}</td>
                </tr>
""");
                }

                builder.AppendLine("""
            </tbody>
        </table>
""");
            }

            builder.AppendLine("""
    </article>
""");
        }

        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildStandalonePurchaseSectionMarkup(JobRecord job)
    {
        if (job.MaterialPurchases.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine("""
<section class="report-section">
    <h3 class="report-section-title">Standalone Purchase Records</h3>
    <table class="report-table report-table-dense">
        <thead>
            <tr>
                <th>Date</th>
                <th>Vendor</th>
                <th>Description</th>
                <th class="align-right">Qty</th>
                <th class="align-right">Subtotal</th>
                <th class="align-right">Tax</th>
                <th class="align-right">Total</th>
                <th>Reference</th>
            </tr>
        </thead>
        <tbody>
""");

        foreach (var purchase in job.MaterialPurchases.OrderBy(item => item.PurchaseDate).ThenBy(item => item.Vendor))
        {
            builder.AppendLine($$"""
            <tr>
                <td>{{Encode(purchase.PurchaseDate.ToString("d"))}}</td>
                <td>{{Encode(purchase.Vendor)}}</td>
                <td>{{Encode(purchase.Description)}}</td>
                <td class="align-right">{{FormatQuantity(purchase.Quantity)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(JobFinancialMath.GetMaterialPurchaseSubtotal(purchase))}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(purchase.SalesTax)}}</td>
                <td class="align-right">{{EstimateMath.GetCurrency(JobFinancialMath.GetMaterialPurchaseTotal(purchase))}}</td>
                <td>{{Encode(GetStandalonePurchaseReference(purchase))}}</td>
            </tr>
""");
        }

        builder.AppendLine("""
        </tbody>
    </table>
</section>
""");

        return builder.ToString();
    }

    //******************************//
    //******** Card Helpers ********//
    //******************************//

    private static string BuildProjectSummaryMarkup(JobRecord job, ClientRecord? client)
    {
        var sourceBidNumber = string.IsNullOrWhiteSpace(job.Baseline.SourceBidNumber)
            ? "Not linked"
            : job.Baseline.SourceBidNumber.Trim();

        return BuildMetaListMarkup(
        [
            ("Job Number", GetDisplayJobNumber(job)),
            ("Project Name", GetDisplayProjectName(job)),
            ("Status", GetDisplayStatus(job.Status)),
            ("Client", client?.Name?.Trim() ?? "Not linked"),
            ("Primary Contact", client?.PrimaryContact?.Trim() ?? "Not provided"),
            ("Email", client?.Email?.Trim() ?? "Not provided"),
            ("Phone", client?.Phone?.Trim() ?? "Not provided"),
            ("Source Bid", sourceBidNumber),
            ("Created", job.CreatedOn.ToString("D")),
            ("Site", GetDisplaySiteName(job.Site)),
            ("Address", GetDisplayProjectAddress(job.Site))
        ]);
    }

    private static string BuildTableOfContentsMarkup(JobRecord job)
    {
        var entries = new List<(string SectionId, string Label)>
        {
            ("overview", "Overview"),
            ("financial-overview", "Profit And Loss Statement"),
            ("schedule-of-values", "Schedule Of Values"),
            ("hours", "Hours And Labor Tracking"),
            ("comparison", "Bid To Job Comparison"),
            ("materials-and-invoices", "Material And Invoice Tracking"),
            ("commitments", "Commitments")
        };

        if (job.ChangeOrders.Count > 0)
        {
            entries.Add(("change-orders", "Change Orders"));
        }

        if (!string.IsNullOrWhiteSpace(job.ProposalSummary) ||
            !string.IsNullOrWhiteSpace(job.Site.ScopeOfWork) ||
            !string.IsNullOrWhiteSpace(job.Baseline.ScopeSummary) ||
            !string.IsNullOrWhiteSpace(job.Exclusions) ||
            !string.IsNullOrWhiteSpace(job.ProposalClosing) ||
            !string.IsNullOrWhiteSpace(job.Site.Notes) ||
            !string.IsNullOrWhiteSpace(job.Notes))
        {
            entries.Add(("notes", "Notes And Scope Context"));
        }

        var builder = new StringBuilder();
        builder.Append("""<nav class="toc-card">""");

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            builder.Append($$"""<a class="toc-link" href="#{{entry.SectionId}}">{{index + 1}}. {{Encode(entry.Label)}}</a>""");
        }

        builder.Append("</nav>");
        return builder.ToString();
    }

    private static string BuildMetricGridMarkup(IEnumerable<(string Label, string Value, string Detail)> metrics)
    {
        var builder = new StringBuilder();
        builder.Append("""<section class="metric-grid">""");

        foreach (var (label, value, detail) in metrics)
        {
            builder.Append($$"""
<article class="metric-card">
    <span class="metric-label">{{Encode(label)}}</span>
    <strong class="metric-value">{{Encode(value)}}</strong>
    <span class="metric-detail">{{Encode(detail)}}</span>
</article>
""");
        }

        builder.Append("</section>");
        return builder.ToString();
    }

    private static string BuildInfoCardMarkup(string title, string bodyMarkup)
    {
        if (string.IsNullOrWhiteSpace(bodyMarkup))
        {
            return string.Empty;
        }

        return $$"""
<article class="info-card">
    <h3 class="info-card-title">{{Encode(title)}}</h3>
    {{bodyMarkup}}
</article>
""";
    }

    private static string BuildOptionalNarrativeCardMarkup(string title, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        return BuildInfoCardMarkup(title, $$"""<div class="report-copy">{{BuildRichTextMarkup(content)}}</div>""");
    }

    private static string BuildNarrativeSectionMarkup(string title, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        return $$"""
<section class="report-section">
    <h3 class="report-section-title">{{Encode(title)}}</h3>
    <div class="report-copy">
        {{BuildRichTextMarkup(content)}}
    </div>
</section>
""";
    }

    private static string BuildMetaListMarkup(IEnumerable<(string Label, string Value)> entries)
    {
        var builder = new StringBuilder();
        builder.Append("""<div class="meta-list">""");

        foreach (var (label, value) in entries.Where(entry => !string.IsNullOrWhiteSpace(entry.Value)))
        {
            builder.Append($$"""
<div class="meta-entry">
    <span class="meta-label">{{Encode(label)}}</span>
    <span class="meta-value">{{Encode(value)}}</span>
</div>
""");
        }

        builder.Append("</div>");
        return builder.ToString();
    }

    private static string BuildBrandingMarkup(DocumentBrandingProfile brandingProfile)
    {
        var hasLogo = !string.IsNullOrWhiteSpace(brandingProfile.LogoDataUri);
        var detailsMarkup = BuildBrandingDetailsMarkup(brandingProfile);

        if (hasLogo)
        {
            return $$"""
<section class="report-branding-panel">
    <img class="report-logo-image" src="{{brandingProfile.LogoDataUri}}" alt="Company logo" />
    {{BuildBrandingCompanyNameMarkup(brandingProfile.CompanyName)}}
    {{detailsMarkup}}
</section>
""";
        }

        if (string.IsNullOrWhiteSpace(brandingProfile.CompanyName) && string.IsNullOrWhiteSpace(detailsMarkup))
        {
            return string.Empty;
        }

        return $$"""
<section class="report-branding-panel">
    {{BuildBrandingCompanyNameMarkup(brandingProfile.CompanyName)}}
    {{detailsMarkup}}
</section>
""";
    }

    private static string BuildBrandingCompanyNameMarkup(string? companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            return string.Empty;
        }

        return $$"""<strong class="report-branding-company-name">{{Encode(companyName.Trim())}}</strong>""";
    }

    private static string BuildBrandingDetailsMarkup(DocumentBrandingProfile brandingProfile)
    {
        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(brandingProfile.CompanyLicenseNumber))
        {
            details.Add($"License #{brandingProfile.CompanyLicenseNumber.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(brandingProfile.CompanyAddress))
        {
            details.Add(brandingProfile.CompanyAddress.Trim());
        }

        if (!string.IsNullOrWhiteSpace(brandingProfile.CompanyPhoneNumber))
        {
            details.Add(brandingProfile.CompanyPhoneNumber.Trim());
        }

        if (!string.IsNullOrWhiteSpace(brandingProfile.CompanyEmail))
        {
            details.Add(brandingProfile.CompanyEmail.Trim());
        }

        if (details.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("""<p class="report-branding-details">""");

        for (var index = 0; index < details.Count; index++)
        {
            if (index > 0)
            {
                builder.Append("""<span class="report-branding-divider">&bull;</span>""");
            }

            builder.Append(Encode(details[index]));
        }

        builder.Append("</p>");
        return builder.ToString();
    }

    private static string BuildStatementRowMarkup(string label, string amount, string context, bool isEmphasized = false)
    {
        var rowClass = isEmphasized ? " class=\"totals-row\"" : string.Empty;
        return $$"""
            <tr{{rowClass}}>
                <td>{{Encode(label)}}</td>
                <td class="align-right">{{Encode(amount)}}</td>
                <td>{{Encode(context)}}</td>
            </tr>
""";
    }

    //******************************//
    //******** Data Helpers ********//
    //******************************//

    private static List<TrackedMaterialLine> BuildTrackedMaterialLines(JobRecord job)
    {
        var result = new List<TrackedMaterialLine>();

        foreach (var item in job.Baseline.LineItems)
        {
            if (item.ActualPurchaseLines.Count > 0)
            {
                result.AddRange(item.ActualPurchaseLines.Select(purchaseLine =>
                    new TrackedMaterialLine(
                        Scope: "Base Scope",
                        Type: GetTrackedDeviceTypeLabel(item.SourceSection, item.CategoryCode),
                        Description: purchaseLine.Description,
                        Quantity: purchaseLine.Quantity,
                        InvoiceReference: GetInvoiceReference(job, purchaseLine.InvoiceId),
                        ActualCost: purchaseLine.ActualCost,
                        Notes: purchaseLine.Notes)));
                continue;
            }

            if (item.ActualCost <= 0m && item.InvoiceId is null)
            {
                continue;
            }

            result.Add(new TrackedMaterialLine(
                Scope: "Base Scope",
                Type: GetTrackedDeviceTypeLabel(item.SourceSection, item.CategoryCode),
                Description: item.Description,
                Quantity: item.EffectiveActualQuantity,
                InvoiceReference: GetInvoiceReference(job, item.InvoiceId),
                ActualCost: item.ActualCost,
                Notes: item.Notes));
        }

        result.AddRange(job.JobDevices
            .Where(item => item.ActualCost > 0m || item.InvoiceId is not null)
            .Select(item => new TrackedMaterialLine(
                Scope: "Job Scope",
                Type: JobCostCodes.GetLabel(item.CategoryCode),
                Description: item.Description,
                Quantity: item.Quantity,
                InvoiceReference: GetInvoiceReference(job, item.InvoiceId),
                ActualCost: item.ActualCost,
                Notes: item.Notes)));

        foreach (var entry in job.ChangeOrders.SelectMany(changeOrder => changeOrder.DeviceItems.Select(item => new { ChangeOrder = changeOrder, Item = item })))
        {
            if (entry.Item.ActualCost <= 0m && entry.Item.InvoiceId is null)
            {
                continue;
            }

            result.Add(new TrackedMaterialLine(
                Scope: $"CO - {GetChangeOrderLabel(entry.ChangeOrder)}",
                Type: JobCostCodes.GetLabel(entry.Item.CategoryCode),
                Description: entry.Item.Description,
                Quantity: entry.Item.Quantity,
                InvoiceReference: GetInvoiceReference(job, entry.Item.InvoiceId),
                ActualCost: entry.Item.ActualCost,
                Notes: entry.Item.Notes));
        }

        return result
            .OrderBy(line => line.Scope, StringComparer.OrdinalIgnoreCase)
            .ThenBy(line => line.Type, StringComparer.OrdinalIgnoreCase)
            .ThenBy(line => line.Description, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<InvoiceLinkedLine> BuildInvoiceLinkedLines(JobRecord job, Guid invoiceId)
    {
        var result = new List<InvoiceLinkedLine>();

        foreach (var item in job.Baseline.LineItems)
        {
            if (item.ActualPurchaseLines.Count > 0)
            {
                result.AddRange(item.ActualPurchaseLines
                    .Where(purchaseLine => purchaseLine.InvoiceId == invoiceId)
                    .Select(purchaseLine => new InvoiceLinkedLine(
                        Scope: "Base Scope",
                        Type: GetTrackedDeviceTypeLabel(item.SourceSection, item.CategoryCode),
                        Description: purchaseLine.Description,
                        Quantity: purchaseLine.Quantity,
                        ActualCost: purchaseLine.ActualCost)));
                continue;
            }

            if (item.InvoiceId == invoiceId)
            {
                result.Add(new InvoiceLinkedLine(
                    Scope: "Base Scope",
                    Type: GetTrackedDeviceTypeLabel(item.SourceSection, item.CategoryCode),
                    Description: item.Description,
                    Quantity: item.EffectiveActualQuantity,
                    ActualCost: item.ActualCost));
            }
        }

        result.AddRange(job.JobDevices
            .Where(item => item.InvoiceId == invoiceId)
            .Select(item => new InvoiceLinkedLine(
                Scope: "Job Scope",
                Type: JobCostCodes.GetLabel(item.CategoryCode),
                Description: item.Description,
                Quantity: item.Quantity,
                ActualCost: item.ActualCost)));

        result.AddRange(job.ChangeOrders
            .SelectMany(changeOrder => changeOrder.DeviceItems
                .Where(item => item.InvoiceId == invoiceId)
                .Select(item => new InvoiceLinkedLine(
                    Scope: $"CO - {GetChangeOrderLabel(changeOrder)}",
                    Type: JobCostCodes.GetLabel(item.CategoryCode),
                    Description: item.Description,
                    Quantity: item.Quantity,
                    ActualCost: item.ActualCost))));

        return result;
    }

    private static int GetTrackedMaterialLineCount(JobRecord job) =>
        BuildTrackedMaterialLines(job).Count;

    private static string GetScheduleValueScopeLabel(JobRecord job, ScheduleValueItem item)
    {
        if (!item.IsChangeOrderLine)
        {
            return "Base Contract";
        }

        var linkedChangeOrder = job.ChangeOrders.FirstOrDefault(changeOrder => changeOrder.Id == item.LinkedChangeOrderId);
        if (linkedChangeOrder is null)
        {
            return "Change Order";
        }

        return linkedChangeOrder.Approved
            ? $"Approved CO - {GetChangeOrderLabel(linkedChangeOrder)}"
            : $"Pending CO - {GetChangeOrderLabel(linkedChangeOrder)}";
    }

    private static string GetLinkedCommitmentLabel(JobRecord job, Guid? linkedCommitmentId)
    {
        if (linkedCommitmentId is null)
        {
            return string.Empty;
        }

        var commitment = job.Commitments.FirstOrDefault(item => item.Id == linkedCommitmentId.Value);
        if (commitment is null)
        {
            return "Linked commitment";
        }

        if (!string.IsNullOrWhiteSpace(commitment.CommitmentNumber))
        {
            return commitment.CommitmentNumber.Trim();
        }

        if (!string.IsNullOrWhiteSpace(commitment.Vendor))
        {
            return commitment.Vendor.Trim();
        }

        return "Linked commitment";
    }

    private static string GetLinkedScheduleValueLabel(JobRecord job, Guid? scheduleValueItemId)
    {
        if (scheduleValueItemId is null)
        {
            return "Not linked";
        }

        var scheduleValueItem = job.ScheduleOfValues.FirstOrDefault(item => item.Id == scheduleValueItemId.Value);
        return scheduleValueItem is null
            ? "Not linked"
            : scheduleValueItem.Description.Trim();
    }

    private static string GetBaseScopeInvoiceReference(JobRecord job, JobBaselineLineItem item)
    {
        if (item.ActualPurchaseLines.Count == 0)
        {
            return GetInvoiceReference(job, item.InvoiceId);
        }

        var references = item.ActualPurchaseLines
            .Select(purchaseLine => GetInvoiceReference(job, purchaseLine.InvoiceId))
            .Where(reference => !string.IsNullOrWhiteSpace(reference) && !string.Equals(reference, "Not linked", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return references.Count == 0 ? "Tracked on purchase rows" : string.Join(", ", references);
    }

    private static string GetInvoiceReference(JobRecord job, Guid? invoiceId)
    {
        if (invoiceId is null)
        {
            return "Not linked";
        }

        var invoice = job.Invoices.FirstOrDefault(item => item.Id == invoiceId.Value);
        return invoice is null ? "Not linked" : GetInvoiceLabel(invoice);
    }

    private static string GetInvoiceLabel(JobInvoiceRecord invoice)
    {
        if (!string.IsNullOrWhiteSpace(invoice.ReferenceNumber))
        {
            return invoice.ReferenceNumber.Trim();
        }

        if (!string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            return invoice.InvoiceNumber.Trim();
        }

        if (!string.IsNullOrWhiteSpace(invoice.Vendor))
        {
            return $"{invoice.Vendor.Trim()} {invoice.InvoiceDate:MM/dd/yyyy}";
        }

        return $"Invoice {invoice.InvoiceDate:MM/dd/yyyy}";
    }

    private static string GetStandalonePurchaseReference(JobMaterialPurchase purchase)
    {
        if (!string.IsNullOrWhiteSpace(purchase.ReceiptNumber))
        {
            return purchase.ReceiptNumber.Trim();
        }

        if (!string.IsNullOrWhiteSpace(purchase.ReferenceNumber))
        {
            return purchase.ReferenceNumber.Trim();
        }

        return "Purchase";
    }

    private static string BuildCommitmentInvoiceLabel(CommitmentRecord commitment)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(commitment.InvoiceNumber))
        {
            parts.Add(commitment.InvoiceNumber.Trim());
        }

        parts.Add(commitment.InvoiceDate.ToString("d"));
        parts.Add($"Due {commitment.DueDate:d}");
        return string.Join(" | ", parts);
    }

    private static decimal GetChangeOrderActualCost(JobRecord job, ChangeOrderRecord changeOrder) =>
        JobFinancialMath.GetTrackedChangeOrderActualCost(job, changeOrder.Id);

    private static string GetChangeOrderLabel(ChangeOrderRecord changeOrder) =>
        string.IsNullOrWhiteSpace(changeOrder.Title) ? "Change Order" : changeOrder.Title.Trim();

    private static int GetTrackedDeviceSortOrder(string? categoryCode) =>
        JobCostCodes.Normalize(categoryCode) switch
        {
            JobCostCodes.Admin => 0,
            JobCostCodes.Engineering => 1,
            JobCostCodes.Components => 2,
            JobCostCodes.Wire => 3,
            JobCostCodes.Material => 4,
            JobCostCodes.Demo => 5,
            JobCostCodes.Install => 6,
            JobCostCodes.Trim => 7,
            JobCostCodes.Test => 8,
            _ => 9
        };

    private static int GetScheduleValueSortOrder(ScheduleValueItem item) =>
        JobCostCodes.Normalize(item.CategoryCode) switch
        {
            JobCostCodes.Admin => 0,
            JobCostCodes.Engineering => 1,
            JobCostCodes.AdminEngineering => 2,
            JobCostCodes.Materials => 3,
            JobCostCodes.Install => 4,
            JobCostCodes.Demo => 5,
            JobCostCodes.Trim => 6,
            JobCostCodes.Test => 7,
            JobCostCodes.ChangeOrder => 8,
            _ => 9
        };

    private static string GetTrackedDeviceTypeLabel(string? sourceSection, string? categoryCode) =>
        string.IsNullOrWhiteSpace(sourceSection)
            ? JobCostCodes.GetLabel(categoryCode ?? JobCostCodes.Other)
            : sourceSection.Trim();

    private static string GetDisplayJobNumber(JobRecord job) =>
        string.IsNullOrWhiteSpace(job.JobNumber) ? "Job Number Pending" : job.JobNumber.Trim();

    private static string GetDisplayProjectName(JobRecord job) =>
        string.IsNullOrWhiteSpace(job.ProjectName) ? "Untitled Project" : job.ProjectName.Trim();

    private static string GetDisplayStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) ? "Status Not Set" : status.Trim();

    private static string GetDisplaySiteName(SiteInformation site) =>
        string.IsNullOrWhiteSpace(site.SiteName) ? "Site To Be Confirmed" : site.SiteName.Trim();

    private static string GetDisplayProjectAddress(SiteInformation site)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(site.AddressLine1))
        {
            lines.Add(site.AddressLine1.Trim());
        }

        if (!string.IsNullOrWhiteSpace(site.AddressLine2))
        {
            lines.Add(site.AddressLine2.Trim());
        }

        var cityStatePostalParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(site.City))
        {
            cityStatePostalParts.Add(site.City.Trim());
        }

        if (!string.IsNullOrWhiteSpace(site.State))
        {
            cityStatePostalParts.Add(site.State.Trim());
        }

        var cityStatePostal = string.Join(", ", cityStatePostalParts);
        if (!string.IsNullOrWhiteSpace(site.PostalCode))
        {
            cityStatePostal = string.IsNullOrWhiteSpace(cityStatePostal)
                ? site.PostalCode.Trim()
                : $"{cityStatePostal} {site.PostalCode.Trim()}";
        }

        if (!string.IsNullOrWhiteSpace(cityStatePostal))
        {
            lines.Add(cityStatePostal);
        }

        return lines.Count == 0 ? "Project address to be confirmed" : string.Join(", ", lines);
    }

    private static decimal GetPercentOfValue(decimal numerator, decimal denominator)
    {
        var safeDenominator = EstimateMath.RoundCurrency(denominator);
        return safeDenominator <= 0m
            ? 0m
            : EstimateMath.RoundCurrency(numerator / safeDenominator);
    }

    //******************************//
    //******* Text Helpers *********//
    //******************************//

    private static string BuildRichTextMarkup(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return """<p>Not provided.</p>""";
        }

        var normalizedContent = content.Replace("\r\n", "\n").Trim();
        var paragraphs = normalizedContent.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var builder = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            var lines = paragraph
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (lines.Count == 0)
            {
                continue;
            }

            if (lines.All(IsBulletLine))
            {
                builder.AppendLine("<ul>");

                foreach (var line in lines)
                {
                    builder.AppendLine($"    <li>{Encode(RemoveBulletPrefix(line))}</li>");
                }

                builder.AppendLine("</ul>");
                continue;
            }

            builder.AppendLine($"""<p>{string.Join("<br />", lines.Select(Encode))}</p>""");
        }

        return builder.Length == 0 ? """<p>Not provided.</p>""" : builder.ToString().Trim();
    }

    private static bool IsBulletLine(string line) =>
        line.StartsWith("- ", StringComparison.Ordinal) ||
        line.StartsWith("* ", StringComparison.Ordinal);

    private static string RemoveBulletPrefix(string line) =>
        line.Length > 2 ? line[2..].Trim() : line.Trim();

    private static string FormatQuantity(decimal quantity) =>
        Math.Round(quantity, 2, MidpointRounding.AwayFromZero).ToString("0.##");

    private static string Encode(string? value) =>
        WebUtility.HtmlEncode(value ?? string.Empty);

    //******************************//
    //******** Styles **************//
    //******************************//

    private static string BuildStyles() =>
        """
        :root {
            color-scheme: light;
            --report-ink: #1a232c;
            --report-muted: #5d6a76;
            --report-line: #d6dde5;
            --report-line-strong: #b9c5d0;
            --report-surface: #f5f8fb;
            --report-surface-accent: #fff5ee;
            --report-page: #ffffff;
            --report-accent: #b94d19;
            --report-accent-soft: #fff2e8;
            --report-success: #2d8f64;
            --report-success-soft: #edf8f2;
            --report-shadow: 0 24px 60px rgba(26, 35, 44, 0.14);
        }

        * {
            box-sizing: border-box;
        }

        body {
            margin: 0;
            padding: 24px;
            background: linear-gradient(180deg, #e8eef4 0%, #f7f9fc 100%);
            color: var(--report-ink);
            font: 14px/1.45 "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
        }

        a {
            color: inherit;
        }

        .report-document {
            display: grid;
            gap: 24px;
        }

        .report-page {
            width: min(8.5in, 100%);
            margin: 0 auto;
            background: var(--report-page);
            border: 1px solid var(--report-line);
            box-shadow: var(--report-shadow);
        }

        .report-page-shell {
            display: grid;
            gap: 20px;
            padding: 0.58in 0.66in 0.54in;
        }

        .report-page-break {
            break-before: page;
            page-break-before: always;
        }

        .report-cover-page {
            min-height: 11in;
        }

        .report-branding-panel {
            display: grid;
            gap: 8px;
        }

        .report-logo-image {
            display: block;
            width: auto;
            max-width: 100%;
            max-height: 80px;
            object-fit: contain;
            object-position: left center;
        }

        .report-branding-company-name {
            font-size: 0.98rem;
            font-weight: 700;
        }

        .report-branding-details {
            margin: 0;
            color: var(--report-muted);
            font-size: 0.82rem;
            line-height: 1.45;
        }

        .report-branding-divider {
            padding: 0 0.36rem;
        }

        .report-cover-header,
        .report-page-header {
            display: grid;
            grid-template-columns: minmax(0, 1fr) auto;
            gap: 22px;
            align-items: start;
            padding-bottom: 18px;
            border-bottom: 2px solid var(--report-ink);
        }

        .report-cover-title {
            margin: 0;
            font-size: 2rem;
            line-height: 1.08;
        }

        .report-page-title {
            margin: 0;
            font-size: 1.55rem;
            line-height: 1.12;
        }

        .report-eyebrow {
            margin: 0 0 6px;
            color: var(--report-accent);
            font-size: 0.72rem;
            font-weight: 700;
            letter-spacing: 0.16em;
            text-transform: uppercase;
        }

        .report-reference {
            margin: 8px 0 0;
            color: var(--report-muted);
            font-size: 0.88rem;
            line-height: 1.42;
        }

        .report-emphasis-card {
            min-width: 230px;
            padding: 16px 18px;
            border: 1px solid #f1ccb8;
            background: var(--report-accent-soft);
        }

        .report-emphasis-label {
            display: block;
            margin-bottom: 6px;
            color: var(--report-muted);
            font-size: 0.76rem;
            font-weight: 700;
            letter-spacing: 0.08em;
            text-transform: uppercase;
        }

        .report-emphasis-value {
            display: block;
            color: var(--report-accent);
            font-size: 1.85rem;
            line-height: 1.08;
        }

        .report-emphasis-detail {
            display: block;
            margin-top: 8px;
            color: var(--report-muted);
            font-size: 0.84rem;
        }

        .metric-grid {
            display: grid;
            grid-template-columns: repeat(3, minmax(0, 1fr));
            gap: 12px;
        }

        .metric-card {
            display: grid;
            gap: 6px;
            padding: 15px 16px;
            border: 1px solid var(--report-line);
            background: var(--report-surface);
        }

        .metric-label {
            color: var(--report-muted);
            font-size: 0.75rem;
            font-weight: 700;
            letter-spacing: 0.08em;
            text-transform: uppercase;
        }

        .metric-value {
            font-size: 1.2rem;
            line-height: 1.16;
        }

        .metric-detail {
            color: var(--report-muted);
            font-size: 0.82rem;
            line-height: 1.36;
        }

        .info-grid {
            display: grid;
            gap: 14px;
            grid-template-columns: repeat(2, minmax(0, 1fr));
        }

        .info-grid-overview {
            align-items: start;
        }

        .info-card {
            display: grid;
            gap: 12px;
            padding: 16px 18px;
            border: 1px solid var(--report-line);
            background: #fff;
        }

        .info-card-title,
        .report-section-title {
            margin: 0;
            font-size: 0.9rem;
            font-weight: 700;
            letter-spacing: 0.08em;
            text-transform: uppercase;
        }

        .meta-list {
            display: grid;
            gap: 10px;
        }

        .meta-entry {
            display: grid;
            gap: 2px;
        }

        .meta-label {
            color: var(--report-muted);
            font-size: 0.74rem;
            font-weight: 700;
            letter-spacing: 0.05em;
            text-transform: uppercase;
        }

        .meta-value {
            white-space: pre-wrap;
            word-break: break-word;
        }

        .toc-card {
            display: grid;
            gap: 10px;
        }

        .toc-link {
            padding: 10px 12px;
            border: 1px solid var(--report-line);
            background: var(--report-surface);
            text-decoration: none;
            font-weight: 600;
        }

        .report-section {
            display: grid;
            gap: 12px;
        }

        .report-copy {
            color: var(--report-ink);
            line-height: 1.55;
        }

        .report-copy p,
        .report-copy ul {
            margin: 0;
        }

        .report-copy ul {
            padding-left: 1.2rem;
        }

        .report-table {
            width: 100%;
            border-collapse: collapse;
            border: 1px solid var(--report-line);
            font-size: 0.88rem;
        }

        .report-table th,
        .report-table td {
            padding: 9px 10px;
            border: 1px solid var(--report-line);
            vertical-align: top;
            text-align: left;
            overflow-wrap: break-word;
            word-break: normal;
            hyphens: none;
        }

        .report-table th {
            background: #edf2f7;
            font-size: 0.76rem;
            font-weight: 700;
            letter-spacing: 0.06em;
            text-transform: uppercase;
            white-space: nowrap;
        }

        .report-table-dense {
            font-size: 0.82rem;
        }

        .report-table-dense th,
        .report-table-dense td {
            padding: 7px 8px;
        }

        .report-table tfoot th,
        .report-table tfoot td,
        .totals-row td,
        .totals-row th {
            font-weight: 700;
            background: #f8fafc;
        }

        .report-subrow td {
            background: #fbfcfe;
            color: #3e4c59;
        }

        .subrow-label {
            padding-left: 1.25rem !important;
        }

        .align-right {
            text-align: right !important;
            white-space: nowrap;
        }

        .invoice-card {
            display: grid;
            gap: 12px;
            padding: 14px 16px;
            border: 1px solid var(--report-line);
            background: #fff;
        }

        .invoice-card-header {
            display: grid;
            grid-template-columns: minmax(0, 1fr) auto;
            gap: 16px;
            align-items: start;
        }

        .invoice-card-header h4 {
            margin: 0;
            font-size: 1rem;
        }

        .invoice-card-meta {
            margin: 6px 0 0;
            color: var(--report-muted);
            font-size: 0.84rem;
        }

        .invoice-card-totals {
            display: grid;
            gap: 4px;
            justify-items: end;
            color: var(--report-muted);
            font-size: 0.84rem;
            text-align: right;
        }

        .invoice-linked-table {
            margin-top: 4px;
        }

        .empty-state-copy {
            margin: 0;
            padding: 12px 14px;
            border: 1px dashed var(--report-line-strong);
            background: #fbfcfe;
            color: var(--report-muted);
        }

        .compact-copy {
            padding: 8px 10px;
        }

        .report-footer {
            padding-top: 12px;
            border-top: 1px solid var(--report-line);
            color: var(--report-muted);
            font-size: 0.78rem;
            text-align: right;
        }

        .break-avoid,
        .metric-card,
        .info-card,
        .invoice-card,
        .report-table tr {
            break-inside: avoid;
            page-break-inside: avoid;
        }

        @media print {
            @page {
                size: letter;
                margin: 0.46in 0.42in 0.4in;
            }

            html,
            body {
                margin: 0;
                padding: 0;
                background: #fff;
            }

            .report-document {
                display: block;
            }

            .report-page {
                display: block;
                width: auto;
                min-height: auto;
                margin: 0;
                border: none;
                box-shadow: none;
                overflow: visible;
            }

            .report-page:not(:last-child) {
                break-after: auto;
                page-break-after: auto;
            }

            .report-page-shell {
                display: block;
                min-height: 0;
                padding: 0;
                -webkit-box-decoration-break: clone;
                box-decoration-break: clone;
            }

            .report-page-shell > * + * {
                margin-top: 18px;
            }

            .report-page-break {
                break-before: page;
                page-break-before: always;
            }

            .report-cover-page {
                min-height: auto;
                break-before: auto;
                page-break-before: auto;
            }

            .report-cover-header,
            .report-page-header,
            .metric-grid,
            .info-grid,
            .report-section-title {
                break-inside: avoid;
                page-break-inside: avoid;
            }

            .report-section-title,
            .report-page-header,
            .report-cover-header,
            .report-footer {
                break-after: avoid;
                page-break-after: avoid;
            }

            .invoice-card,
            .report-section {
                break-inside: auto;
                page-break-inside: auto;
            }

            .report-table {
                font-size: 0.76rem;
            }

            .report-table-dense {
                font-size: 0.71rem;
            }

            .report-table th,
            .report-table td {
                padding: 5px 6px;
            }

            .report-table-dense th,
            .report-table-dense td {
                padding: 4px 5px;
            }

            .report-table,
            .invoice-linked-table {
                page-break-inside: auto;
            }

            .report-table thead {
                display: table-header-group;
            }

            .report-table tfoot {
                display: table-row-group;
            }

            .invoice-card {
                break-inside: avoid-page;
                page-break-inside: avoid;
            }

            .invoice-card-header {
                grid-template-columns: minmax(0, 1fr);
                gap: 10px;
            }

            .invoice-card-totals {
                justify-items: start;
                text-align: left;
            }

            a {
                text-decoration: none;
            }
        }
        """;

    //******************************//
    //******** Report Rows *********//
    //******************************//

    private sealed record InvoiceLinkedLine(
        string Scope,
        string Type,
        string Description,
        decimal Quantity,
        decimal ActualCost);

    private sealed record TrackedMaterialLine(
        string Scope,
        string Type,
        string Description,
        decimal Quantity,
        string InvoiceReference,
        decimal ActualCost,
        string Notes);
}

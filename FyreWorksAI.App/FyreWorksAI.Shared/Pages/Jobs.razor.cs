using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace FyreWorksAI.Shared.Pages;

//******************************//
//******** Jobs******************//
//******************************//
public partial class Jobs : IDisposable
{
    private const string PageSectionNavigationOwnerKey = "jobs";
    private const string ProjectContactsSectionId = "project-contacts";
    private const string ClientFacingSectionId = "client-facing";
    private const string BaselineReferenceSectionId = "baseline-reference";
    private const string ScheduleOfValuesSectionId = "schedule-of-values";
    private const string TimeEntriesSectionId = "time-entries";
    private const string BidDevicesSectionId = "bid-devices";
    private const string JobDevicesSectionId = "job-devices";
    private const string InvoicesSectionId = "invoices";
    private const string ChangeOrdersSectionId = "change-orders";
    private const string CommitmentsSectionId = "commitments";
    private const string ProjectNotesSectionId = "project-notes";
    private const string ProjectContactsElementId = "jobs-project-contacts-section";
    private const string ClientFacingElementId = "jobs-client-facing-section";
    private const string BaselineReferenceElementId = "jobs-baseline-reference-section";
    private const string ScheduleOfValuesElementId = "jobs-schedule-of-values-section";
    private const string TimeEntriesElementId = "jobs-time-entries-section";
    private const string BidDevicesElementId = "jobs-bid-devices-section";
    private const string JobDevicesElementId = "jobs-job-devices-section";
    private const string InvoicesElementId = "jobs-invoices-section";
    private const string ChangeOrdersElementId = "jobs-change-orders-section";
    private const string CommitmentsElementId = "jobs-commitments-section";
    private const string ProjectNotesElementId = "jobs-project-notes-section";

    [SupplyParameterFromQuery(Name = "selected")]
    public Guid? RequestedJobId { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private PageSectionNavigationState PageSectionNavigationState { get; set; } = default!;

    private static readonly IReadOnlyList<PageSectionNavigationItem> JobPageSectionNavigationItems =
    [
        new(ProjectContactsSectionId, ProjectContactsElementId, "Project Contacts"),
        new(ClientFacingSectionId, ClientFacingElementId, "Exclusions + Proposal"),
        new(BaselineReferenceSectionId, BaselineReferenceElementId, "Baseline Reference"),
        new(ScheduleOfValuesSectionId, ScheduleOfValuesElementId, "Schedule Of Values"),
        new(TimeEntriesSectionId, TimeEntriesElementId, "Time Entries"),
        new(BidDevicesSectionId, BidDevicesElementId, "Bid Devices"),
        new(JobDevicesSectionId, JobDevicesElementId, "Job Devices"),
        new(InvoicesSectionId, InvoicesElementId, "Invoices"),
        new(ChangeOrdersSectionId, ChangeOrdersElementId, "Change Orders"),
        new(CommitmentsSectionId, CommitmentsElementId, "Commitments"),
        new(ProjectNotesSectionId, ProjectNotesElementId, "Notes")
    ];

    private Guid? SelectedJobId { get; set; }
    private string StatusMessage { get; set; } = string.Empty;
    private string? ActiveMainSectionId { get; set; }
    private string? PendingSectionElementId { get; set; }
    private static readonly IReadOnlyList<string> TimeEntryLaborClasses =
    [
        nameof(PersonnelType.Journeyman),
        nameof(PersonnelType.Apprentice),
        JobCostCodes.Admin,
        JobCostCodes.Engineering
    ];
    private static readonly IReadOnlyList<string> BaselineOfficePhaseCodes =
    [
        JobCostCodes.Admin,
        JobCostCodes.Engineering
    ];
    private static readonly IReadOnlyList<string> BaselineFieldPhaseCodes =
    [
        JobCostCodes.Install,
        JobCostCodes.Demo,
        JobCostCodes.Trim,
        JobCostCodes.Test
    ];
    private static readonly IReadOnlyList<string> BaselinePhaseCodes =
    [
        JobCostCodes.Admin,
        JobCostCodes.Engineering,
        JobCostCodes.Install,
        JobCostCodes.Demo,
        JobCostCodes.Trim,
        JobCostCodes.Test
    ];
    private HashSet<string> ExpandedSectionIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<Guid> ExpandedInvoiceIds { get; } = [];
    private HashSet<Guid> ExpandedChangeOrderIds { get; } = [];
    private HashSet<Guid> ExpandedScheduleValueIds { get; } = [];
    private bool IsDirectoryPanelExpanded { get; set; }

    private JobRecord? SelectedJob =>
        SelectedJobId is null
            ? null
            : Store.Workspace.Jobs.FirstOrDefault(job => job.Id == SelectedJobId.Value);

    private ClientRecord? CurrentClient =>
        SelectedJob is null
            ? null
            : Store.GetClient(SelectedJob.ClientId);

    private BidRecord? CurrentSourceBid =>
        SelectedJob is null || SelectedJob.SourceBidId is null
            ? null
            : Store.Workspace.Bids.FirstOrDefault(bid => bid.Id == SelectedJob.SourceBidId.Value);

    private LaborTemplate? CurrentJobTemplate =>
        SelectedJob is null
            ? null
            : ResolveJobTemplate(SelectedJob);

    protected override async Task OnInitializedAsync()
    {
        await Store.InitializeAsync();
        ApplySelection();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (string.IsNullOrWhiteSpace(PendingSectionElementId))
        {
            return;
        }

        var sectionElementId = PendingSectionElementId;
        PendingSectionElementId = null;
        await JsRuntime.InvokeVoidAsync("fyreWorksPageSectionNavigation.scrollToSection", sectionElementId);
    }

    protected override void OnParametersSet()
    {
        if (Store.IsInitialized)
        {
            ApplySelection();
        }
    }

    private void ApplySelection()
    {
        if (RequestedJobId is not null && Store.Workspace.Jobs.Any(job => job.Id == RequestedJobId.Value))
        {
            SelectedJobId = RequestedJobId;
        }
        else if (SelectedJobId is null || Store.Workspace.Jobs.All(job => job.Id != SelectedJobId.Value))
        {
            SelectedJobId = Store.Workspace.Jobs.FirstOrDefault()?.Id;
        }

        if (SelectedJob is not null)
        {
            Store.SyncJobFinancials(SelectedJob);
            ExpandedInvoiceIds.RemoveWhere(invoiceId => SelectedJob.Invoices.All(item => item.Id != invoiceId));
            ExpandedChangeOrderIds.RemoveWhere(changeOrderId => SelectedJob.ChangeOrders.All(item => item.Id != changeOrderId));
            ExpandedScheduleValueIds.RemoveWhere(scheduleValueId => SelectedJob.ScheduleOfValues.All(item => item.Id != scheduleValueId));
            foreach (var entry in SelectedJob.TimeEntries)
            {
                NormalizeTimeEntry(entry);
            }
        }

        if (SelectedJob is null)
        {
            ActiveMainSectionId = null;
            ExpandedInvoiceIds.Clear();
            ExpandedChangeOrderIds.Clear();
            ExpandedScheduleValueIds.Clear();
        }

        RefreshPageSectionNavigation();
    }

    private void SelectJob(Guid jobId)
    {
        SelectedJobId = jobId;
        CollapseAllSections();
        ExpandedInvoiceIds.Clear();
        ExpandedChangeOrderIds.Clear();
        ExpandedScheduleValueIds.Clear();
        CloseDirectoryPanel();
        PendingSectionElementId = null;
        StatusMessage = string.Empty;
        RefreshPageSectionNavigation();
        NavigationManager.NavigateTo($"/jobs?selected={jobId}", replace: true);
    }

    private void NavigateToBid(Guid bidId) =>
        NavigationManager.NavigateTo($"/bids?selected={bidId}");

    private async Task CreateJobAsync()
    {
        var job = Store.CreateBlankJob();
        SelectedJobId = job.Id;
        CollapseAllSections();
        CloseDirectoryPanel();
        PendingSectionElementId = null;
        StatusMessage = "New job created.";
        RefreshPageSectionNavigation();
        await Store.SaveAsync();
        NavigationManager.NavigateTo($"/jobs?selected={job.Id}", replace: true);
    }

    private void CreateClientForJob()
    {
        if (SelectedJob is null)
        {
            return;
        }

        var client = Store.CreateClient();
        SelectedJob.ClientId = client.Id;
        StatusMessage = "New client linked to the job.";
    }

    private async Task SaveAsync()
    {
        if (SelectedJob is not null)
        {
            Store.SyncJobFinancials(SelectedJob);
        }

        await Store.SaveAsync();
        StatusMessage = "Job saved.";
    }

    private async Task DeleteJobAsync()
    {
        if (SelectedJob is null)
        {
            return;
        }

        var deletedJobId = SelectedJob.Id;
        if (!Store.DeleteJob(deletedJobId))
        {
            return;
        }

        SelectedJobId = GetNextJobId();
        CollapseAllSections();
        PendingSectionElementId = null;
        RefreshPageSectionNavigation();
        await Store.SaveAsync();
        StatusMessage = "Job deleted.";
        NavigationManager.NavigateTo(
            SelectedJobId is null ? "/jobs" : $"/jobs?selected={SelectedJobId}",
            replace: true);
    }

    private async Task ExportAsync()
    {
        if (SelectedJob is null)
        {
            return;
        }

        Store.SyncJobFinancials(SelectedJob);
        var path = await Store.ExportJobCostReportAsync(SelectedJob);
        await Store.SaveAsync();
        StatusMessage = $"Job cost export created at {path}.";
    }

    private async Task OnClientChanged(ChangeEventArgs args)
    {
        if (SelectedJob is null)
        {
            return;
        }

        SelectedJob.ClientId = ParseNullableGuid(args.Value?.ToString());
        await Task.CompletedTask;
    }

    private Guid? GetNextJobId() =>
        Store.Workspace.Jobs
            .Where(job => job.IsActive)
            .OrderBy(job => job.ProjectName)
            .Select(job => (Guid?)job.Id)
            .Concat(
                Store.Workspace.Jobs
                    .Where(job => !job.IsActive)
                    .OrderByDescending(job => job.CreatedOn)
                    .Select(job => (Guid?)job.Id))
            .FirstOrDefault();

    private decimal GetBaselineCostForCode(string costCode) =>
        JobCostCodes.Normalize(costCode) switch
        {
            JobCostCodes.Admin => SelectedJob?.Baseline.EstimatedAdminCost ?? 0m,
            JobCostCodes.Engineering => SelectedJob?.Baseline.EstimatedEngineeringCost ?? 0m,
            JobCostCodes.Install => SelectedJob?.Baseline.EstimatedInstallCost ?? 0m,
            JobCostCodes.Demo => SelectedJob?.Baseline.EstimatedDemoCost ?? 0m,
            JobCostCodes.Trim => SelectedJob?.Baseline.EstimatedTrimCost ?? 0m,
            JobCostCodes.Test => SelectedJob?.Baseline.EstimatedTestCost ?? 0m,
            _ => 0m
        };

    private decimal GetActualCostForCode(string costCode) =>
        SelectedJob is null
            ? 0m
            : JobFinancialMath.GetJobActualCost(SelectedJob, costCode);

    private decimal GetBaselineSaleForCode(string costCode) =>
        JobCostCodes.Normalize(costCode) switch
        {
            JobCostCodes.Admin => SelectedJob?.Baseline.EstimatedAdminSale ?? 0m,
            JobCostCodes.Engineering => SelectedJob?.Baseline.EstimatedEngineeringSale ?? 0m,
            JobCostCodes.Install => SelectedJob?.Baseline.EstimatedInstallSale ?? 0m,
            JobCostCodes.Demo => SelectedJob?.Baseline.EstimatedDemoSale ?? 0m,
            JobCostCodes.Trim => SelectedJob?.Baseline.EstimatedTrimSale ?? 0m,
            JobCostCodes.Test => SelectedJob?.Baseline.EstimatedTestSale ?? 0m,
            _ => 0m
        };

    private decimal GetBaselineHoursTotal(IEnumerable<string> costCodes) =>
        SelectedJob is null
            ? 0m
            : EstimateMath.RoundHours(costCodes.Sum(costCode => JobFinancialMath.GetBaselineHours(SelectedJob.Baseline, costCode)));

    private decimal GetActualHoursTotal(IEnumerable<string> costCodes) =>
        SelectedJob is null
            ? 0m
            : EstimateMath.RoundHours(costCodes.Sum(costCode => JobFinancialMath.GetJobActualHours(SelectedJob, costCode)));

    private decimal GetRemainingHoursTotal(IEnumerable<string> costCodes) =>
        EstimateMath.RoundHours(GetBaselineHoursTotal(costCodes) - GetActualHoursTotal(costCodes));

    private decimal GetBaselineCostTotal(IEnumerable<string> costCodes) =>
        EstimateMath.RoundCurrency(costCodes.Sum(GetBaselineCostForCode));

    private decimal GetBaselineSaleTotal(IEnumerable<string> costCodes) =>
        EstimateMath.RoundCurrency(costCodes.Sum(GetBaselineSaleForCode));

    private decimal GetActualCostTotal(IEnumerable<string> costCodes) =>
        EstimateMath.RoundCurrency(costCodes.Sum(GetActualCostForCode));

    private decimal GetBaselineTotalHours() =>
        GetBaselineHoursTotal(BaselinePhaseCodes);

    private int GetBidComponentCount() =>
        SelectedJob?.Baseline.Components.Count ?? 0;

    private decimal GetBidDeviceQuantityTotal(IEnumerable<JobBaselineLineItem> items) =>
        Math.Round(items.Sum(item => item.Quantity), 2, MidpointRounding.AwayFromZero);

    private decimal GetBidDeviceHoursTotal(IEnumerable<JobBaselineLineItem> items) =>
        EstimateMath.RoundHours(items.Sum(item => item.EstimatedHours));

    private decimal GetBidDeviceEstimatedCostTotal(IEnumerable<JobBaselineLineItem> items) =>
        EstimateMath.RoundCurrency(items.Sum(item => item.EstimatedCost));

    private decimal GetBidDeviceEstimatedSaleTotal(IEnumerable<JobBaselineLineItem> items) =>
        EstimateMath.RoundCurrency(items.Sum(item => item.EstimatedSale));

    private decimal GetBidDeviceActualCostTotal(IEnumerable<JobBaselineLineItem> items) =>
        EstimateMath.RoundCurrency(items.Sum(item => item.ActualCost));

    private decimal GetBidDeviceCostVarianceTotal(IEnumerable<JobBaselineLineItem> items) =>
        EstimateMath.RoundCurrency(GetBidDeviceActualCostTotal(items) - GetBidDeviceEstimatedCostTotal(items));

    private decimal GetBidDeviceSaleVarianceTotal(IEnumerable<JobBaselineLineItem> items) =>
        EstimateMath.RoundCurrency(GetBidDeviceEstimatedSaleTotal(items) - GetBidDeviceActualCostTotal(items));

    private string GetBidDeviceTypeLabel(JobBaselineLineItem item) =>
        string.IsNullOrWhiteSpace(item.SourceSection)
            ? JobCostCodes.GetLabel(item.CategoryCode)
            : item.SourceSection;

    private int GetDeviceSortOrder(string? categoryCode) =>
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

    private int GetScheduleValueSortOrder(ScheduleValueItem item) =>
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

    private Task OnPageSectionNavigationRequestedAsync(PageSectionNavigationItem item)
    {
        CollapseAllSections();
        ExpandSection(item.SectionId);
        PendingSectionElementId = item.ElementId;
        return InvokeAsync(StateHasChanged);
    }

    private void RefreshPageSectionNavigation()
    {
        if (SelectedJob is null)
        {
            PageSectionNavigationState.Clear(PageSectionNavigationOwnerKey);
            return;
        }

        PageSectionNavigationState.Configure(
            PageSectionNavigationOwnerKey,
            JobPageSectionNavigationItems,
            OnPageSectionNavigationRequestedAsync,
            ActiveMainSectionId);
    }

    private void SetActiveMainSection(string? sectionId)
    {
        ActiveMainSectionId = sectionId;
        PageSectionNavigationState.SetActiveSection(PageSectionNavigationOwnerKey, sectionId);
    }

    private void CollapseAllSections()
    {
        ExpandedSectionIds.Clear();
        SetActiveMainSection(null);
    }

    private void ExpandSection(string sectionId)
    {
        ExpandedSectionIds.Add(sectionId);
        SetActiveMainSection(sectionId);
    }

    private void ToggleSection(string sectionId)
    {
        if (!ExpandedSectionIds.Add(sectionId))
        {
            ExpandedSectionIds.Remove(sectionId);
            if (string.Equals(ActiveMainSectionId, sectionId, StringComparison.OrdinalIgnoreCase))
            {
                SetActiveMainSection(null);
            }

            return;
        }

        SetActiveMainSection(sectionId);
    }

    private void ToggleDirectoryPanel() =>
        IsDirectoryPanelExpanded = !IsDirectoryPanelExpanded;

    private void CloseDirectoryPanel() =>
        IsDirectoryPanelExpanded = false;

    private bool IsSectionExpanded(string sectionId) =>
        ExpandedSectionIds.Contains(sectionId);

    private string GetInvoiceLabel(JobInvoiceRecord invoice)
    {
        if (!string.IsNullOrWhiteSpace(invoice.ReferenceNumber))
        {
            return invoice.ReferenceNumber;
        }

        if (!string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            return invoice.InvoiceNumber;
        }

        if (!string.IsNullOrWhiteSpace(invoice.Vendor))
        {
            return $"{invoice.Vendor} {invoice.InvoiceDate:MM/dd/yyyy}";
        }

        return $"Invoice {invoice.InvoiceDate:MM/dd/yyyy}";
    }

    private void ToggleInvoiceExpanded(Guid invoiceId)
    {
        if (!ExpandedInvoiceIds.Add(invoiceId))
        {
            ExpandedInvoiceIds.Remove(invoiceId);
        }
    }

    private bool IsInvoiceExpanded(Guid invoiceId) =>
        ExpandedInvoiceIds.Contains(invoiceId);

    private string GetChangeOrderLabel(ChangeOrderRecord changeOrder) =>
        string.IsNullOrWhiteSpace(changeOrder.Title) ? "Change Order" : changeOrder.Title.Trim();

    private void ToggleChangeOrderExpanded(Guid changeOrderId)
    {
        if (!ExpandedChangeOrderIds.Add(changeOrderId))
        {
            ExpandedChangeOrderIds.Remove(changeOrderId);
        }
    }

    private bool IsChangeOrderExpanded(Guid changeOrderId) =>
        ExpandedChangeOrderIds.Contains(changeOrderId);

    private LaborTemplate? ResolveJobTemplate(JobRecord job)
    {
        if (job.SourceBidId is not null)
        {
            var sourceBid = Store.Workspace.Bids.FirstOrDefault(item => item.Id == job.SourceBidId.Value);
            var sourceTemplate = Store.GetTemplate(sourceBid?.TemplateId);
            if (sourceTemplate is not null)
            {
                return sourceTemplate;
            }
        }

        return Store.GetTemplate(Store.Workspace.Settings.DefaultTemplateId)
            ?? Store.Workspace.Templates.FirstOrDefault(template => !template.IsArchived)
            ?? Store.Workspace.Templates.FirstOrDefault();
    }

    private string GetTimeEntryLaborLabel(string laborClass) =>
        NormalizeTimeEntryLaborClass(laborClass) switch
        {
            JobCostCodes.Admin => "Administrative",
            JobCostCodes.Engineering => "Engineering",
            _ => NormalizeTimeEntryLaborClass(laborClass)
        };

    private string NormalizeTimeEntryLaborClass(string? value, string? costCode = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GetDefaultLaborClassForCostCode(costCode);
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "journeyman" or "technician" => nameof(PersonnelType.Journeyman),
            "apprentice" => nameof(PersonnelType.Apprentice),
            "admin" or "administrative" => JobCostCodes.Admin,
            "engineering" => JobCostCodes.Engineering,
            _ => GetDefaultLaborClassForCostCode(costCode)
        };
    }

    private string GetDefaultLaborClassForCostCode(string? costCode) =>
        JobCostCodes.Normalize(costCode) switch
        {
            JobCostCodes.Admin => JobCostCodes.Admin,
            JobCostCodes.Engineering => JobCostCodes.Engineering,
            _ => nameof(PersonnelType.Journeyman)
        };

    private bool IsTimeEntryOvernightLocked(JobTimeEntry entry) =>
        NormalizeTimeEntryLaborClass(entry.CrewMember, entry.CostCode) is JobCostCodes.Admin or JobCostCodes.Engineering;

    private static bool IsFieldTimeEntryCostCode(string? costCode) =>
        JobCostCodes.Normalize(costCode) is JobCostCodes.Install or JobCostCodes.Demo or JobCostCodes.Trim or JobCostCodes.Test;

    private static bool IsOfficeTimeEntryCostCode(string? costCode) =>
        JobCostCodes.Normalize(costCode) is JobCostCodes.Admin or JobCostCodes.Engineering;

    private decimal GetDefaultTimeEntryRate(JobTimeEntry entry)
    {
        var template = CurrentJobTemplate;
        var laborClass = NormalizeTimeEntryLaborClass(entry.CrewMember, entry.CostCode);

        return laborClass switch
        {
            nameof(PersonnelType.Journeyman) => entry.IsOvernight
                ? template?.JourneymanOvernightBilledRate ?? EstimateMath.RoundCurrency(Store.Workspace.Settings.FieldLaborRate * 1.5m)
                : template?.JourneymanRegularBilledRate ?? Store.Workspace.Settings.FieldLaborRate,
            nameof(PersonnelType.Apprentice) => entry.IsOvernight
                ? template?.ApprenticeOvernightBilledRate ?? EstimateMath.RoundCurrency(Store.Workspace.Settings.FieldLaborRate * 1.5m)
                : template?.ApprenticeRegularBilledRate ?? Store.Workspace.Settings.FieldLaborRate,
            JobCostCodes.Admin => template?.AdminBilledRate ?? Store.Workspace.Settings.AdminLaborRate,
            JobCostCodes.Engineering => template?.EngineeringBilledRate ?? Store.Workspace.Settings.EngineeringLaborRate,
            _ => template?.JourneymanRegularBilledRate ?? Store.Workspace.Settings.FieldLaborRate
        };
    }

    private void ApplyTimeEntryTemplateRate(JobTimeEntry entry)
    {
        entry.CrewMember = NormalizeTimeEntryLaborClass(entry.CrewMember, entry.CostCode);
        if (IsTimeEntryOvernightLocked(entry))
        {
            entry.IsOvernight = false;
        }

        entry.HourlyRate = EstimateMath.RoundCurrency(Math.Max(0m, GetDefaultTimeEntryRate(entry)));
    }

    private void OnTimeEntryLaborTypeChanged(JobTimeEntry entry) =>
        ApplyTimeEntryTemplateRate(entry);

    private void OnTimeEntryCostCodeChanged(JobTimeEntry entry)
    {
        entry.CostCode = JobCostCodes.Normalize(entry.CostCode);
        entry.Hours = IsFieldTimeEntryCostCode(entry.CostCode)
            ? GetTimeEntryAvailableHours(entry)
            : 0m;
        NormalizeTimeEntry(entry);
    }

    private void OnTimeEntryOvernightChanged(JobTimeEntry entry)
    {
        if (IsTimeEntryOvernightLocked(entry))
        {
            entry.IsOvernight = false;
        }

        ApplyTimeEntryTemplateRate(entry);
    }

    private void AddTimeEntry()
    {
        if (SelectedJob is null)
        {
            return;
        }

        var entry = new JobTimeEntry
        {
            CrewMember = nameof(PersonnelType.Journeyman),
            CostCode = JobCostCodes.Install
        };

        entry.Hours = GetTimeEntryAvailableHours(entry);
        ApplyTimeEntryTemplateRate(entry);
        SelectedJob.TimeEntries.Add(entry);
        ExpandSection(TimeEntriesSectionId);
    }

    private void RemoveTimeEntry(Guid entryId) =>
        SelectedJob?.TimeEntries.RemoveAll(entry => entry.Id == entryId);

    private void NormalizeTimeEntry(JobTimeEntry entry)
    {
        entry.CostCode = JobCostCodes.Normalize(entry.CostCode);
        var normalizedCrewMember = NormalizeTimeEntryLaborClass(entry.CrewMember, entry.CostCode);
        var shouldApplyTemplateRate = entry.HourlyRate <= 0m || !string.Equals(entry.CrewMember, normalizedCrewMember, StringComparison.OrdinalIgnoreCase);
        entry.CrewMember = normalizedCrewMember;
        if (IsTimeEntryOvernightLocked(entry))
        {
            entry.IsOvernight = false;
        }

        entry.Hours = EstimateMath.RoundHours(Math.Max(0m, entry.Hours));
        entry.HourlyRate = EstimateMath.RoundCurrency(Math.Max(0m, entry.HourlyRate));
        if (shouldApplyTemplateRate)
        {
            ApplyTimeEntryTemplateRate(entry);
        }
    }

    private decimal GetTimeEntryAvailableHours(JobTimeEntry entry)
    {
        if (SelectedJob is null || !IsFieldTimeEntryCostCode(entry.CostCode))
        {
            return 0m;
        }

        var baselineHours = JobFinancialMath.GetBaselineHours(SelectedJob.Baseline, entry.CostCode);
        var usedHoursByOtherEntries = EstimateMath.RoundHours(SelectedJob.TimeEntries
            .Where(item => item.Id != entry.Id && JobCostCodes.Normalize(item.CostCode) == JobCostCodes.Normalize(entry.CostCode))
            .Sum(item => item.Hours));

        return EstimateMath.RoundHours(Math.Max(0m, baselineHours - usedHoursByOtherEntries));
    }

    private decimal GetTimeEntryRemainingHours(JobTimeEntry entry) =>
        SelectedJob is null
            ? 0m
            : EstimateMath.RoundHours(
                JobFinancialMath.GetBaselineHours(SelectedJob.Baseline, entry.CostCode) -
                JobFinancialMath.GetJobActualHours(SelectedJob, entry.CostCode));

    private string GetTimeEntryRemainingHoursDisplay(JobTimeEntry entry) =>
        EstimateMath.GetHours(Math.Abs(GetTimeEntryRemainingHours(entry)));

    private string? GetTimeEntryRemainingHoursStyle(JobTimeEntry entry) =>
        GetTimeEntryRemainingHours(entry) < 0m
            ? "color: var(--danger);"
            : null;

    private string GetTimeEntryHoursInputValue(JobTimeEntry entry)
    {
        if (entry.Hours <= 0m && IsOfficeTimeEntryCostCode(entry.CostCode))
        {
            return string.Empty;
        }

        return entry.Hours.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void OnTimeEntryHoursChanged(JobTimeEntry entry, ChangeEventArgs args)
    {
        entry.Hours = ParseHoursValue(args.Value?.ToString());
        NormalizeTimeEntry(entry);
    }

    private void OnBidDeviceActualCostChanged(JobBaselineLineItem item, ChangeEventArgs args) =>
        item.ActualUnitCost = ParseCurrencyValue(args.Value?.ToString());

    private void OnBidDeviceInvoiceChanged(JobBaselineLineItem item, ChangeEventArgs args) =>
        item.InvoiceId = ParseNullableGuid(args.Value?.ToString());

    private void AddJobDevice()
    {
        if (SelectedJob is null)
        {
            return;
        }

        var item = new JobDeviceItem
        {
            Description = "Added Device",
            CategoryCode = JobCostCodes.Material,
            Quantity = 1m
        };

        NormalizeJobDevice(item);
        SelectedJob.JobDevices.Add(item);
        ExpandSection(JobDevicesSectionId);
    }

    private void RemoveJobDevice(Guid itemId) =>
        SelectedJob?.JobDevices.RemoveAll(item => item.Id == itemId);

    private void NormalizeJobDevice(JobDeviceItem item)
    {
        item.CategoryCode = JobCostCodes.Normalize(item.CategoryCode);
        item.Description = string.IsNullOrWhiteSpace(item.Description) ? "Job Device" : item.Description.Trim();
        item.Quantity = item.Quantity <= 0m ? 1m : item.Quantity;
        item.UnitLabel = string.IsNullOrWhiteSpace(item.UnitLabel) ? "ea" : item.UnitLabel.Trim();
        item.EstimatedUnitCost = EstimateMath.RoundCurrency(Math.Max(0m, item.EstimatedUnitCost));
        item.EstimatedUnitSale = EstimateMath.RoundCurrency(Math.Max(0m, item.EstimatedUnitSale));
        item.ActualUnitCost = EstimateMath.RoundCurrency(Math.Max(0m, item.ActualUnitCost));
    }

    private void OnJobDeviceEstimatedCostChanged(JobDeviceItem item, ChangeEventArgs args) =>
        item.EstimatedUnitCost = ParseCurrencyValue(args.Value?.ToString());

    private void OnJobDeviceEstimatedSaleChanged(JobDeviceItem item, ChangeEventArgs args) =>
        item.EstimatedUnitSale = ParseCurrencyValue(args.Value?.ToString());

    private void OnJobDeviceActualCostChanged(JobDeviceItem item, ChangeEventArgs args) =>
        item.ActualUnitCost = ParseCurrencyValue(args.Value?.ToString());

    private void OnJobDeviceInvoiceChanged(JobDeviceItem item, ChangeEventArgs args) =>
        item.InvoiceId = ParseNullableGuid(args.Value?.ToString());

    private void AddInvoice()
    {
        if (SelectedJob is null)
        {
            return;
        }

        var invoice = new JobInvoiceRecord
        {
            InvoiceDate = DateTime.Today
        };

        SelectedJob.Invoices.Add(invoice);
        ExpandSection(InvoicesSectionId);
        ExpandedInvoiceIds.Add(invoice.Id);
    }

    private void NormalizeInvoice(JobInvoiceRecord invoice)
    {
        invoice.ReferenceNumber = invoice.ReferenceNumber?.Trim() ?? string.Empty;
        invoice.Vendor = invoice.Vendor?.Trim() ?? string.Empty;
        invoice.InvoiceNumber = invoice.InvoiceNumber?.Trim() ?? string.Empty;
        invoice.Notes = invoice.Notes?.Trim() ?? string.Empty;
        invoice.InvoiceTotal = EstimateMath.RoundCurrency(Math.Max(0m, invoice.InvoiceTotal));
        invoice.Attachments ??= [];
    }

    private void RemoveInvoice(Guid invoiceId)
    {
        if (SelectedJob is null)
        {
            return;
        }

        var invoice = SelectedJob.Invoices.FirstOrDefault(item => item.Id == invoiceId);
        if (invoice is null)
        {
            return;
        }

        foreach (var attachment in invoice.Attachments.ToList())
        {
            Store.RemoveAttachment(invoice.Attachments, attachment);
        }

        foreach (var item in SelectedJob.Baseline.LineItems.Where(item => item.InvoiceId == invoiceId))
        {
            item.InvoiceId = null;
        }

        foreach (var item in SelectedJob.JobDevices.Where(item => item.InvoiceId == invoiceId))
        {
            item.InvoiceId = null;
        }

        SelectedJob.Invoices.RemoveAll(item => item.Id == invoiceId);
        ExpandedInvoiceIds.Remove(invoiceId);
    }

    private string GetInvoiceAttachmentArea()
    {
        if (SelectedJob is null)
        {
            return "jobs";
        }

        return Path.Combine("jobs", SelectedJob.Id.ToString("N"), "invoices");
    }

    private void AddChangeOrder()
    {
        if (SelectedJob is null)
        {
            return;
        }

        var changeOrder = new ChangeOrderRecord { Title = "Change Order" };
        SelectedJob.ChangeOrders.Add(changeOrder);
        ExpandSection(ChangeOrdersSectionId);
        ExpandedChangeOrderIds.Add(changeOrder.Id);
        SyncJobFinancials();
    }

    private void RemoveChangeOrder(Guid changeOrderId)
    {
        if (SelectedJob is null)
        {
            return;
        }

        var changeOrder = SelectedJob.ChangeOrders.FirstOrDefault(item => item.Id == changeOrderId);
        if (changeOrder is null)
        {
            return;
        }

        foreach (var attachment in changeOrder.Attachments.ToList())
        {
            Store.RemoveAttachment(changeOrder.Attachments, attachment);
        }

        SelectedJob.ChangeOrders.RemoveAll(item => item.Id == changeOrderId);
        ExpandedChangeOrderIds.Remove(changeOrderId);
        SyncJobFinancials();
    }

    private string GetChangeOrderAttachmentArea(Guid changeOrderId)
    {
        if (SelectedJob is null)
        {
            return "jobs";
        }

        return Path.Combine("jobs", SelectedJob.Id.ToString("N"), "change-orders", changeOrderId.ToString("N"));
    }

    private void AddScheduleValue()
    {
        if (SelectedJob is null)
        {
            return;
        }

        var item = new ScheduleValueItem
        {
            Description = JobCostCodes.GetLabel(JobCostCodes.Other),
            CategoryCode = JobCostCodes.Other
        };

        SelectedJob.ScheduleOfValues.Add(item);

        ExpandSection(ScheduleOfValuesSectionId);
        ExpandedScheduleValueIds.Add(item.Id);
        SyncJobFinancials();
    }

    private void RemoveScheduleValue(Guid itemId)
    {
        if (SelectedJob is null)
        {
            return;
        }

        foreach (var commitment in SelectedJob.Commitments.Where(commitment => commitment.ScheduleValueItemId == itemId))
        {
            commitment.ScheduleValueItemId = null;
            commitment.CommittedAmount = 0m;
        }

        SelectedJob.ScheduleOfValues.RemoveAll(item => item.Id == itemId);
        ExpandedScheduleValueIds.Remove(itemId);
        SyncJobFinancials();
    }

    private void ToggleScheduleValueExpanded(Guid itemId)
    {
        if (!ExpandedScheduleValueIds.Add(itemId))
        {
            ExpandedScheduleValueIds.Remove(itemId);
        }
    }

    private bool IsScheduleValueExpanded(Guid itemId) =>
        ExpandedScheduleValueIds.Contains(itemId);

    private void AddScheduleValueSubLine(ScheduleValueItem item)
    {
        if (SelectedJob is null)
        {
            return;
        }

        item.SubLines.Add(new ScheduleValueSubLine
        {
            Description = $"{item.Description} Progress",
            LineValue = item.SubLines.Count == 0
                ? EstimateMath.RoundCurrency(Math.Max(0m, item.ScheduledValue > 0m ? item.ScheduledValue : item.ReferenceValue))
                : 0m
        });

        ExpandedScheduleValueIds.Add(item.Id);
        SyncJobFinancials();
    }

    private void RemoveScheduleValueSubLine(ScheduleValueItem item, Guid subLineId)
    {
        item.SubLines.RemoveAll(subLine => subLine.Id == subLineId);
        SyncJobFinancials();
    }

    private decimal GetScheduleValueReferenceAmount(string categoryCode) =>
        SelectedJob is null
            ? 0m
            : JobFinancialMath.GetBaselineScheduledRevenue(SelectedJob.Baseline, categoryCode);

    private void OnScheduleValueCategoryChanged(ScheduleValueItem item)
    {
        if (SelectedJob is null)
        {
            return;
        }

        var currentDescription = item.Description?.Trim() ?? string.Empty;
        item.CategoryCode = JobCostCodes.Normalize(item.CategoryCode);
        if (string.IsNullOrWhiteSpace(currentDescription) ||
            item.IsAutoGenerated ||
            string.Equals(currentDescription, "Billing Line", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(currentDescription, JobCostCodes.GetLabel(JobCostCodes.Other), StringComparison.OrdinalIgnoreCase))
        {
            item.Description = JobCostCodes.GetLabel(item.CategoryCode);
        }

        if (!item.IsChangeOrderLine)
        {
            item.ReferenceValue = GetScheduleValueReferenceAmount(item.CategoryCode);
        }

        item.IsPercentageManual = false;
        SyncJobFinancials();
    }

    private decimal GetScheduleValueReferenceTotal(IEnumerable<ScheduleValueItem> items) =>
        EstimateMath.RoundCurrency(items.Sum(item => item.ReferenceValue));

    private decimal GetScheduleValuePercentTotal(IEnumerable<ScheduleValueItem> items) =>
        EstimateMath.RoundCurrency(items.Sum(item => item.PercentageOfTotal));

    private decimal GetScheduleValueBilledTotal(IEnumerable<ScheduleValueItem> items) =>
        EstimateMath.RoundCurrency(items.Sum(item => item.BilledToDate));

    private decimal GetScheduleValuePaidTotal(IEnumerable<ScheduleValueItem> items) =>
        EstimateMath.RoundCurrency(items.Sum(item => item.PaidToDate));

    private decimal GetScheduleValuePaidPercentTotal(IEnumerable<ScheduleValueItem> items)
    {
        var scheduledTotal = EstimateMath.RoundCurrency(items.Sum(item => item.ScheduledValue));
        var paidTotal = GetScheduleValuePaidTotal(items);
        return scheduledTotal <= 0m
            ? 0m
            : EstimateMath.RoundCurrency((paidTotal / scheduledTotal) * 100m);
    }

    private string? GetScheduleValuePercentTotalStyle(IEnumerable<ScheduleValueItem> items)
    {
        var totalPercent = GetScheduleValuePercentTotal(items);
        return Math.Abs(totalPercent - 100m) > 0.05m
            ? "color: var(--danger);"
            : null;
    }

    private decimal GetScheduleValueSubLinePaidPercent(ScheduleValueSubLine subLine) =>
        subLine.LineValue <= 0m
            ? 0m
            : EstimateMath.RoundCurrency((subLine.PaidAmount / subLine.LineValue) * 100m);

    private decimal GetLinkedCommitmentPaidPercent(ScheduleValueItem item) =>
        SelectedJob is null
            ? 0m
            : JobFinancialMath.GetLinkedCommitmentPaidPercent(SelectedJob, item.Id, item.ScheduledValue);

    private string GetScheduleValueSubLineCommitmentLabel(Guid? commitmentId)
    {
        if (SelectedJob is null || commitmentId is null)
        {
            return "Not linked";
        }

        var commitment = SelectedJob.Commitments.FirstOrDefault(item => item.Id == commitmentId.Value);
        if (commitment is null)
        {
            return "Not linked";
        }

        if (!string.IsNullOrWhiteSpace(commitment.CommitmentNumber))
        {
            return commitment.CommitmentNumber;
        }

        if (!string.IsNullOrWhiteSpace(commitment.Vendor))
        {
            return commitment.Vendor;
        }

        return string.IsNullOrWhiteSpace(commitment.Description) ? "Linked commitment" : commitment.Description;
    }

    private string GetScheduleValueSubLineLinkLabel(ScheduleValueSubLine subLine) =>
        subLine.LinkedCommitmentId is null
            ? (subLine.IsAutoGenerated ? "Estimated Sale" : "Manual")
            : GetScheduleValueSubLineCommitmentLabel(subLine.LinkedCommitmentId);

    private static string GetCurrencyInputValue(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private void OnScheduleValueSubLineLineValueChanged(ScheduleValueItem item, ScheduleValueSubLine subLine, ChangeEventArgs args)
    {
        subLine.LineValue = ParseCurrencyValue(args.Value?.ToString());
        SyncJobFinancials();
    }

    private ChangeOrderRecord? GetLinkedChangeOrder(ScheduleValueItem item) =>
        SelectedJob is null || item.LinkedChangeOrderId is null
            ? null
            : SelectedJob.ChangeOrders.FirstOrDefault(changeOrder => changeOrder.Id == item.LinkedChangeOrderId.Value);

    private bool IsScheduleValueChangeOrderApproved(ScheduleValueItem item) =>
        !item.IsChangeOrderLine || GetLinkedChangeOrder(item)?.Approved == true;

    private string GetCommitmentScheduleValueLabel(ScheduleValueItem item)
    {
        var categoryLabel = JobCostCodes.GetLabel(item.CategoryCode);
        var description = item.Description?.Trim() ?? string.Empty;

        if (JobCostCodes.Normalize(item.CategoryCode) == JobCostCodes.Other &&
            (string.IsNullOrWhiteSpace(description) ||
             string.Equals(description, "Billing Line", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(description, categoryLabel, StringComparison.OrdinalIgnoreCase)))
        {
            return categoryLabel;
        }

        return string.IsNullOrWhiteSpace(description) ? categoryLabel : description;
    }

    private void AddCommitment()
    {
        if (SelectedJob is null)
        {
            return;
        }

        SelectedJob.Commitments.Add(new CommitmentRecord
        {
            Vendor = "Vendor",
            ScheduleValueItemId = SelectedJob.ScheduleOfValues.FirstOrDefault()?.Id
        });

        ExpandSection(CommitmentsSectionId);
        var firstScheduleValueId = SelectedJob.ScheduleOfValues.FirstOrDefault()?.Id;
        if (firstScheduleValueId is not null)
        {
            ExpandedScheduleValueIds.Add(firstScheduleValueId.Value);
        }
        SyncJobFinancials();
    }

    private void OnCommitmentScheduleValueChanged(CommitmentRecord commitment, ChangeEventArgs args)
    {
        commitment.ScheduleValueItemId = ParseNullableGuid(args.Value?.ToString());
        if (commitment.ScheduleValueItemId is null)
        {
            commitment.CommittedAmount = 0m;
        }
        else
        {
            ExpandedScheduleValueIds.Add(commitment.ScheduleValueItemId.Value);
        }

        SyncJobFinancials();
    }

    private void RemoveCommitment(Guid commitmentId)
    {
        SelectedJob?.Commitments.RemoveAll(commitment => commitment.Id == commitmentId);
        SyncJobFinancials();
    }

    private void SyncJobFinancials()
    {
        if (SelectedJob is null)
        {
            return;
        }

        Store.SyncJobFinancials(SelectedJob);
    }

    private static decimal ParseCurrencyValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        var parsedValue = decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : 0m;
        return EstimateMath.RoundCurrency(Math.Max(0m, parsedValue));
    }

    private static decimal ParseHoursValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        var parsedValue = decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : 0m;
        return EstimateMath.RoundHours(Math.Max(0m, parsedValue));
    }

    private static Guid? ParseNullableGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : null;

    public void Dispose()
    {
        PageSectionNavigationState.Clear(PageSectionNavigationOwnerKey);
    }
}

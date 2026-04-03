using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using FyreWorksAI.Shared.Core.Services.Status;

namespace FyreWorksAI.Shared.Pages;

//******************************//
//******** Service***************//
//******************************//
public partial class Service : IDisposable
{
    private const string PageSectionNavigationOwnerKey = "service";
    private const string AgreementInfoSectionId = "agreement-info";
    private const string ProtectedPremisesSectionId = "protected-premises";
    private const string MonitoringPaymentsSectionId = "monitoring-payments";
    private const string ServiceCallsSectionId = "service-calls";
    private const string ServiceQuotesSectionId = "service-quotes";
    private const string ServiceNotesSectionId = "service-notes";
    private const string AgreementInfoElementId = "service-agreement-info-section";
    private const string ProtectedPremisesElementId = "service-protected-premises-section";
    private const string MonitoringPaymentsElementId = "service-monitoring-payments-section";
    private const string ServiceCallsElementId = "service-service-calls-section";
    private const string ServiceQuotesElementId = "service-service-quotes-section";
    private const string ServiceNotesElementId = "service-notes-section";

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private PageSectionNavigationState PageSectionNavigationState { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "selected")]
    public Guid? RequestedAgreementId { get; set; }

    private static readonly IReadOnlyList<PageSectionNavigationItem> ServicePageSectionNavigationItems =
    [
        new(AgreementInfoSectionId, AgreementInfoElementId, "Agreement Info"),
        new(ProtectedPremisesSectionId, ProtectedPremisesElementId, "Protected Premises"),
        new(MonitoringPaymentsSectionId, MonitoringPaymentsElementId, "Monitoring Payments"),
        new(ServiceCallsSectionId, ServiceCallsElementId, "Service Calls"),
        new(ServiceQuotesSectionId, ServiceQuotesElementId, "Service Quotes"),
        new(ServiceNotesSectionId, ServiceNotesElementId, "Notes")
    ];

    private Guid? SelectedAgreementId { get; set; }
    private Guid? SelectedQuoteId { get; set; }
    private string StatusMessage { get; set; } = string.Empty;
    private string? ActiveMainSectionId { get; set; }
    private string? PendingSectionElementId { get; set; }
    private HashSet<string> ExpandedSectionIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    private bool IsDirectoryPanelExpanded { get; set; }

    private ServiceAgreement? SelectedAgreement =>
        SelectedAgreementId is null
            ? null
            : Store.Workspace.ServiceAgreements.FirstOrDefault(agreement => agreement.Id == SelectedAgreementId.Value);

    private ServiceQuoteRecord? SelectedQuote =>
        SelectedAgreement is null || SelectedQuoteId is null
            ? null
            : SelectedAgreement.Quotes.FirstOrDefault(quote => quote.Id == SelectedQuoteId.Value);

    private ClientRecord? CurrentClient =>
        SelectedAgreement is null
            ? null
            : Store.GetClient(SelectedAgreement.ClientId);

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
        if (RequestedAgreementId is not null && Store.Workspace.ServiceAgreements.Any(agreement => agreement.Id == RequestedAgreementId.Value))
        {
            SelectedAgreementId = RequestedAgreementId;
        }
        else if (SelectedAgreementId is null || Store.Workspace.ServiceAgreements.All(agreement => agreement.Id != SelectedAgreementId.Value))
        {
            SelectedAgreementId = Store.Workspace.ServiceAgreements.FirstOrDefault()?.Id;
        }

        if (SelectedAgreement is null)
        {
            SelectedQuoteId = null;
            ActiveMainSectionId = null;
            ExpandedSectionIds.Clear();
        }
        else if (SelectedQuoteId is null || SelectedAgreement.Quotes.All(quote => quote.Id != SelectedQuoteId.Value))
        {
            SelectedQuoteId = SelectedAgreement.Quotes.FirstOrDefault()?.Id;
        }

        RefreshPageSectionNavigation();
    }

    private void SelectAgreement(Guid agreementId)
    {
        SelectedAgreementId = agreementId;
        SelectedQuoteId = SelectedAgreement?.Quotes.FirstOrDefault()?.Id;
        CollapseAllSections();
        CloseDirectoryPanel();
        PendingSectionElementId = null;
        StatusMessage = string.Empty;
        RefreshPageSectionNavigation();
        NavigationManager.NavigateTo($"/service?selected={agreementId}", replace: true);
    }

    private async Task CreateAgreementAsync()
    {
        var agreement = Store.CreateServiceAgreement();
        SelectedAgreementId = agreement.Id;
        SelectedQuoteId = agreement.Quotes.FirstOrDefault()?.Id;
        CollapseAllSections();
        CloseDirectoryPanel();
        PendingSectionElementId = null;
        StatusMessage = StatusMessageFormatter.WithTimestamp("New service agreement created.");
        RefreshPageSectionNavigation();
        await Store.SaveAsync();
        NavigationManager.NavigateTo($"/service?selected={agreement.Id}", replace: true);
    }

    private void ToggleDirectoryPanel() =>
        IsDirectoryPanelExpanded = !IsDirectoryPanelExpanded;

    private void CloseDirectoryPanel() =>
        IsDirectoryPanelExpanded = false;

    private void CreateClientForAgreement()
    {
        if (SelectedAgreement is null)
        {
            return;
        }

        var client = Store.CreateClient();
        SelectedAgreement.ClientId = client.Id;
        StatusMessage = StatusMessageFormatter.WithTimestamp("New client linked to the agreement.");
    }

    private async Task SaveAsync()
    {
        await Store.SaveAsync();
        StatusMessage = StatusMessageFormatter.WithTimestamp("Service agreement saved.");
    }

    private async Task RegenerateScheduleAsync()
    {
        if (SelectedAgreement is null)
        {
            return;
        }

        Store.RegenerateMonitoringSchedule(SelectedAgreement);
        await Store.SaveAsync();
        StatusMessage = StatusMessageFormatter.WithTimestamp("Monitoring schedule rebuilt from the current contract settings.");
    }

    private async Task OnClientChanged(ChangeEventArgs args)
    {
        if (SelectedAgreement is null)
        {
            return;
        }

        SelectedAgreement.ClientId = ParseNullableGuid(args.Value?.ToString());
        await Task.CompletedTask;
    }

    private void AddMonitoringPayment()
    {
        if (SelectedAgreement is null)
        {
            return;
        }

        var payment = new MonitoringPayment
        {
            DueDate = EstimateMath.GetNextBillingDate(SelectedAgreement),
            Amount = SelectedAgreement.MonthlyMonitoringAmount
        };

        SelectedAgreement.MonitoringPayments.Add(payment);
        ExpandSection(MonitoringPaymentsSectionId);
        PendingSectionElementId = GetMonitoringPaymentElementId(payment.Id);
    }

    private void RemoveMonitoringPayment(Guid paymentId) =>
        SelectedAgreement?.MonitoringPayments.RemoveAll(payment => payment.Id == paymentId);

    private void AddServiceCall()
    {
        if (SelectedAgreement is null)
        {
            return;
        }

        var serviceCall = new ServiceCallRecord
        {
            Title = "Service Call",
            Status = "Open"
        };

        SelectedAgreement.ServiceCalls.Add(serviceCall);
        ExpandSection(ServiceCallsSectionId);
        PendingSectionElementId = GetServiceCallElementId(serviceCall.Id);
    }

    private void RemoveServiceCall(Guid serviceCallId) =>
        SelectedAgreement?.ServiceCalls.RemoveAll(serviceCall => serviceCall.Id == serviceCallId);

    private void AddQuote()
    {
        if (SelectedAgreement is null)
        {
            return;
        }

        var quote = new ServiceQuoteRecord
        {
            Title = $"Quote {SelectedAgreement.Quotes.Count + 1}"
        };

        SelectedAgreement.Quotes.Add(quote);
        SelectedQuoteId = quote.Id;
        ExpandSection(ServiceQuotesSectionId);
        PendingSectionElementId = GetServiceQuoteElementId(quote.Id);
    }

    private void SelectQuote(Guid quoteId)
    {
        SelectedQuoteId = quoteId;
        StatusMessage = string.Empty;
    }

    private void AddQuoteItem()
    {
        if (SelectedQuote is null)
        {
            return;
        }

        var item = new ServiceQuoteItem { Description = "Quoted Item" };
        SelectedQuote.Items.Add(item);
        ExpandSection(ServiceQuotesSectionId);
        PendingSectionElementId = GetServiceQuoteItemElementId(item.Id);
    }

    private void RemoveQuoteItem(Guid itemId) =>
        SelectedQuote?.Items.RemoveAll(item => item.Id == itemId);

    private Task OnPageSectionNavigationRequestedAsync(PageSectionNavigationItem item)
    {
        CollapseAllSections();
        ExpandSection(item.SectionId);
        PendingSectionElementId = item.ElementId;
        return InvokeAsync(StateHasChanged);
    }

    private void RefreshPageSectionNavigation()
    {
        if (SelectedAgreement is null)
        {
            PageSectionNavigationState.Clear(PageSectionNavigationOwnerKey);
            return;
        }

        PageSectionNavigationState.Configure(
            PageSectionNavigationOwnerKey,
            ServicePageSectionNavigationItems,
            OnPageSectionNavigationRequestedAsync,
            ActiveMainSectionId,
            "Agreement",
            GetPageContextName());
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

    private bool IsSectionExpanded(string sectionId) =>
        ExpandedSectionIds.Contains(sectionId);

    private string GetPageContextName()
    {
        if (SelectedAgreement is null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(SelectedAgreement.AgreementName))
        {
            return SelectedAgreement.AgreementName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(SelectedAgreement.AgreementNumber))
        {
            return SelectedAgreement.AgreementNumber.Trim();
        }

        return "Untitled Agreement";
    }

    private static string GetMonitoringPaymentElementId(Guid paymentId) =>
        $"service-monitoring-payment-{paymentId:N}";

    private static string GetServiceCallElementId(Guid serviceCallId) =>
        $"service-call-{serviceCallId:N}";

    private static string GetServiceQuoteElementId(Guid quoteId) =>
        $"service-quote-{quoteId:N}";

    private static string GetServiceQuoteItemElementId(Guid itemId) =>
        $"service-quote-item-{itemId:N}";

    private static Guid? ParseNullableGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : null;

    public void Dispose()
    {
        PageSectionNavigationState.Clear(PageSectionNavigationOwnerKey);
    }
}

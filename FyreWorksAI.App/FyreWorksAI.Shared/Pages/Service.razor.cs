using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FyreWorksAI.Shared.Core.Services.Status;

namespace FyreWorksAI.Shared.Pages;

//******************************//
//******** Service *************//
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
    private string ServiceQuoteExportFileName { get; set; } = string.Empty;
    private string? ActiveMainSectionId { get; set; }
    private string? PendingSectionElementId { get; set; }
    private HashSet<string> ExpandedSectionIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<Guid> ExpandedServiceCallIds { get; } = [];
    private bool IsDirectoryPanelExpanded { get; set; }
    private bool IsServiceQuoteExportDialogOpen { get; set; }

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

    //******************************//
    //******** Selection ***********//
    //******************************//

    private void ApplySelection()
    {
        if (RequestedAgreementId is not null &&
            Store.Workspace.ServiceAgreements.Any(agreement => agreement.Id == RequestedAgreementId.Value))
        {
            SelectedAgreementId = RequestedAgreementId;
        }
        else if (SelectedAgreementId is null ||
                 Store.Workspace.ServiceAgreements.All(agreement => agreement.Id != SelectedAgreementId.Value))
        {
            SelectedAgreementId = Store.Workspace.ServiceAgreements.FirstOrDefault()?.Id;
        }

        if (SelectedAgreement is null)
        {
            SelectedQuoteId = null;
            ActiveMainSectionId = null;
            ExpandedSectionIds.Clear();
            ExpandedServiceCallIds.Clear();
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
        ExpandedServiceCallIds.Clear();
        CollapseAllSections();
        CloseDirectoryPanel();
        PendingSectionElementId = null;
        StatusMessage = string.Empty;
        RefreshPageSectionNavigation();
        NavigationManager.NavigateTo($"/service?selected={agreementId}", replace: true);
    }

    private void SelectQuote(Guid quoteId)
    {
        SelectedQuoteId = quoteId;
        StatusMessage = string.Empty;
    }

    //******************************//
    //******** Agreement ***********//
    //******************************//

    private async Task CreateAgreementAsync()
    {
        var agreement = Store.CreateServiceAgreement();
        SelectedAgreementId = agreement.Id;
        SelectedQuoteId = agreement.Quotes.FirstOrDefault()?.Id;
        ExpandedServiceCallIds.Clear();
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

    //******************************//
    //****** Monitoring ************//
    //******************************//

    private void AddMonitoringPayment()
    {
        if (SelectedAgreement is null)
        {
            return;
        }

        var payment = new MonitoringPayment
        {
            DueDate = EstimateMath.GetNextBillingDate(SelectedAgreement),
            Amount = EstimateMath.RoundCurrency(SelectedAgreement.MonthlyMonitoringAmount)
        };

        SelectedAgreement.MonitoringPayments.Add(payment);
        NormalizeMonitoringPayments();
        ExpandSection(MonitoringPaymentsSectionId);
        PendingSectionElementId = GetMonitoringPaymentElementId(payment.Id);
    }

    private void RemoveMonitoringPayment(Guid paymentId)
    {
        if (SelectedAgreement is null)
        {
            return;
        }

        SelectedAgreement.MonitoringPayments.RemoveAll(payment => payment.Id == paymentId);
        NormalizeMonitoringPayments();
    }

    private void NormalizeMonitoringPayments()
    {
        if (SelectedAgreement is null)
        {
            return;
        }

        foreach (var payment in SelectedAgreement.MonitoringPayments)
        {
            NormalizeMonitoringPayment(payment);
        }

        SelectedAgreement.MonitoringPayments = SelectedAgreement.MonitoringPayments
            .OrderBy(payment => payment.DueDate)
            .ToList();
    }

    private static void NormalizeMonitoringPayment(MonitoringPayment payment)
    {
        payment.Amount = EstimateMath.RoundCurrency(Math.Max(0m, payment.Amount));
        payment.AmountBilled = EstimateMath.RoundCurrency(Math.Max(0m, payment.AmountBilled));
        payment.ReceivedAmount = EstimateMath.RoundCurrency(Math.Max(0m, payment.ReceivedAmount));
        payment.IsPaid = EstimateMath.IsMonitoringPaymentSettled(payment);
        payment.Notes ??= string.Empty;
    }

    private IReadOnlyList<MonitoringPayment> GetOrderedMonitoringPayments() =>
        SelectedAgreement?.MonitoringPayments
            .OrderBy(payment => payment.DueDate)
            .ToList()
        ?? [];

    private int GetMonitoringPaymentInstallmentNumber(MonitoringPayment payment)
    {
        var orderedPayments = GetOrderedMonitoringPayments();
        for (var index = 0; index < orderedPayments.Count; index++)
        {
            if (orderedPayments[index].Id == payment.Id)
            {
                return index + 1;
            }
        }

        return 0;
    }

    private string GetMonitoringPaymentStatus(MonitoringPayment payment)
    {
        if (EstimateMath.IsMonitoringPaymentSettled(payment))
        {
            return "Received";
        }

        if (EstimateMath.GetMonitoringPaymentReceivedAmount(payment) > 0m)
        {
            return "Partial Receipt";
        }

        if (EstimateMath.GetMonitoringPaymentBilledAmount(payment) > 0m)
        {
            return "Billed";
        }

        return "Scheduled";
    }

    private string GetCurrentMonitoringInstallmentSummary()
    {
        if (SelectedAgreement is null)
        {
            return "No monitoring schedule loaded.";
        }

        var currentInstallment = EstimateMath.GetCurrentServiceInstallment(SelectedAgreement);
        if (currentInstallment is null)
        {
            return "All monitoring installments are fully received.";
        }

        return $"Installment {GetMonitoringPaymentInstallmentNumber(currentInstallment)} due {currentInstallment.DueDate:d}.";
    }

    private void OnMonitoringPaymentBilledDateChanged(MonitoringPayment payment, ChangeEventArgs args)
    {
        payment.BilledOn = ParseNullableDate(args.Value?.ToString());
        NormalizeMonitoringPayment(payment);
    }

    private void OnMonitoringPaymentReceivedDateChanged(MonitoringPayment payment, ChangeEventArgs args)
    {
        payment.ReceivedOn = ParseNullableDate(args.Value?.ToString());
        NormalizeMonitoringPayment(payment);
    }

    //******************************//
    //****** Service Calls *********//
    //******************************//

    private void AddServiceCall()
    {
        if (SelectedAgreement is null)
        {
            return;
        }

        var serviceCall = Store.CreateServiceCall(SelectedAgreement);
        ExpandSection(ServiceCallsSectionId);
        ExpandedServiceCallIds.Add(serviceCall.Id);
        PendingSectionElementId = GetServiceCallElementId(serviceCall.Id);
    }

    private void AddReturnVisit(ServiceCallRecord existingServiceCall)
    {
        if (SelectedAgreement is null)
        {
            return;
        }

        var serviceCall = Store.CreateReturnVisitServiceCall(SelectedAgreement, existingServiceCall);
        ExpandSection(ServiceCallsSectionId);
        ExpandedServiceCallIds.Add(serviceCall.Id);
        PendingSectionElementId = GetServiceCallElementId(serviceCall.Id);
        StatusMessage = StatusMessageFormatter.WithTimestamp($"Return visit created under {serviceCall.ServiceTicketNumber}.");
    }

    private void ToggleServiceCall(Guid serviceCallId)
    {
        if (!ExpandedServiceCallIds.Add(serviceCallId))
        {
            ExpandedServiceCallIds.Remove(serviceCallId);
        }
    }

    private bool IsServiceCallExpanded(Guid serviceCallId) =>
        ExpandedServiceCallIds.Contains(serviceCallId);

    private void RemoveServiceCall(Guid serviceCallId)
    {
        if (SelectedAgreement is null || !Store.RemoveServiceCall(SelectedAgreement, serviceCallId))
        {
            return;
        }

        ExpandedServiceCallIds.Remove(serviceCallId);
        StatusMessage = StatusMessageFormatter.WithTimestamp("Service call removed.");
    }

    private IReadOnlyList<ServiceCallRecord> GetOrderedServiceCalls() =>
        SelectedAgreement?.ServiceCalls
            .OrderByDescending(serviceCall => serviceCall.ServiceTicketNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(serviceCall => serviceCall.ReturnVisitSequence)
            .ThenBy(serviceCall => serviceCall.OpenedOn)
            .ToList()
        ?? [];

    private decimal GetTotalServiceCallHours() =>
        EstimateMath.RoundHours(GetOrderedServiceCalls().Sum(EstimateMath.GetServiceCallLaborHours));

    private decimal GetTotalServiceCallQuotedAmount() =>
        EstimateMath.RoundCurrency(GetOrderedServiceCalls().Sum(serviceCall => serviceCall.SourceQuoteAmount));

    private decimal GetTotalServiceCallInvoiceAmount() =>
        EstimateMath.RoundCurrency(GetOrderedServiceCalls().Sum(EstimateMath.GetServiceCallInvoiceAmount));

    private decimal GetTotalServiceCallBilledAmount() =>
        EstimateMath.RoundCurrency(GetOrderedServiceCalls().Sum(EstimateMath.GetServiceCallBilledAmount));

    private decimal GetTotalServiceCallPaidAmount() =>
        EstimateMath.RoundCurrency(GetOrderedServiceCalls().Sum(EstimateMath.GetServiceCallPaidAmount));

    private decimal GetTotalServiceCallOutstandingAmount() =>
        EstimateMath.RoundCurrency(GetOrderedServiceCalls().Sum(EstimateMath.GetServiceCallOutstandingAmount));

    private int GetOpenServiceCallCount() =>
        GetOrderedServiceCalls().Count(serviceCall =>
            !string.Equals(serviceCall.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(serviceCall.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));

    private void NavigateToServiceCall(Guid serviceCallId)
    {
        ExpandSection(ServiceCallsSectionId);
        ExpandedServiceCallIds.Add(serviceCallId);
        PendingSectionElementId = GetServiceCallElementId(serviceCallId);
    }

    private void NavigateToQuote(Guid quoteId)
    {
        SelectedQuoteId = quoteId;
        ExpandSection(ServiceQuotesSectionId);
        PendingSectionElementId = GetServiceQuoteElementId(quoteId);
    }

    private void NormalizeServiceCallBilling(ServiceCallRecord serviceCall)
    {
        serviceCall.Billing.LaborHours = EstimateMath.RoundHours(Math.Max(0m, serviceCall.Billing.LaborHours));
        serviceCall.Billing.LaborAmount = EstimateMath.RoundCurrency(Math.Max(0m, serviceCall.Billing.LaborAmount));
        serviceCall.Billing.MaterialAmount = EstimateMath.RoundCurrency(Math.Max(0m, serviceCall.Billing.MaterialAmount));
        serviceCall.Billing.InvoiceAmount = EstimateMath.RoundCurrency(Math.Max(0m, serviceCall.Billing.InvoiceAmount));
        serviceCall.Billing.BilledAmount = EstimateMath.RoundCurrency(Math.Max(0m, serviceCall.Billing.BilledAmount));
        serviceCall.Billing.PaidAmount = EstimateMath.RoundCurrency(Math.Max(0m, serviceCall.Billing.PaidAmount));
        serviceCall.Billing.Notes ??= string.Empty;
    }

    private void OnServiceCallBilledDateChanged(ServiceCallRecord serviceCall, ChangeEventArgs args)
    {
        serviceCall.Billing.BilledOn = ParseNullableDate(args.Value?.ToString());
        NormalizeServiceCallBilling(serviceCall);
    }

    private void OnServiceCallPaidDateChanged(ServiceCallRecord serviceCall, ChangeEventArgs args)
    {
        serviceCall.Billing.PaidOn = ParseNullableDate(args.Value?.ToString());
        NormalizeServiceCallBilling(serviceCall);
    }

    //******************************//
    //****** Service Quotes ********//
    //******************************//

    private void AddQuote()
    {
        if (SelectedAgreement is null)
        {
            return;
        }

        var defaultLaborCostRate = EstimateMath.RoundCurrency(Math.Max(0m, Store.Workspace.Settings.FieldLaborRate));

        var quote = new ServiceQuoteRecord
        {
            Title = $"Quote {SelectedAgreement.Quotes.Count + 1}",
            LaborLines =
            [
                new ServiceQuoteLaborLine
                {
                    Description = "Service Labor",
                    CostRate = defaultLaborCostRate,
                    SaleRate = EstimateMath.GetDefaultSaleFromMarkup(defaultLaborCostRate, Store.Workspace.Settings.DefaultMarkupPercent)
                }
            ]
        };

        SelectedAgreement.Quotes.Add(quote);
        SelectedQuoteId = quote.Id;
        ExpandSection(ServiceQuotesSectionId);
        PendingSectionElementId = GetServiceQuoteElementId(quote.Id);
    }

    private void AddQuoteLaborLine()
    {
        if (SelectedQuote is null)
        {
            return;
        }

        var defaultLaborCostRate = EstimateMath.RoundCurrency(Math.Max(0m, Store.Workspace.Settings.FieldLaborRate));
        var laborLine = new ServiceQuoteLaborLine
        {
            Description = SelectedQuote.LaborLines.Count == 0 ? "Service Labor" : $"Service Labor {SelectedQuote.LaborLines.Count + 1}",
            CostRate = defaultLaborCostRate,
            SaleRate = EstimateMath.GetDefaultSaleFromMarkup(defaultLaborCostRate, Store.Workspace.Settings.DefaultMarkupPercent)
        };

        SelectedQuote.LaborLines.Add(laborLine);
        ExpandSection(ServiceQuotesSectionId);
        PendingSectionElementId = GetServiceQuoteLaborLineElementId(laborLine.Id);
    }

    private void RemoveQuoteLaborLine(Guid laborLineId) =>
        SelectedQuote?.LaborLines.RemoveAll(line => line.Id == laborLineId);

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

    private bool IsQuoteAccepted(ServiceQuoteRecord quote) =>
        string.Equals(quote.Status, "Accepted", StringComparison.OrdinalIgnoreCase);

    private void NormalizeServiceQuotePricing(ServiceQuoteRecord quote)
    {
        quote.AdjustedSalePrice = EstimateMath.RoundCurrency(Math.Max(0m, quote.AdjustedSalePrice));
    }

    private void NormalizeServiceQuoteLaborLine(ServiceQuoteLaborLine laborLine)
    {
        laborLine.Description = string.IsNullOrWhiteSpace(laborLine.Description)
            ? "Service Labor"
            : laborLine.Description.Trim();
        laborLine.Hours = EstimateMath.RoundHours(Math.Max(0m, laborLine.Hours));
        laborLine.CostRate = EstimateMath.RoundCurrency(Math.Max(0m, laborLine.CostRate));
        laborLine.SaleRate = EstimateMath.RoundCurrency(laborLine.SaleRate > 0m
            ? laborLine.SaleRate
            : EstimateMath.GetDefaultSaleFromMarkup(laborLine.CostRate, Store.Workspace.Settings.DefaultMarkupPercent));
    }

    private void RoundServiceQuoteAdjustedSale()
    {
        if (SelectedQuote is null)
        {
            return;
        }

        SelectedQuote.AdjustedSalePrice = EstimateMath.RoundCurrency(Math.Max(0m, SelectedQuote.AdjustedSalePrice));
    }

    private ServiceCallRecord? GetLinkedServiceCall(ServiceQuoteRecord quote) =>
        SelectedAgreement?.ServiceCalls.FirstOrDefault(serviceCall => serviceCall.Id == quote.ConvertedServiceCallId);

    private string GetQuoteConversionButtonLabel(ServiceQuoteRecord quote) =>
        GetLinkedServiceCall(quote) is not null
            ? "Open Linked Service Call"
            : IsQuoteAccepted(quote)
                ? "Create Service Call"
                : "Accept + Create Call";

    private async Task ConvertSelectedQuoteToServiceCallAsync()
    {
        if (SelectedAgreement is null || SelectedQuote is null)
        {
            return;
        }

        var existingServiceCall = GetLinkedServiceCall(SelectedQuote);
        if (existingServiceCall is not null)
        {
            NavigateToServiceCall(existingServiceCall.Id);
            StatusMessage = StatusMessageFormatter.WithTimestamp($"Linked service call {existingServiceCall.ServiceJobNumber} opened.");
            return;
        }

        var serviceCall = Store.ConvertServiceQuoteToServiceCall(SelectedAgreement, SelectedQuote);
        ExpandedServiceCallIds.Add(serviceCall.Id);
        await Store.SaveAsync();
        NavigateToServiceCall(serviceCall.Id);
        StatusMessage = StatusMessageFormatter.WithTimestamp($"Service quote accepted and converted to {serviceCall.ServiceJobNumber}.");
    }

    private async Task ExportSelectedQuoteAsync()
    {
        if (SelectedAgreement is null || SelectedQuote is null)
        {
            return;
        }

        ServiceQuoteExportFileName = BuildDefaultServiceQuoteExportFileName(SelectedAgreement, SelectedQuote);
        IsServiceQuoteExportDialogOpen = true;
        await InvokeAsync(StateHasChanged);
    }

    private void CancelServiceQuoteExport()
    {
        IsServiceQuoteExportDialogOpen = false;
        ServiceQuoteExportFileName = string.Empty;
    }

    private async Task ConfirmServiceQuoteExportAsync()
    {
        if (SelectedAgreement is null || SelectedQuote is null)
        {
            CancelServiceQuoteExport();
            return;
        }

        await Store.SaveAsync();
        var path = await Store.ExportServiceQuoteAsync(SelectedAgreement, SelectedQuote, ServiceQuoteExportFileName);
        CancelServiceQuoteExport();
        StatusMessage = StatusMessageFormatter.WithTimestamp(
            path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                ? $"Service quote PDF created at {path}."
                : $"Service quote document created at {path}. PDF export was unavailable, so an HTML file was created instead.");
    }

    private string BuildDefaultServiceQuoteExportFileName(ServiceAgreement agreement, ServiceQuoteRecord quote)
    {
        var siteName = string.IsNullOrWhiteSpace(agreement.Site.SiteName)
            ? "Service Site"
            : agreement.Site.SiteName.Trim();
        var title = string.IsNullOrWhiteSpace(quote.Title)
            ? "Service Quote"
            : quote.Title.Trim();
        return $"{siteName} {title}";
    }

    //******************************//
    //******** Sections ************//
    //******************************//

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

    //******************************//
    //******** Helpers *************//
    //******************************//

    private static string GetDateInputValue(DateTime? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static DateTime? ParseNullableDate(string? value) =>
        DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
            ? parsedDate
            : null;

    private static string GetMonitoringPaymentElementId(Guid paymentId) =>
        $"service-monitoring-payment-{paymentId:N}";

    private static string GetServiceCallElementId(Guid serviceCallId) =>
        $"service-call-{serviceCallId:N}";

    private static string GetServiceQuoteElementId(Guid quoteId) =>
        $"service-quote-{quoteId:N}";

    private static string GetServiceQuoteItemElementId(Guid itemId) =>
        $"service-quote-item-{itemId:N}";

    private static string GetServiceQuoteLaborLineElementId(Guid laborLineId) =>
        $"service-quote-labor-line-{laborLineId:N}";

    private static Guid? ParseNullableGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : null;

    public void Dispose()
    {
        PageSectionNavigationState.Clear(PageSectionNavigationOwnerKey);
    }
}

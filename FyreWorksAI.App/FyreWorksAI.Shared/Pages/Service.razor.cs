using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace FyreWorksAI.Shared.Pages;

//******************************//
//******** Service***************//
//******************************//
public partial class Service
{

    [SupplyParameterFromQuery(Name = "selected")]
    public Guid? RequestedAgreementId { get; set; }

    private Guid? SelectedAgreementId { get; set; }
    private Guid? SelectedQuoteId { get; set; }
    private string StatusMessage { get; set; } = string.Empty;
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
        SelectedQuoteId = SelectedAgreement?.Quotes.FirstOrDefault()?.Id;
    }

    protected override void OnParametersSet()
    {
        if (Store.IsInitialized)
        {
            ApplySelection();
            SelectedQuoteId ??= SelectedAgreement?.Quotes.FirstOrDefault()?.Id;
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
    }

    private void SelectAgreement(Guid agreementId)
    {
        SelectedAgreementId = agreementId;
        SelectedQuoteId = SelectedAgreement?.Quotes.FirstOrDefault()?.Id;
        CloseDirectoryPanel();
        StatusMessage = string.Empty;
    }

    private async Task CreateAgreementAsync()
    {
        var agreement = Store.CreateServiceAgreement();
        SelectedAgreementId = agreement.Id;
        SelectedQuoteId = agreement.Quotes.FirstOrDefault()?.Id;
        CloseDirectoryPanel();
        StatusMessage = "New service agreement created.";
        await Store.SaveAsync();
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
        StatusMessage = "New client linked to the agreement.";
    }

    private async Task SaveAsync()
    {
        await Store.SaveAsync();
        StatusMessage = "Service agreement saved.";
    }

    private async Task RegenerateScheduleAsync()
    {
        if (SelectedAgreement is null)
        {
            return;
        }

        Store.RegenerateMonitoringSchedule(SelectedAgreement);
        await Store.SaveAsync();
        StatusMessage = "Monitoring schedule rebuilt from the current contract settings.";
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

    private void AddMonitoringPayment() =>
        SelectedAgreement?.MonitoringPayments.Add(new MonitoringPayment
        {
            DueDate = EstimateMath.GetNextBillingDate(SelectedAgreement),
            Amount = SelectedAgreement.MonthlyMonitoringAmount
        });

    private void RemoveMonitoringPayment(Guid paymentId) =>
        SelectedAgreement?.MonitoringPayments.RemoveAll(payment => payment.Id == paymentId);

    private void AddServiceCall() =>
        SelectedAgreement?.ServiceCalls.Add(new ServiceCallRecord
        {
            Title = "Service Call",
            Status = "Open"
        });

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
    }

    private void SelectQuote(Guid quoteId)
    {
        SelectedQuoteId = quoteId;
        StatusMessage = string.Empty;
    }

    private void AddQuoteItem() =>
        SelectedQuote?.Items.Add(new ServiceQuoteItem { Description = "Quoted Item" });

    private void RemoveQuoteItem(Guid itemId) =>
        SelectedQuote?.Items.RemoveAll(item => item.Id == itemId);

    private static Guid? ParseNullableGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : null;
}

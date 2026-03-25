using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace FyreWorksAI.Shared.Pages;

//******************************//
//******** Bids******************//
//******************************//
public partial class Bids
{

    [SupplyParameterFromQuery(Name = "selected")]
    public Guid? RequestedBidId { get; set; }

    private Guid? SelectedBidId { get; set; }
    private string StatusMessage { get; set; } = string.Empty;
    private HashSet<Guid> ExpandedComponentIds { get; } = [];
    private bool IsDirectoryPanelExpanded { get; set; }

    private BidRecord? SelectedBid => SelectedBidId is null ? null : Store.Workspace.Bids.FirstOrDefault(bid => bid.Id == SelectedBidId.Value);
    private ClientRecord? CurrentClient => SelectedBid is null ? null : Store.GetClient(SelectedBid.ClientId);
    private JobRecord? CurrentLinkedJob => SelectedBid is null ? null : Store.Workspace.Jobs.FirstOrDefault(job => job.SourceBidId == SelectedBid.Id);

    protected override async Task OnInitializedAsync()
    {
        await Store.InitializeAsync();
        ApplySelection();
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
        if (RequestedBidId is not null && Store.Workspace.Bids.Any(bid => bid.Id == RequestedBidId.Value))
        {
            SelectedBidId = RequestedBidId;
        }
        else if (SelectedBidId is null || Store.Workspace.Bids.All(bid => bid.Id != SelectedBidId.Value))
        {
            SelectedBidId = Store.Workspace.Bids.FirstOrDefault()?.Id;
        }
    }

    private void SelectBid(Guid bidId)
    {
        SelectedBidId = bidId;
        CloseDirectoryPanel();
        StatusMessage = string.Empty;
        NavigationManager.NavigateTo($"/bids?selected={bidId}", replace: true);
    }

    private void NavigateToJob(Guid jobId) =>
        NavigationManager.NavigateTo($"/jobs?selected={jobId}");

    private async Task CreateBidAsync()
    {
        var bid = Store.CreateBid();
        SelectedBidId = bid.Id;
        CloseDirectoryPanel();
        StatusMessage = "New bid created.";
        await Store.SaveAsync();
        NavigationManager.NavigateTo($"/bids?selected={bid.Id}", replace: true);
    }

    private void ToggleDirectoryPanel() =>
        IsDirectoryPanelExpanded = !IsDirectoryPanelExpanded;

    private void CloseDirectoryPanel() =>
        IsDirectoryPanelExpanded = false;

    private void CreateClientForBid()
    {
        if (SelectedBid is null) return;
        var client = Store.CreateClient();
        SelectedBid.ClientId = client.Id;
        StatusMessage = "New client linked to the bid.";
    }

    private async Task SaveAsync()
    {
        await Store.SaveAsync();
        StatusMessage = "Bid saved.";
    }

    private async Task DeleteBidAsync()
    {
        if (SelectedBid is null)
        {
            return;
        }

        var deletedBidId = SelectedBid.Id;
        if (!Store.DeleteBid(deletedBidId))
        {
            return;
        }

        SelectedBidId = GetNextBidId();
        ExpandedComponentIds.Clear();
        await Store.SaveAsync();
        StatusMessage = "Bid deleted.";
        NavigationManager.NavigateTo(
            SelectedBidId is null ? "/bids" : $"/bids?selected={SelectedBidId}",
            replace: true);
    }

    private async Task ApplyTemplateAsync()
    {
        if (SelectedBid is null) return;
        Store.ApplyTemplateToBid(SelectedBid);
        SyncAllCalculatedSales();
        await Store.SaveAsync();
        StatusMessage = "Template values applied to the bid.";
    }

    private async Task SaveCurrentTemplateAsync()
    {
        if (SelectedBid is null) return;
        var template = Store.CreateTemplateFromBid(SelectedBid);
        await Store.SaveAsync();
        StatusMessage = $"Saved pricing profile {template.Name}.";
    }

    private async Task ExportProposalAsync()
    {
        if (SelectedBid is null) return;
        await Store.SaveAsync();
        var path = await Store.ExportBidProposalAsync(SelectedBid);
        StatusMessage = $"Proposal export created at {path}.";
    }

    private async Task ConvertToJobAsync()
    {
        if (SelectedBid is null) return;
        var job = Store.ConvertBidToJob(SelectedBid);
        await Store.SaveAsync();
        NavigationManager.NavigateTo($"/jobs?selected={job.Id}");
    }

    private Task OnTemplateChanged(ChangeEventArgs args)
    {
        if (SelectedBid is null) return Task.CompletedTask;
        SelectedBid.TemplateId = ParseNullableGuid(args.Value?.ToString());
        Store.ApplyTemplateToBid(SelectedBid);
        SyncAllCalculatedSales();
        return Task.CompletedTask;
    }

    private Task OnClientChanged(ChangeEventArgs args)
    {
        if (SelectedBid is null) return Task.CompletedTask;
        SelectedBid.ClientId = ParseNullableGuid(args.Value?.ToString());
        return Task.CompletedTask;
    }

    private Guid? GetNextBidId() =>
        Store.Workspace.Bids
            .Where(bid => bid.IsActive)
            .OrderBy(bid => bid.DueDate)
            .Select(bid => (Guid?)bid.Id)
            .Concat(
                Store.Workspace.Bids
                    .Where(bid => !bid.IsActive)
                    .OrderByDescending(bid => bid.CreatedOn)
                    .Select(bid => (Guid?)bid.Id))
            .FirstOrDefault();

    private void AddAdministrativeTask() => SelectedBid?.AdministrativeTasks.Add(new WorkTask { Title = "Administrative Task" });
    private void RemoveAdministrativeTask(Guid taskId) => SelectedBid?.AdministrativeTasks.RemoveAll(task => task.Id == taskId);
    private void AddEngineeringTask() => SelectedBid?.EngineeringTasks.Add(new WorkTask { Title = "Engineering Task" });
    private void RemoveEngineeringTask(Guid taskId) => SelectedBid?.EngineeringTasks.RemoveAll(task => task.Id == taskId);

    private void AddComponent()
    {
        if (SelectedBid is null) return;
        var component = new BidComponent
        {
            Name = "Device",
            Quantity = 1m,
            LocationProfile = string.Empty,
            InstallType = string.Empty,
            InstallMinutes = 0m,
            DemoMinutes = 0m,
            TrimMinutes = 0m,
            TestMinutes = 0m
        };
        SelectedBid.Components.Add(component);
        ExpandedComponentIds.Add(component.Id);
        RefreshFieldLaborMix();
    }

    private void AddDemoItem()
    {
        if (SelectedBid is null) return;
        var demoItem = new BidDemoItem
        {
            Name = "Demo Item",
            Quantity = 1m,
            LocationProfile = string.Empty,
            InstallType = string.Empty,
            DemoHoursEach = 0m
        };

        SelectedBid.DemoItems.Add(demoItem);
        RefreshFieldLaborMix();
    }

    private void MatchComponent(BidComponent component)
    {
        var matched = Store.ApplyTemplateToComponent(component, Store.GetTemplate(SelectedBid?.TemplateId));
        StatusMessage = matched ? "Component labor matched from the selected template." : "No matching template rule was found for that location/condition combination.";
    }

    private void MatchComponentSilently(BidComponent component)
    {
        Store.ApplyTemplateToComponent(component, Store.GetTemplate(SelectedBid?.TemplateId));
    }

    private void MatchComponentAndRefresh(BidComponent component)
    {
        MatchComponentSilently(component);
        RefreshFieldLaborMix();
    }

    private void MatchDemoItemSilently(BidDemoItem demoItem)
    {
        Store.ApplyTemplateToDemoItem(demoItem, Store.GetTemplate(SelectedBid?.TemplateId));
    }

    private void MatchDemoItemAndRefresh(BidDemoItem demoItem)
    {
        MatchDemoItemSilently(demoItem);
        RefreshFieldLaborMix();
    }

    private void RoundAcceptedSale()
    {
        if (SelectedBid is null) return;
        SelectedBid.AcceptedSalePrice = EstimateMath.RoundCurrency(SelectedBid.AcceptedSalePrice);
    }

    private void SyncComponentSale(BidComponent component)
    {
        if (SelectedBid is null) return;
        component.UnitSale = EstimateMath.GetDefaultSaleFromMarkup(component.UnitCost, SelectedBid.MarkupPercent);
    }

    private void NormalizeComponentCost(BidComponent component)
    {
        component.UnitCost = EstimateMath.RoundCurrency(component.UnitCost);
        SyncComponentSale(component);
    }

    private static void NormalizeComponentSale(BidComponent component)
    {
        component.UnitSale = EstimateMath.RoundCurrency(component.UnitSale);
    }

    private void RemoveComponent(Guid componentId)
    {
        if (SelectedBid is null) return;
        SelectedBid.Components.RemoveAll(component => component.Id == componentId);
        ExpandedComponentIds.Remove(componentId);
        RefreshFieldLaborMix();
    }

    private void RemoveDemoItem(Guid demoItemId)
    {
        if (SelectedBid is null) return;
        SelectedBid.DemoItems.RemoveAll(demoItem => demoItem.Id == demoItemId);
        RefreshFieldLaborMix();
    }

    private void AddWire() => AddMaterialLine(BidMaterialKind.Wire, "Wire", "Wire Item");
    private void AddMaterial() => AddMaterialLine(BidMaterialKind.Material, "Material", "Material Item");

    private void AddMaterialLine(BidMaterialKind kind, string category, string description)
    {
        if (SelectedBid is null) return;
        SelectedBid.Materials.Add(new BidMaterialItem { Kind = kind, Category = category, Description = description });
    }

    private void RemoveMaterial(Guid materialId) => SelectedBid?.Materials.RemoveAll(material => material.Id == materialId);

    private void RefreshFieldLaborMix()
    {
        if (SelectedBid is null) return;
        Store.RebalanceBidLaborDistribution(SelectedBid);
    }

    private Task RefreshBidView() => InvokeAsync(StateHasChanged);

    private void NormalizeLaborLine(BidLaborDistributionLine line)
    {
        line.InstallHours = EstimateMath.RoundHours(line.InstallHours);
        line.DemoHours = EstimateMath.RoundHours(line.DemoHours);
        line.TrimHours = EstimateMath.RoundHours(line.TrimHours);
        line.TestHours = EstimateMath.RoundHours(line.TestHours);
    }

    private void NormalizeDemoHours(BidDemoItem demoItem)
    {
        demoItem.DemoHoursEach = EstimateMath.RoundHours(demoItem.DemoHoursEach);
        RefreshFieldLaborMix();
    }

    private void ToggleComponentExpanded(Guid componentId)
    {
        if (!ExpandedComponentIds.Add(componentId))
        {
            ExpandedComponentIds.Remove(componentId);
        }
    }

    private bool IsComponentExpanded(Guid componentId) =>
        ExpandedComponentIds.Contains(componentId);

    private static string GetComponentDisplayName(BidComponent component) =>
        string.IsNullOrWhiteSpace(component.Name) ? "Untitled Component" : component.Name.Trim();

    private static string FormatCompactNumber(decimal value) =>
        value.ToString("0.##");

    private void SyncAllCalculatedSales()
    {
        if (SelectedBid is null) return;

        foreach (var task in SelectedBid.AdministrativeTasks.Where(task => task.PricingMode == TaskPricingMode.Fixed))
        {
            task.SalePrice = EstimateMath.GetDefaultSaleFromMarkup(task.CostPrice, SelectedBid.MarkupPercent);
        }

        foreach (var task in SelectedBid.EngineeringTasks.Where(task => task.PricingMode == TaskPricingMode.Fixed))
        {
            task.SalePrice = EstimateMath.GetDefaultSaleFromMarkup(task.CostPrice, SelectedBid.MarkupPercent);
        }

        foreach (var component in SelectedBid.Components)
        {
            component.UnitSale = EstimateMath.GetDefaultSaleFromMarkup(component.UnitCost, SelectedBid.MarkupPercent);
        }

        foreach (var material in SelectedBid.Materials)
        {
            material.UnitSale = EstimateMath.GetDefaultSaleFromMarkup(material.UnitCost, SelectedBid.MarkupPercent);
        }
    }

    private static Guid? ParseNullableGuid(string? value) => Guid.TryParse(value, out var parsed) ? parsed : null;
}

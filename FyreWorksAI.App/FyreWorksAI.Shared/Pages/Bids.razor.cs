using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using FyreWorksAI.Shared.Core.Services.Status;

namespace FyreWorksAI.Shared.Pages;

//******************************//
//******** Bids******************//
//******************************//
public partial class Bids : IDisposable
{
    private const string PageSectionNavigationOwnerKey = "bids";
    private const string ContactBillingSectionId = "contact-billing";
    private const string PricingSectionId = "pricing";
    private const string SiteInfoSectionId = "site-info";
    private const string ExclusionsSectionId = "exclusions";
    private const string ProposalSectionId = "proposal";
    private const string FieldLaborSectionId = "field-labor";
    private const string OfficeScopeSectionId = "office-scope";
    private const string EngineeringScopeSectionId = "engineering-scope";
    private const string DemoScopeSectionId = "demo-scope";
    private const string ComponentsSectionId = "components";
    private const string WireItemsSectionId = "wire-items";
    private const string MaterialItemsSectionId = "material-items";
    private const string BidNotesSectionId = "bid-notes";
    private const string ContactBillingElementId = "bids-contact-billing-section";
    private const string PricingElementId = "bids-pricing-section";
    private const string SiteInfoElementId = "bids-site-info-section";
    private const string ExclusionsElementId = "bids-exclusions-section";
    private const string ProposalElementId = "bids-proposal-section";
    private const string FieldLaborElementId = "bids-field-labor-section";
    private const string OfficeScopeElementId = "bids-office-scope-section";
    private const string EngineeringScopeElementId = "bids-engineering-scope-section";
    private const string DemoScopeElementId = "bids-demo-scope-section";
    private const string ComponentsElementId = "bids-components-section";
    private const string WireItemsElementId = "bids-wire-items-section";
    private const string MaterialItemsElementId = "bids-material-items-section";
    private const string BidNotesElementId = "bids-notes-section";

    [SupplyParameterFromQuery(Name = "selected")]
    public Guid? RequestedBidId { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private PageSectionNavigationState PageSectionNavigationState { get; set; } = default!;

    private static readonly IReadOnlyList<PageSectionNavigationItem> BidPageSectionNavigationItems =
    [
        new(ContactBillingSectionId, ContactBillingElementId, "Contact + Billing"),
        new(PricingSectionId, PricingElementId, "Pricing"),
        new(SiteInfoSectionId, SiteInfoElementId, "Site Info"),
        new(ExclusionsSectionId, ExclusionsElementId, "Exclusions"),
        new(ProposalSectionId, ProposalElementId, "Proposal"),
        new(FieldLaborSectionId, FieldLaborElementId, "Field Labor"),
        new(OfficeScopeSectionId, OfficeScopeElementId, "Office Scope"),
        new(EngineeringScopeSectionId, EngineeringScopeElementId, "Engineering"),
        new(DemoScopeSectionId, DemoScopeElementId, "Demo Scope"),
        new(ComponentsSectionId, ComponentsElementId, "Components"),
        new(WireItemsSectionId, WireItemsElementId, "Wire"),
        new(MaterialItemsSectionId, MaterialItemsElementId, "Material"),
        new(BidNotesSectionId, BidNotesElementId, "Notes")
    ];

    private Guid? SelectedBidId { get; set; }
    private string StatusMessage { get; set; } = string.Empty;
    private string? ActiveMainSectionId { get; set; }
    private string? PendingSectionElementId { get; set; }
    private HashSet<string> ExpandedSectionIds { get; } = new(StringComparer.OrdinalIgnoreCase);
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
        if (RequestedBidId is not null && Store.Workspace.Bids.Any(bid => bid.Id == RequestedBidId.Value))
        {
            SelectedBidId = RequestedBidId;
        }
        else if (SelectedBidId is null || Store.Workspace.Bids.All(bid => bid.Id != SelectedBidId.Value))
        {
            SelectedBidId = Store.Workspace.Bids.FirstOrDefault()?.Id;
        }

        if (SelectedBid is not null)
        {
            ExpandedComponentIds.RemoveWhere(componentId => SelectedBid.Components.All(component => component.Id != componentId));
        }
        else
        {
            ExpandedComponentIds.Clear();
            CollapseAllSections();
        }

        RefreshPageSectionNavigation();
    }

    private void SelectBid(Guid bidId)
    {
        SelectedBidId = bidId;
        CollapseAllSections();
        CloseDirectoryPanel();
        PendingSectionElementId = null;
        StatusMessage = string.Empty;
        RefreshPageSectionNavigation();
        NavigationManager.NavigateTo($"/bids?selected={bidId}", replace: true);
    }

    private void NavigateToJob(Guid jobId) =>
        NavigationManager.NavigateTo($"/jobs?selected={jobId}");

    private async Task CreateBidAsync()
    {
        var bid = Store.CreateBid();
        SelectedBidId = bid.Id;
        CollapseAllSections();
        CloseDirectoryPanel();
        PendingSectionElementId = null;
        StatusMessage = StatusMessageFormatter.WithTimestamp("New bid created.");
        RefreshPageSectionNavigation();
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
        StatusMessage = StatusMessageFormatter.WithTimestamp("New client linked to the bid.");
    }

    private async Task SaveAsync()
    {
        await Store.SaveAsync();
        StatusMessage = StatusMessageFormatter.WithTimestamp("Bid saved.");
    }

    private async Task DuplicateBidAsync()
    {
        if (SelectedBid is null)
        {
            return;
        }

        var duplicatedBid = Store.DuplicateBid(SelectedBid);
        SelectedBidId = duplicatedBid.Id;
        CollapseAllSections();
        ExpandedComponentIds.Clear();
        CloseDirectoryPanel();
        PendingSectionElementId = null;
        RefreshPageSectionNavigation();
        await Store.SaveAsync();
        StatusMessage = StatusMessageFormatter.WithTimestamp($"Bid duplicated to {duplicatedBid.BidNumber}.");
        NavigationManager.NavigateTo($"/bids?selected={duplicatedBid.Id}", replace: true);
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
        CollapseAllSections();
        ExpandedComponentIds.Clear();
        PendingSectionElementId = null;
        RefreshPageSectionNavigation();
        await Store.SaveAsync();
        StatusMessage = StatusMessageFormatter.WithTimestamp("Bid deleted.");
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
        StatusMessage = StatusMessageFormatter.WithTimestamp("Template values applied to the bid.");
    }

    private async Task SaveCurrentTemplateAsync()
    {
        if (SelectedBid is null) return;
        var template = Store.CreateTemplateFromBid(SelectedBid);
        await Store.SaveAsync();
        StatusMessage = StatusMessageFormatter.WithTimestamp($"Saved pricing profile {template.Name}.");
    }

    private async Task ExportProposalAsync()
    {
        if (SelectedBid is null) return;
        await Store.SaveAsync();
        var path = await Store.ExportBidProposalAsync(SelectedBid);
        StatusMessage = StatusMessageFormatter.WithTimestamp($"Proposal export created at {path}.");
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

    private Task OnPageSectionNavigationRequestedAsync(PageSectionNavigationItem item)
    {
        CollapseAllSections();
        ExpandSection(item.SectionId);
        PendingSectionElementId = item.ElementId;
        return InvokeAsync(StateHasChanged);
    }

    private void RefreshPageSectionNavigation()
    {
        if (SelectedBid is null)
        {
            PageSectionNavigationState.Clear(PageSectionNavigationOwnerKey);
            return;
        }

        PageSectionNavigationState.Configure(
            PageSectionNavigationOwnerKey,
            BidPageSectionNavigationItems,
            OnPageSectionNavigationRequestedAsync,
            ActiveMainSectionId,
            "Project",
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

    private static string GetLinkedJobNavigationLabel(JobRecord job) =>
        string.IsNullOrWhiteSpace(job.JobNumber)
            ? "To Job"
            : $"To {job.JobNumber.Trim()}";

    private void AddAdministrativeTask()
    {
        if (SelectedBid is null)
        {
            return;
        }

        var task = new WorkTask { Title = "Administrative Task" };
        SelectedBid.AdministrativeTasks.Add(task);
        ExpandSection(OfficeScopeSectionId);
        PendingSectionElementId = GetAdministrativeTaskElementId(task.Id);
    }

    private void RemoveAdministrativeTask(Guid taskId) => SelectedBid?.AdministrativeTasks.RemoveAll(task => task.Id == taskId);

    private void AddEngineeringTask()
    {
        if (SelectedBid is null)
        {
            return;
        }

        var task = new WorkTask { Title = "Engineering Task" };
        SelectedBid.EngineeringTasks.Add(task);
        ExpandSection(EngineeringScopeSectionId);
        PendingSectionElementId = GetEngineeringTaskElementId(task.Id);
    }

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
        ExpandSection(ComponentsSectionId);
        ExpandedComponentIds.Add(component.Id);
        PendingSectionElementId = GetComponentElementId(component.Id);
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
        ExpandSection(DemoScopeSectionId);
        PendingSectionElementId = GetDemoItemElementId(demoItem.Id);
        RefreshFieldLaborMix();
    }

    private void MatchComponent(BidComponent component)
    {
        var matched = Store.ApplyTemplateToComponent(component, Store.GetTemplate(SelectedBid?.TemplateId));
        StatusMessage = StatusMessageFormatter.WithTimestamp(
            matched
                ? "Component labor matched from the selected template."
                : "No matching template rule was found for that location/condition combination.");
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

    private void AddWire()
    {
        if (SelectedBid is null)
        {
            return;
        }

        var item = AddMaterialLine(BidMaterialKind.Wire, "Wire", "Wire Item");
        ExpandSection(WireItemsSectionId);
        PendingSectionElementId = GetMaterialItemElementId(item.Id);
    }

    private void AddMaterial()
    {
        if (SelectedBid is null)
        {
            return;
        }

        var item = AddMaterialLine(BidMaterialKind.Material, "Material", "Material Item");
        ExpandSection(MaterialItemsSectionId);
        PendingSectionElementId = GetMaterialItemElementId(item.Id);
    }

    private BidMaterialItem AddMaterialLine(BidMaterialKind kind, string category, string description)
    {
        if (SelectedBid is null)
        {
            return new BidMaterialItem();
        }

        var item = new BidMaterialItem
        {
            Kind = kind,
            Category = category,
            Description = description
        };

        SelectedBid.Materials.Add(item);
        return item;
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

    private static string GetAdministrativeTaskElementId(WorkTask task) =>
        GetAdministrativeTaskElementId(task.Id);

    private static string GetAdministrativeTaskElementId(Guid taskId) =>
        $"bids-administrative-task-{taskId:N}";

    private static string GetEngineeringTaskElementId(WorkTask task) =>
        GetEngineeringTaskElementId(task.Id);

    private static string GetEngineeringTaskElementId(Guid taskId) =>
        $"bids-engineering-task-{taskId:N}";

    private static string GetDemoItemElementId(Guid demoItemId) =>
        $"bids-demo-item-{demoItemId:N}";

    private static string GetComponentElementId(Guid componentId) =>
        $"bids-component-{componentId:N}";

    private static string GetWireItemElementId(BidMaterialItem item) =>
        GetMaterialItemElementId(item.Id);

    private static string GetMaterialItemElementId(BidMaterialItem item) =>
        GetMaterialItemElementId(item.Id);

    private static string GetMaterialItemElementId(Guid materialId) =>
        $"bids-material-item-{materialId:N}";

    private string GetPageContextName()
    {
        if (SelectedBid is null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(SelectedBid.ProjectName))
        {
            return SelectedBid.ProjectName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(SelectedBid.BidNumber))
        {
            return SelectedBid.BidNumber.Trim();
        }

        return "Untitled Bid";
    }

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

    public void Dispose()
    {
        PageSectionNavigationState.Clear(PageSectionNavigationOwnerKey);
    }
}

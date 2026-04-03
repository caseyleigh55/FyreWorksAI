using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace FyreWorksAI.Shared.Components;

//******************************//
//******** BidMaterialTable******//
//******************************//
public partial class BidMaterialTable
{

    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public List<BidMaterialItem> Items { get; set; } = [];

    [Parameter]
    public decimal MarkupPercent { get; set; }

    [Parameter]
    public EventCallback<Guid> OnRemove { get; set; }

    [Parameter]
    public EventCallback OnChanged { get; set; }

    [Parameter]
    public Func<BidMaterialItem, string>? GetRowElementId { get; set; }

    private async Task NormalizeCostAndNotify(BidMaterialItem item)
    {
        item.UnitCost = EstimateMath.RoundCurrency(item.UnitCost);
        item.UnitSale = EstimateMath.GetDefaultSaleFromMarkup(item.UnitCost, MarkupPercent);
        await NotifyChanged();
    }

    private async Task NormalizeSaleAndNotify(BidMaterialItem item)
    {
        item.UnitSale = EstimateMath.RoundCurrency(item.UnitSale);
        await NotifyChanged();
    }

    private string? ResolveRowElementId(BidMaterialItem item) =>
        GetRowElementId?.Invoke(item);

    private Task NotifyChanged() => OnChanged.InvokeAsync();
}

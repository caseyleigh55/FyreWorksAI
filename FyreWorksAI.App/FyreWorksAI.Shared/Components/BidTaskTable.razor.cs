using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace FyreWorksAI.Shared.Components;

//******************************//
//******** BidTaskTable**********//
//******************************//
public partial class BidTaskTable
{

    [Parameter, EditorRequired]
    public BidRecord Bid { get; set; } = default!;

    [Parameter, EditorRequired]
    public List<WorkTask> Tasks { get; set; } = [];

    [Parameter]
    public bool Administrative { get; set; }

    [Parameter]
    public EventCallback<Guid> OnRemove { get; set; }

    [Parameter]
    public EventCallback OnChanged { get; set; }

    private decimal GetCost(WorkTask task) =>
        Administrative
            ? EstimateMath.GetWorkTaskCost(task, Bid.AdminDirectRate)
            : EstimateMath.GetWorkTaskCost(task, Bid.EngineeringDirectRate);

    private decimal GetSale(WorkTask task) =>
        Administrative
            ? EstimateMath.GetWorkTaskSale(task, Bid.AdminDirectRate, Bid.AdminBilledRate, Bid.MarkupPercent)
            : EstimateMath.GetWorkTaskSale(task, Bid.EngineeringDirectRate, Bid.EngineeringBilledRate, Bid.MarkupPercent);

    private async Task NormalizeHoursAndNotify(WorkTask task)
    {
        task.EstimatedHours = EstimateMath.RoundHours(task.EstimatedHours);
        await NotifyChanged();
    }

    private async Task NormalizeCostAndNotify(WorkTask task)
    {
        task.CostPrice = EstimateMath.RoundCurrency(task.CostPrice);
        await SyncTaskSaleAndNotify(task);
    }

    private async Task NormalizeSaleAndNotify(WorkTask task)
    {
        task.SalePrice = EstimateMath.RoundCurrency(task.SalePrice);
        await NotifyChanged();
    }

    private async Task SyncTaskSaleAndNotify(WorkTask task)
    {
        if (task.PricingMode == TaskPricingMode.Fixed)
        {
            task.SalePrice = EstimateMath.GetDefaultSaleFromMarkup(task.CostPrice, Bid.MarkupPercent);
        }

        await NotifyChanged();
    }

    private Task NotifyChanged() => OnChanged.InvokeAsync();
}

using System.Text.Json.Serialization;

namespace FyreWorksAI.Shared.Core.Models.Service;

//******************************//
//******* Service Quote Item ***//
//******************************//
public sealed class ServiceQuoteItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
    public string Notes { get; set; } = string.Empty;

    [JsonIgnore]
    public decimal TotalCost => Quantity * UnitCost;

    [JsonIgnore]
    public decimal TotalPrice => Quantity * UnitPrice;
}

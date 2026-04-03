using System.Text.Json.Serialization;

namespace FyreWorksAI.Shared.Core.Models.Jobs;

//******************************//
//***** Change Order Device ****//
//******************************//

public sealed class ChangeOrderDeviceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CategoryCode { get; set; } = JobCostCodes.Material;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public string UnitLabel { get; set; } = "ea";
    public decimal EstimatedUnitCost { get; set; }
    public decimal EstimatedUnitSale { get; set; }
    public decimal ActualUnitCost { get; set; }
    public Guid? InvoiceId { get; set; }
    public string Notes { get; set; } = string.Empty;

    [JsonIgnore]
    public decimal EstimatedCost => Quantity * EstimatedUnitCost;

    [JsonIgnore]
    public decimal EstimatedSale => Quantity * EstimatedUnitSale;

    [JsonIgnore]
    public decimal ActualCost => Quantity * ActualUnitCost;
}

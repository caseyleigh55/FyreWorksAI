using System.Text.Json.Serialization;

namespace FyreWorksAI.Shared.Core.Models.Service;

//******************************//
//**** Service Quote Labor *****//
//******************************//
public sealed class ServiceQuoteLaborLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = "Service Labor";
    public decimal Hours { get; set; }
    public decimal CostRate { get; set; }
    public decimal SaleRate { get; set; }

    [JsonIgnore]
    public decimal TotalCost => Hours * CostRate;

    [JsonIgnore]
    public decimal TotalSale => Hours * SaleRate;
}

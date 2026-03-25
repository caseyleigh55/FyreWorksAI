using System.Text.Json.Serialization;

namespace FyreWorksAI.Shared.Core.Models.Bids;

//******************************//
//******** Bid Scope ***********//
//******************************//

public sealed class WorkTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public TaskPricingMode PricingMode { get; set; } = TaskPricingMode.Hourly;
    public decimal EstimatedHours { get; set; } = 1m;
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public bool Complete { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class BidLaborDistributionLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public PersonnelType PersonnelType { get; set; }
    public HourType HourType { get; set; }
    public decimal InstallHours { get; set; }
    public decimal DemoHours { get; set; }
    public decimal TrimHours { get; set; }
    public decimal TestHours { get; set; }

    [JsonIgnore]
    public decimal TotalHours => InstallHours + DemoHours + TrimHours + TestHours;
}

public sealed class BidComponent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public string LocationProfile { get; set; } = "Normal Area";
    public string InstallType { get; set; } = "Normal";
    public decimal MaterialCostEach { get; set; }
    public decimal UnitSale { get; set; }
    public bool IncludeInstall { get; set; } = true;
    public bool IncludeTrim { get; set; } = true;
    public bool IncludeTest { get; set; } = true;
    public decimal InstallMinutes { get; set; } = 15m;
    public decimal DemoMinutes { get; set; }
    public decimal TrimMinutes { get; set; } = 10m;
    public decimal TestMinutes { get; set; } = 5m;
    public string Notes { get; set; } = string.Empty;

    [JsonIgnore]
    public decimal UnitCost
    {
        get => MaterialCostEach;
        set => MaterialCostEach = value;
    }

    [JsonIgnore]
    public decimal InstallHours => IncludeInstall ? Quantity * InstallMinutes / 60m : 0m;

    [JsonIgnore]
    public decimal DemoHours => Quantity * DemoMinutes / 60m;

    [JsonIgnore]
    public decimal TrimHours => IncludeTrim ? Quantity * TrimMinutes / 60m : 0m;

    [JsonIgnore]
    public decimal TestHours => IncludeTest ? Quantity * TestMinutes / 60m : 0m;

    [JsonIgnore]
    public decimal TotalMinutes =>
        Quantity * ((IncludeInstall ? InstallMinutes : 0m) + (IncludeTrim ? TrimMinutes : 0m) + (IncludeTest ? TestMinutes : 0m));

    [JsonIgnore]
    public decimal TotalMaterialCost => Quantity * MaterialCostEach;

    [JsonIgnore]
    public decimal TotalMaterialSale => Quantity * UnitSale;
}

public sealed class BidDemoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Demo Item";
    public decimal Quantity { get; set; } = 1m;
    public string LocationProfile { get; set; } = "Normal Area";
    public string InstallType { get; set; } = "Normal";
    public decimal DemoHoursEach { get; set; }
    public string Notes { get; set; } = string.Empty;

    [JsonIgnore]
    public decimal TotalHours => Quantity * DemoHoursEach;
}

public sealed class BidMaterialItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public BidMaterialKind Kind { get; set; } = BidMaterialKind.Unknown;
    public string Category { get; set; } = "Material";
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitCost { get; set; }
    public decimal UnitSale { get; set; }
    public string Notes { get; set; } = string.Empty;

    [JsonIgnore]
    public decimal ExtendedCost => Quantity * UnitCost;

    [JsonIgnore]
    public decimal ExtendedSale => Quantity * UnitSale;
}

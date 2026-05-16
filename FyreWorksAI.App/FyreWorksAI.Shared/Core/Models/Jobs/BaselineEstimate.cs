using System.Text.Json.Serialization;

namespace FyreWorksAI.Shared.Core.Models.Jobs;

//******************************//
//******** Job Baseline ********//
//******************************//

public sealed class BaselineEstimate
{
    public string SourceBidNumber { get; set; } = string.Empty;
    public string ScopeSummary { get; set; } = string.Empty;
    public decimal OriginalRevenue { get; set; }
    public decimal EstimatedLaborCost { get; set; }
    public decimal EstimatedFieldLaborSale { get; set; }
    public decimal EstimatedMaterialCost { get; set; }
    public decimal EstimatedMaterialSale { get; set; }
    public decimal EstimatedTotalCost { get; set; }
    public decimal EstimatedFieldHours { get; set; }
    public decimal EstimatedAdminHours { get; set; }
    public decimal EstimatedEngineeringHours { get; set; }
    public decimal EstimatedInstallHours { get; set; }
    public decimal EstimatedDemoHours { get; set; }
    public decimal EstimatedTrimHours { get; set; }
    public decimal EstimatedTestHours { get; set; }
    public decimal EstimatedInstallCost { get; set; }
    public decimal EstimatedDemoCost { get; set; }
    public decimal EstimatedTrimCost { get; set; }
    public decimal EstimatedTestCost { get; set; }
    public decimal EstimatedInstallSale { get; set; }
    public decimal EstimatedDemoSale { get; set; }
    public decimal EstimatedTrimSale { get; set; }
    public decimal EstimatedTestSale { get; set; }
    public decimal EstimatedAdminCost { get; set; }
    public decimal EstimatedAdminSale { get; set; }
    public decimal EstimatedEngineeringCost { get; set; }
    public decimal EstimatedEngineeringSale { get; set; }
    public decimal EstimatedComponentCost { get; set; }
    public decimal EstimatedComponentSale { get; set; }
    public decimal EstimatedWireCost { get; set; }
    public decimal EstimatedWireSale { get; set; }
    public decimal EstimatedMaterialOnlyCost { get; set; }
    public decimal EstimatedMaterialOnlySale { get; set; }
    public List<WorkTask> AdministrativeTasks { get; set; } = [];
    public List<WorkTask> EngineeringTasks { get; set; } = [];
    public List<BidComponent> Components { get; set; } = [];
    public List<BidDemoItem> DemoItems { get; set; } = [];
    public List<BidMaterialItem> Materials { get; set; } = [];
    public List<JobBaselineLineItem> LineItems { get; set; } = [];
}

public sealed class JobBaselineLineItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ReferenceNumber { get; set; } = string.Empty;
    public string SourceSection { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = JobCostCodes.Other;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public string UnitLabel { get; set; } = "ea";
    public decimal EstimatedUnitCost { get; set; }
    public decimal EstimatedUnitSale { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal ActualUnitCost { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal EstimatedHours { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<JobBaselineActualPurchaseLine> ActualPurchaseLines { get; set; } = [];

    [JsonIgnore]
    public decimal EstimatedCost => Quantity * EstimatedUnitCost;

    [JsonIgnore]
    public decimal EstimatedSale => Quantity * EstimatedUnitSale;

    [JsonIgnore]
    public bool HasActualPurchaseLines => ActualPurchaseLines.Count > 0;

    [JsonIgnore]
    public decimal EffectiveActualQuantity => HasActualPurchaseLines
        ? ActualPurchaseLines.Sum(item => item.Quantity)
        : ActualQuantity;

    [JsonIgnore]
    public decimal EffectiveActualUnitCost => EffectiveActualQuantity <= 0m
        ? 0m
        : ActualCost / EffectiveActualQuantity;

    [JsonIgnore]
    public decimal ActualCost => HasActualPurchaseLines
        ? ActualPurchaseLines.Sum(item => item.ActualCost)
        : ActualQuantity * ActualUnitCost;
}

public sealed class JobBaselineActualPurchaseLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public string UnitLabel { get; set; } = "ea";
    public decimal ActualUnitCost { get; set; }
    public Guid? InvoiceId { get; set; }
    public string Notes { get; set; } = string.Empty;

    [JsonIgnore]
    public decimal ActualCost => Quantity * ActualUnitCost;
}

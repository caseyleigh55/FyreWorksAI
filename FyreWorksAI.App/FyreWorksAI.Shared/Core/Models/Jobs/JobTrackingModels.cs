using System.Text.Json.Serialization;

namespace FyreWorksAI.Shared.Core.Models.Jobs;

//******************************//
//******** Job Tracking ********//
//******************************//

public sealed class JobDeviceItem
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

public sealed class JobInvoiceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public string Vendor { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal InvoiceTotal { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<AttachmentRecord> Attachments { get; set; } = [];
}

public sealed class JobTimeEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime WorkDate { get; set; } = DateTime.Today;
    public string CrewMember { get; set; } = string.Empty;
    public bool IsOvernight { get; set; }
    public decimal Hours { get; set; } = 8m;
    public decimal HourlyRate { get; set; } = 115m;
    public string CostCode { get; set; } = "Field";
    public string Notes { get; set; } = string.Empty;

    [JsonIgnore]
    public decimal TotalCost => Hours * HourlyRate;
}

public sealed class JobMaterialPurchase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime PurchaseDate { get; set; } = DateTime.Today;
    public string Vendor { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ActualCost { get; set; }
    public Guid? BaselineLineItemId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitCost { get; set; }
    public decimal SalesTax { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<AttachmentRecord> Attachments { get; set; } = [];

    [JsonIgnore]
    public decimal Subtotal => Quantity * UnitCost;

    [JsonIgnore]
    public decimal TotalCost => Subtotal + SalesTax;
}

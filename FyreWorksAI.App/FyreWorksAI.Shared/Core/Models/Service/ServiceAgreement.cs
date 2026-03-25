using System.Text.Json.Serialization;

namespace FyreWorksAI.Shared.Core.Models.Service;

//******************************//
//******** Service Work ********//
//******************************//

public sealed class MonitoringPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime DueDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime ReceivedOn { get; set; } = DateTime.Today;
    public string Notes { get; set; } = string.Empty;
}

public sealed class ServiceCallRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateTime OpenedOn { get; set; } = DateTime.Today;
    public DateTime ScheduledFor { get; set; } = DateTime.Today;
    public DateTime CompletedOn { get; set; } = DateTime.Today;
    public string Status { get; set; } = "Open";
    public string Technician { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

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

public sealed class ServiceQuoteRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "Service Quote";
    public DateTime CreatedOn { get; set; } = DateTime.Today;
    public string Status { get; set; } = "Draft";
    public string Notes { get; set; } = string.Empty;
    public List<ServiceQuoteItem> Items { get; set; } = [];
}

public sealed class ServiceAgreement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AgreementNumber { get; set; } = string.Empty;
    public string AgreementName { get; set; } = "Monitoring Agreement";
    public Guid? ClientId { get; set; }
    public SiteInformation Site { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime ContractStart { get; set; } = DateTime.Today;
    public int ContractMonths { get; set; } = 24;
    public decimal MonthlyMonitoringAmount { get; set; } = 165m;
    public int InspectionIntervalMonths { get; set; } = 12;
    public DateTime NextInspectionDate { get; set; } = DateTime.Today.AddMonths(12);
    public string Notes { get; set; } = string.Empty;
    public List<MonitoringPayment> MonitoringPayments { get; set; } = [];
    public List<ServiceCallRecord> ServiceCalls { get; set; } = [];
    public List<ServiceQuoteRecord> Quotes { get; set; } = [];
    public List<AttachmentRecord> Attachments { get; set; } = [];
}

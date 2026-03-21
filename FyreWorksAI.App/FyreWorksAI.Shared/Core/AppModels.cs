using System.Text.Json.Serialization;

namespace FyreWorksAI.Shared;

public enum StorageMode
{
    TextFile,
    Sqlite,
    SqlServer
}

public sealed class FyreWorksWorkspace
{
    public AppSettings Settings { get; set; } = new();
    public List<ClientRecord> Clients { get; set; } = [];
    public List<LaborTemplate> Templates { get; set; } = [];
    public List<BidRecord> Bids { get; set; } = [];
    public List<JobRecord> Jobs { get; set; } = [];
    public List<ServiceAgreement> ServiceAgreements { get; set; } = [];
}

public sealed class AppSettings
{
    public StorageMode StorageMode { get; set; } = StorageMode.TextFile;
    public string StorageNotes { get; set; } = "Text-file storage is active today. The repository contracts are ready for future SQLite or SQL Server adapters when you want to expand beyond flat files.";
    public Guid? DefaultTemplateId { get; set; }
    public decimal FieldLaborRate { get; set; } = 115m;
    public decimal AdminLaborRate { get; set; } = 68m;
    public decimal EngineeringLaborRate { get; set; } = 96m;
    public decimal DefaultMarkupPercent { get; set; } = 22m;
    public int DefaultServiceContractMonths { get; set; } = 24;
    public int DefaultInspectionIntervalMonths { get; set; } = 12;
    public decimal DefaultMonthlyMonitoringAmount { get; set; } = 165m;
}

public sealed class ClientRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New Client";
    public string PrimaryContact { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string ServiceAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class SiteInformation
{
    public string SiteName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string OccupancyType { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    [JsonIgnore]
    public string SingleLineAddress
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(AddressLine1))
            {
                parts.Add(AddressLine1.Trim());
            }

            if (!string.IsNullOrWhiteSpace(AddressLine2))
            {
                parts.Add(AddressLine2.Trim());
            }

            var cityStatePostal = string.Join(", ", new[] { City, State }.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));
            if (!string.IsNullOrWhiteSpace(PostalCode))
            {
                cityStatePostal = string.IsNullOrWhiteSpace(cityStatePostal)
                    ? PostalCode.Trim()
                    : $"{cityStatePostal} {PostalCode.Trim()}";
            }

            if (!string.IsNullOrWhiteSpace(cityStatePostal))
            {
                parts.Add(cityStatePostal);
            }

            return string.Join(" | ", parts);
        }
    }
}

public sealed class LaborTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Standard Alarm Template";
    public string Notes { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public List<LaborRule> Rules { get; set; } = [];
}

public sealed class LaborRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LocationProfile { get; set; } = "Normal Area";
    public string InstallType { get; set; } = "No Pipe";
    public decimal InstallMinutes { get; set; } = 15m;
    public decimal DemoMinutes { get; set; } = 0m;
    public decimal TrimMinutes { get; set; } = 10m;
    public decimal TestMinutes { get; set; } = 5m;
    public string Notes { get; set; } = string.Empty;
}

public sealed class WorkTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public decimal EstimatedHours { get; set; } = 1m;
    public bool Complete { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class BidComponent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public string LocationProfile { get; set; } = "Normal Area";
    public string InstallType { get; set; } = "No Pipe";
    public decimal MaterialCostEach { get; set; }
    public decimal InstallMinutes { get; set; } = 15m;
    public decimal DemoMinutes { get; set; }
    public decimal TrimMinutes { get; set; } = 10m;
    public decimal TestMinutes { get; set; } = 5m;
    public string Notes { get; set; } = string.Empty;

    [JsonIgnore]
    public decimal TotalMinutes => Quantity * (InstallMinutes + DemoMinutes + TrimMinutes + TestMinutes);

    [JsonIgnore]
    public decimal TotalMaterialCost => Quantity * MaterialCostEach;
}

public sealed class BidMaterialItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Category { get; set; } = "Material";
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitCost { get; set; }
    public string Notes { get; set; } = string.Empty;

    [JsonIgnore]
    public decimal ExtendedCost => Quantity * UnitCost;
}

public sealed class BidRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BidNumber { get; set; } = string.Empty;
    public string ProjectName { get; set; } = "New Bid";
    public Guid? ClientId { get; set; }
    public SiteInformation Site { get; set; } = new();
    public string Status { get; set; } = "Draft";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.Today;
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);
    public Guid? TemplateId { get; set; }
    public decimal FieldLaborRate { get; set; } = 115m;
    public decimal AdminLaborRate { get; set; } = 68m;
    public decimal EngineeringLaborRate { get; set; } = 96m;
    public decimal MarkupPercent { get; set; } = 22m;
    public decimal ProposedRevenue { get; set; }
    public string ScopeSummary { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<WorkTask> AdministrativeTasks { get; set; } = [];
    public List<WorkTask> EngineeringTasks { get; set; } = [];
    public List<BidComponent> Components { get; set; } = [];
    public List<BidMaterialItem> Materials { get; set; } = [];
    public List<AttachmentRecord> Attachments { get; set; } = [];
}

public sealed class BaselineEstimate
{
    public string SourceBidNumber { get; set; } = string.Empty;
    public string ScopeSummary { get; set; } = string.Empty;
    public decimal OriginalRevenue { get; set; }
    public decimal EstimatedLaborCost { get; set; }
    public decimal EstimatedMaterialCost { get; set; }
    public decimal EstimatedTotalCost { get; set; }
    public decimal EstimatedFieldHours { get; set; }
    public decimal EstimatedAdminHours { get; set; }
    public decimal EstimatedEngineeringHours { get; set; }
    public List<WorkTask> AdministrativeTasks { get; set; } = [];
    public List<WorkTask> EngineeringTasks { get; set; } = [];
    public List<BidComponent> Components { get; set; } = [];
    public List<BidMaterialItem> Materials { get; set; } = [];
}

public sealed class JobTimeEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime WorkDate { get; set; } = DateTime.Today;
    public string CrewMember { get; set; } = string.Empty;
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
    public string ReceiptNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class ChangeOrderRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime ApprovedOn { get; set; } = DateTime.Today;
    public string Title { get; set; } = string.Empty;
    public decimal RevenueAmount { get; set; }
    public decimal EstimatedCostImpact { get; set; }
    public bool Approved { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
}

public sealed class ScheduleValueItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public decimal ScheduledValue { get; set; }
    public decimal BilledToDate { get; set; }
    public decimal PaidToDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class CommitmentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Vendor { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal CommittedAmount { get; set; }
    public decimal BilledAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);
    public string Notes { get; set; } = string.Empty;
}

public sealed class JobRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string JobNumber { get; set; } = string.Empty;
    public string ProjectName { get; set; } = "New Job";
    public Guid? ClientId { get; set; }
    public Guid? SourceBidId { get; set; }
    public SiteInformation Site { get; set; } = new();
    public string Status { get; set; } = "Planning";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.Today;
    public BaselineEstimate Baseline { get; set; } = new();
    public List<JobTimeEntry> TimeEntries { get; set; } = [];
    public List<JobMaterialPurchase> MaterialPurchases { get; set; } = [];
    public List<ChangeOrderRecord> ChangeOrders { get; set; } = [];
    public List<ScheduleValueItem> ScheduleOfValues { get; set; } = [];
    public List<CommitmentRecord> Commitments { get; set; } = [];
    public List<AttachmentRecord> Attachments { get; set; } = [];
    public string Notes { get; set; } = string.Empty;
}

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

public sealed class AttachmentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedOn { get; set; } = DateTime.Now;
}

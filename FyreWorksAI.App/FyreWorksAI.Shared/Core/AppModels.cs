using System.Text.Json.Serialization;

namespace FyreWorksAI.Shared;

public enum StorageMode
{
    TextFile,
    Sqlite,
    SqlServer
}

public enum TaskPricingMode
{
    Hourly,
    Fixed
}

public enum BidMaterialKind
{
    Unknown,
    Wire,
    Material
}

public enum PersonnelType
{
    Journeyman,
    Apprentice
}

public enum HourType
{
    Regular,
    Overnight
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
    public List<YearSequenceCounter> BidNumberCounters { get; set; } = [];
}

public sealed class YearSequenceCounter
{
    public int Year { get; set; }
    public int NextSequence { get; set; } = 1;
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
    public string ScopeOfWork { get; set; } = string.Empty;
    public string ParcelNumber { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = string.Empty;
    public string BuildingArea { get; set; } = string.Empty;
    public string NumberOfStories { get; set; } = string.Empty;
    public string OccupancyGroup { get; set; } = string.Empty;
    public string OccupantLoad { get; set; } = string.Empty;
    public string ConstructionType { get; set; } = string.Empty;
    public bool IsSprinklered { get; set; }

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
    public string BidNumberFormat { get; set; } = "BID-YY-NNNN";
    public decimal DefaultMarkupPercent { get; set; } = 22m;
    public decimal JourneymanRegularDirectRate { get; set; } = 65m;
    public decimal JourneymanRegularBilledRate { get; set; } = 90m;
    public decimal JourneymanOvernightDirectRate { get; set; } = 97.5m;
    public decimal JourneymanOvernightBilledRate { get; set; } = 135m;
    public decimal ApprenticeRegularDirectRate { get; set; } = 45m;
    public decimal ApprenticeRegularBilledRate { get; set; } = 65m;
    public decimal ApprenticeOvernightDirectRate { get; set; } = 67.5m;
    public decimal ApprenticeOvernightBilledRate { get; set; } = 97.5m;
    public decimal AdminDirectRate { get; set; } = 68m;
    public decimal AdminBilledRate { get; set; } = 95m;
    public decimal EngineeringDirectRate { get; set; } = 96m;
    public decimal EngineeringBilledRate { get; set; } = 130m;
    public List<LaborRule> Rules { get; set; } = [];
}

public sealed class LaborRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LocationProfile { get; set; } = "Normal Area";
    public string InstallType { get; set; } = "Normal";
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
    public decimal JourneymanRegularDirectRate { get; set; } = 65m;
    public decimal JourneymanRegularBilledRate { get; set; } = 90m;
    public decimal JourneymanOvernightDirectRate { get; set; } = 97.5m;
    public decimal JourneymanOvernightBilledRate { get; set; } = 135m;
    public decimal ApprenticeRegularDirectRate { get; set; } = 45m;
    public decimal ApprenticeRegularBilledRate { get; set; } = 65m;
    public decimal ApprenticeOvernightDirectRate { get; set; } = 67.5m;
    public decimal ApprenticeOvernightBilledRate { get; set; } = 97.5m;
    public decimal AdminDirectRate { get; set; } = 68m;
    public decimal AdminBilledRate { get; set; } = 95m;
    public decimal EngineeringDirectRate { get; set; } = 96m;
    public decimal EngineeringBilledRate { get; set; } = 130m;
    public List<BidLaborDistributionLine> LaborDistribution { get; set; } = [];
    public List<WorkTask> AdministrativeTasks { get; set; } = [];
    public List<WorkTask> EngineeringTasks { get; set; } = [];
    public List<BidComponent> Components { get; set; } = [];
    public List<BidDemoItem> DemoItems { get; set; } = [];
    public List<BidMaterialItem> Materials { get; set; } = [];
    public List<AttachmentRecord> Attachments { get; set; } = [];

    [JsonIgnore]
    public decimal AcceptedSalePrice
    {
        get => ProposedRevenue;
        set => ProposedRevenue = value;
    }
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
    public List<BidDemoItem> DemoItems { get; set; } = [];
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

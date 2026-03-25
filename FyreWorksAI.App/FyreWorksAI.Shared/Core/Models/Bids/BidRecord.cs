using System.Text.Json.Serialization;

namespace FyreWorksAI.Shared.Core.Models.Bids;

//******************************//
//******** Bid Record **********//
//******************************//

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
    public string Exclusions { get; set; } = string.Empty;
    public string ProposalSummary { get; set; } = string.Empty;
    public string ProposalClosing { get; set; } = string.Empty;
    public List<AttachmentRecord> Attachments { get; set; } = [];

    [JsonIgnore]
    public decimal AcceptedSalePrice
    {
        get => ProposedRevenue;
        set => ProposedRevenue = value;
    }
}

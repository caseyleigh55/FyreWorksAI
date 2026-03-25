namespace FyreWorksAI.Shared.Core.Models.Jobs;

//******************************//
//********* Job Record *********//
//******************************//

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
    public List<JobDeviceItem> JobDevices { get; set; } = [];
    public List<JobInvoiceRecord> Invoices { get; set; } = [];
    public List<JobTimeEntry> TimeEntries { get; set; } = [];
    public List<JobMaterialPurchase> MaterialPurchases { get; set; } = [];
    public List<ChangeOrderRecord> ChangeOrders { get; set; } = [];
    public List<ScheduleValueItem> ScheduleOfValues { get; set; } = [];
    public List<CommitmentRecord> Commitments { get; set; } = [];
    public List<AttachmentRecord> Attachments { get; set; } = [];
    public string Exclusions { get; set; } = string.Empty;
    public string ProposalSummary { get; set; } = string.Empty;
    public string ProposalClosing { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

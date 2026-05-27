namespace FyreWorksAI.Shared.Core.Models.Service;

//******************************//
//****** Service Quote *********//
//******************************//
public sealed class ServiceQuoteRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "Service Quote";
    public DateTime CreatedOn { get; set; } = DateTime.Today;
    public DateTime? AcceptedOn { get; set; }
    public string Status { get; set; } = "Draft";
    public List<ServiceQuoteLaborLine> LaborLines { get; set; } = [];

    // Legacy quote labor fields are preserved so older workspace data can be migrated forward.
    public decimal ServiceLaborHours { get; set; }
    public decimal ServiceLaborCostRate { get; set; }
    public decimal ServiceLaborSaleRate { get; set; }
    public decimal AdjustedSalePrice { get; set; }
    public string Notes { get; set; } = string.Empty;
    public Guid? ConvertedServiceCallId { get; set; }
    public List<ServiceQuoteItem> Items { get; set; } = [];
    public List<AttachmentRecord> Attachments { get; set; } = [];
}

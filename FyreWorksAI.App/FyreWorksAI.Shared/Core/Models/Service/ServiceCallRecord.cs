namespace FyreWorksAI.Shared.Core.Models.Service;

//******************************//
//****** Service Call **********//
//******************************//
public sealed class ServiceCallRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RootServiceCallId { get; set; }
    public int ReturnVisitSequence { get; set; }
    public string ServiceTicketNumber { get; set; } = string.Empty;
    public string ServiceJobNumber { get; set; } = string.Empty;
    public Guid? SourceQuoteId { get; set; }
    public string SourceQuoteTitle { get; set; } = string.Empty;
    public decimal SourceQuoteAmount { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime OpenedOn { get; set; } = DateTime.Today;
    public DateTime ScheduledFor { get; set; } = DateTime.Today;
    public DateTime CompletedOn { get; set; } = DateTime.Today;
    public string Status { get; set; } = "Open";
    public string Technician { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ServiceCallBillingRecord Billing { get; set; } = new();
    public List<AttachmentRecord> Attachments { get; set; } = [];
}

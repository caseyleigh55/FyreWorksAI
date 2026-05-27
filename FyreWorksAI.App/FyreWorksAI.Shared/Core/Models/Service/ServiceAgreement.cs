namespace FyreWorksAI.Shared.Core.Models.Service;

//******************************//
//****** Service Agreement *****//
//******************************//
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

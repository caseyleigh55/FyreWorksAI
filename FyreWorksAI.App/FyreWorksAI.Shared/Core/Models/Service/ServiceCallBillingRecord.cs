namespace FyreWorksAI.Shared.Core.Models.Service;

//******************************//
//****** Service Call Bill *****//
//******************************//
public sealed class ServiceCallBillingRecord
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal LaborHours { get; set; }
    public decimal LaborAmount { get; set; }
    public decimal MaterialAmount { get; set; }
    public decimal InvoiceAmount { get; set; }
    public decimal BilledAmount { get; set; }
    public DateTime? BilledOn { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime? PaidOn { get; set; }
    public string Notes { get; set; } = string.Empty;
}

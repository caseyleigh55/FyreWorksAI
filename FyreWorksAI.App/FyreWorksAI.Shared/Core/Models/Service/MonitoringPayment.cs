namespace FyreWorksAI.Shared.Core.Models.Service;

//******************************//
//***** Monitoring Payment *****//
//******************************//
public sealed class MonitoringPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime DueDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public decimal AmountBilled { get; set; }
    public DateTime? BilledOn { get; set; }
    public decimal ReceivedAmount { get; set; }
    public DateTime? ReceivedOn { get; set; }
    public bool IsPaid { get; set; }
    public string Notes { get; set; } = string.Empty;
}

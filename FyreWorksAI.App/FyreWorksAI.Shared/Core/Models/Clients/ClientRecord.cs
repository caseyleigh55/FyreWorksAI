namespace FyreWorksAI.Shared.Core.Models.Clients;

//******************************//
//******** Client Record *******//
//******************************//

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

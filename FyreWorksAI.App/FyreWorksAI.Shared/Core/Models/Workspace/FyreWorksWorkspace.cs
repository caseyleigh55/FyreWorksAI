namespace FyreWorksAI.Shared.Core.Models.Workspace;

//******************************//
//******** Workspace ***********//
//******************************//

public sealed class FyreWorksWorkspace
{
    public AppSettings Settings { get; set; } = new();
    public List<ClientRecord> Clients { get; set; } = [];
    public List<LaborTemplate> Templates { get; set; } = [];
    public List<BidRecord> Bids { get; set; } = [];
    public List<JobRecord> Jobs { get; set; } = [];
    public List<ServiceAgreement> ServiceAgreements { get; set; } = [];
}

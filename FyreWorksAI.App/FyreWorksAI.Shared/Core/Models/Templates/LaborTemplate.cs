namespace FyreWorksAI.Shared.Core.Models.Templates;

//******************************//
//******** Labor Setup *********//
//******************************//

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

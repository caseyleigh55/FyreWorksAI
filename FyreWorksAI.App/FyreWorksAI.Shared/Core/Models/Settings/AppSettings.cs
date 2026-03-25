namespace FyreWorksAI.Shared.Core.Models.Settings;

//******************************//
//******** App Settings ********//
//******************************//

public enum StorageMode
{
    TextFile,
    Sqlite,
    SqlServer
}

public sealed class AppSettings
{
    public StorageMode StorageMode { get; set; } = StorageMode.TextFile;
    public string StorageNotes { get; set; } = "Text-file storage is active today. The repository contracts are ready for future SQLite or SQL Server adapters when you want to expand beyond flat files.";
    public Guid? DefaultTemplateId { get; set; }
    public decimal FieldLaborRate { get; set; } = 115m;
    public decimal AdminLaborRate { get; set; } = 68m;
    public decimal EngineeringLaborRate { get; set; } = 96m;
    public decimal DefaultMarkupPercent { get; set; } = 22m;
    public int DefaultServiceContractMonths { get; set; } = 24;
    public int DefaultInspectionIntervalMonths { get; set; } = 12;
    public decimal DefaultMonthlyMonitoringAmount { get; set; } = 165m;
    public List<YearSequenceCounter> BidNumberCounters { get; set; } = [];
    public List<YearSequenceCounter> JobNumberCounters { get; set; } = [];
}

public sealed class YearSequenceCounter
{
    public int Year { get; set; }
    public int NextSequence { get; set; } = 1;
}

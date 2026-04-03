namespace FyreWorksAI.Shared.Core.Models.Jobs;

//******************************//
//******** Daily Log ***********//
//******************************//

public sealed class JobDailyLogRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime WorkDate { get; set; } = DateTime.Today;
    public string Description { get; set; } = "Daily Log";
    public List<AttachmentRecord> Attachments { get; set; } = [];
}

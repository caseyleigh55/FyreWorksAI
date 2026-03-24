using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;

namespace FyreWorksAI.Shared;

public sealed record PickedFile(string SourcePath, string FileName, string ContentType);

public interface IAttachmentService
{
    bool SupportsPicking { get; }
    bool SupportsOpening { get; }
    Task<IReadOnlyList<PickedFile>> PickFilesAsync();
    Task OpenAsync(string fullPath);
}

public interface IStoragePathResolver
{
    string GetRootDirectory();
}

public interface IWorkspaceLocationService
{
    bool SupportsOpeningDirectories { get; }
    Task OpenDirectoryAsync(string fullPath);
}

public interface IWorkspaceStorage
{
    string DataFilePath { get; }
    Task<FyreWorksWorkspace?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(FyreWorksWorkspace workspace, CancellationToken cancellationToken = default);
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFyreWorksCore(this IServiceCollection services)
    {
        services.AddSingleton<IWorkspaceStorage, TextFileWorkspaceStorage>();
        services.AddSingleton<WorkspaceStore>();
        return services;
    }
}

public sealed class TextFileWorkspaceStorage(IStoragePathResolver pathResolver) : IWorkspaceStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string DataFilePath => Path.Combine(pathResolver.GetRootDirectory(), "data", "fyreworks-data.txt");

    public async Task<FyreWorksWorkspace?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(DataFilePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(DataFilePath);
        return await JsonSerializer.DeserializeAsync<FyreWorksWorkspace>(stream, JsonOptions, cancellationToken);
    }

    public async Task SaveAsync(FyreWorksWorkspace workspace, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFilePath)!);
        await using var stream = File.Create(DataFilePath);
        await JsonSerializer.SerializeAsync(stream, workspace, JsonOptions, cancellationToken);
    }
}

public sealed class WorkspaceStore(
    IWorkspaceStorage storage,
    IStoragePathResolver pathResolver,
    IAttachmentService attachmentService,
    IWorkspaceLocationService workspaceLocationService)
{
    private static readonly JsonSerializerOptions CloneOptions = new()
    {
        WriteIndented = false
    };
    private static readonly Regex StandardJobNumberPattern = new(
        @"^JOB-(?<year>\d{2})-(?<sequence>\d{4})$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex LegacyJobNumberPattern = new(
        @"^JOB-(?<date>\d{8})-(?<sequence>\d{3,4})$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public FyreWorksWorkspace Workspace { get; private set; } = new();
    public bool IsInitialized { get; private set; }
    public string DataFilePath => storage.DataFilePath;
    public string RootDirectory => pathResolver.GetRootDirectory();
    public string AttachmentRootPath => Path.Combine(RootDirectory, "attachments");
    public string ExportRootPath => Path.Combine(RootDirectory, "exports");
    public string BackupRootPath => Path.Combine(RootDirectory, "backups");
    public bool CanPickAttachments => attachmentService.SupportsPicking;
    public bool CanOpenAttachments => attachmentService.SupportsOpening;
    public bool CanOpenStorageLocations => workspaceLocationService.SupportsOpeningDirectories;

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        Workspace = await storage.LoadAsync() ?? CreateDefaultWorkspace();
        EnsureDefaults(Workspace);
        IsInitialized = true;
        await SaveAsync();
    }

    public async Task SaveAsync()
    {
        EnsureDefaults(Workspace);
        await storage.SaveAsync(Workspace);
    }

    public ClientRecord? GetClient(Guid? clientId) =>
        clientId is null
            ? null
            : Workspace.Clients.FirstOrDefault(client => client.Id == clientId.Value);

    public LaborTemplate? GetTemplate(Guid? templateId) =>
        templateId is null
            ? null
            : Workspace.Templates.FirstOrDefault(template => template.Id == templateId.Value);

    public ClientRecord CreateClient(string? name = null)
    {
        var client = new ClientRecord
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"Client {Workspace.Clients.Count + 1}" : name.Trim()
        };

        Workspace.Clients.Insert(0, client);
        return client;
    }

    public BidRecord CreateBid()
    {
        var template = GetTemplate(Workspace.Settings.DefaultTemplateId) ?? Workspace.Templates.First(template => !template.IsArchived);
        var bid = new BidRecord
        {
            ProjectName = $"Bid {Workspace.Bids.Count + 1}",
            CreatedOn = DateTime.Today,
            DueDate = DateTime.Today.AddDays(14),
            TemplateId = template.Id,
            Site =
            {
                SiteName = "New Project Site"
            }
        };

        ApplyTemplateValuesToBid(bid, template);
        bid.BidNumber = GenerateBidNumber(template, bid.CreatedOn);
        EnsureLaborDistributionRows(bid);
        RebalanceLaborDistributionToCalculatedHours(bid);

        Workspace.Bids.Insert(0, bid);
        return bid;
    }

    public JobRecord CreateBlankJob()
    {
        var createdOn = DateTime.Today;
        var job = new JobRecord
        {
            JobNumber = GenerateJobNumber(createdOn),
            ProjectName = $"Job {Workspace.Jobs.Count + 1}",
            CreatedOn = createdOn,
            Baseline =
            {
                ScopeSummary = "Direct job entry without a converted bid."
            }
        };

        JobFinancialBuilder.EnsureJobDerivedData(job);
        Workspace.Jobs.Insert(0, job);
        return job;
    }

    public ServiceAgreement CreateServiceAgreement()
    {
        var agreement = new ServiceAgreement
        {
            AgreementNumber = GenerateNumber("SVC", Workspace.ServiceAgreements.Count + 1),
            AgreementName = $"Monitoring Agreement {Workspace.ServiceAgreements.Count + 1}",
            ContractStart = DateTime.Today,
            ContractMonths = Workspace.Settings.DefaultServiceContractMonths,
            MonthlyMonitoringAmount = Workspace.Settings.DefaultMonthlyMonitoringAmount,
            InspectionIntervalMonths = Workspace.Settings.DefaultInspectionIntervalMonths,
            NextInspectionDate = DateTime.Today.AddMonths(Workspace.Settings.DefaultInspectionIntervalMonths),
            Site =
            {
                SiteName = "Protected Premises"
            }
        };

        RegenerateMonitoringSchedule(agreement);
        Workspace.ServiceAgreements.Insert(0, agreement);
        return agreement;
    }

    public LaborTemplate CreateTemplate()
    {
        var template = CreateStarterTemplate($"Template {Workspace.Templates.Count + 1}");
        Workspace.Templates.Insert(0, template);
        return template;
    }

    public LaborTemplate CreateTemplateFromBid(BidRecord bid, string? templateName = null)
    {
        var sourceTemplate = GetTemplate(bid.TemplateId) ?? GetTemplate(Workspace.Settings.DefaultTemplateId);
        var template = sourceTemplate is null
            ? CreateStarterTemplate(string.Empty)
            : Clone(sourceTemplate);

        template.Id = Guid.NewGuid();
        template.Name = string.IsNullOrWhiteSpace(templateName)
            ? $"{bid.ProjectName} Profile"
            : templateName.Trim();
        template.IsArchived = false;
        template.DefaultMarkupPercent = bid.MarkupPercent;
        template.JourneymanRegularDirectRate = bid.JourneymanRegularDirectRate;
        template.JourneymanRegularBilledRate = bid.JourneymanRegularBilledRate;
        template.JourneymanOvernightDirectRate = bid.JourneymanOvernightDirectRate;
        template.JourneymanOvernightBilledRate = bid.JourneymanOvernightBilledRate;
        template.ApprenticeRegularDirectRate = bid.ApprenticeRegularDirectRate;
        template.ApprenticeRegularBilledRate = bid.ApprenticeRegularBilledRate;
        template.ApprenticeOvernightDirectRate = bid.ApprenticeOvernightDirectRate;
        template.ApprenticeOvernightBilledRate = bid.ApprenticeOvernightBilledRate;
        template.AdminDirectRate = bid.AdminDirectRate;
        template.AdminBilledRate = bid.AdminBilledRate;
        template.EngineeringDirectRate = bid.EngineeringDirectRate;
        template.EngineeringBilledRate = bid.EngineeringBilledRate;

        Workspace.Templates.Insert(0, template);
        bid.TemplateId = template.Id;
        return template;
    }

    public JobRecord ConvertBidToJob(BidRecord bid)
    {
        var existingJob = Workspace.Jobs.FirstOrDefault(job => job.SourceBidId == bid.Id);
        if (existingJob is not null)
        {
            return existingJob;
        }

        var createdOn = DateTime.Today;
        var jobNumber = GenerateJobNumber(createdOn);
        var baseline = JobFinancialBuilder.BuildBaselineFromBid(bid, jobNumber);

        var job = new JobRecord
        {
            JobNumber = jobNumber,
            ProjectName = bid.ProjectName,
            ClientId = bid.ClientId,
            SourceBidId = bid.Id,
            Site = Clone(bid.Site),
            CreatedOn = createdOn,
            Status = "Planning",
            IsActive = true,
            Notes = $"Converted from bid {bid.BidNumber}.",
            Baseline = baseline,
            Exclusions = bid.Exclusions,
            ProposalSummary = bid.ProposalSummary,
            ProposalClosing = bid.ProposalClosing
        };

        JobFinancialBuilder.EnsureJobDerivedData(job);
        Workspace.Jobs.Insert(0, job);
        bid.Status = "Awarded";
        return job;
    }

    public void ApplyTemplateToBid(BidRecord bid)
    {
        var template = GetTemplate(bid.TemplateId) ?? GetTemplate(Workspace.Settings.DefaultTemplateId);
        if (template is null)
        {
            return;
        }

        ApplyTemplateValuesToBid(bid, template);

        foreach (var component in bid.Components)
        {
            ApplyTemplateToComponent(component, template);
            if (component.UnitSale <= 0m)
            {
                component.UnitSale = EstimateMath.GetDefaultSaleFromMarkup(component.UnitCost, bid.MarkupPercent);
            }
        }

        foreach (var demoItem in bid.DemoItems)
        {
            ApplyTemplateToDemoItem(demoItem, template);
        }

        foreach (var material in bid.Materials)
        {
            if (material.UnitSale <= 0m)
            {
                material.UnitSale = EstimateMath.GetDefaultSaleFromMarkup(material.UnitCost, bid.MarkupPercent);
            }
        }

        RebalanceLaborDistributionToCalculatedHours(bid);
    }

    public void RebalanceBidLaborDistribution(BidRecord bid) =>
        RebalanceLaborDistributionToCalculatedHours(bid);

    public bool ApplyTemplateToComponent(BidComponent component, LaborTemplate? template)
    {
        var rule = FindRule(template, component.LocationProfile, component.InstallType);

        if (rule is null)
        {
            return false;
        }

        component.InstallMinutes = rule.InstallMinutes;
        component.DemoMinutes = rule.DemoMinutes;
        component.TrimMinutes = rule.TrimMinutes;
        component.TestMinutes = rule.TestMinutes;
        return true;
    }

    public bool ApplyTemplateToDemoItem(BidDemoItem demoItem, LaborTemplate? template)
    {
        var rule = FindRule(template, demoItem.LocationProfile, demoItem.InstallType);

        if (rule is null)
        {
            return false;
        }

        demoItem.DemoHoursEach = rule.DemoMinutes / 60m;
        return true;
    }

    public void RegenerateMonitoringSchedule(ServiceAgreement agreement)
    {
        agreement.ContractMonths = Math.Max(1, agreement.ContractMonths);
        agreement.InspectionIntervalMonths = Math.Max(1, agreement.InspectionIntervalMonths);

        var existingPayments = agreement.MonitoringPayments
            .GroupBy(payment => payment.DueDate.Date)
            .ToDictionary(group => group.Key, group => group.First());

        var rebuilt = new List<MonitoringPayment>();
        for (var monthIndex = 0; monthIndex < agreement.ContractMonths; monthIndex++)
        {
            var dueDate = new DateTime(agreement.ContractStart.Year, agreement.ContractStart.Month, 1)
                .AddMonths(monthIndex);
            var payment = existingPayments.TryGetValue(dueDate.Date, out var existing)
                ? existing
                : new MonitoringPayment
                {
                    DueDate = dueDate,
                    Amount = agreement.MonthlyMonitoringAmount,
                    ReceivedOn = dueDate
                };

            payment.DueDate = dueDate;
            payment.Amount = agreement.MonthlyMonitoringAmount;
            rebuilt.Add(payment);
        }

        agreement.MonitoringPayments = rebuilt;
        agreement.NextInspectionDate = agreement.ContractStart.AddMonths(agreement.InspectionIntervalMonths);
    }

    public async Task<int> AddAttachmentsAsync(List<AttachmentRecord> attachments, string area, Guid ownerId)
    {
        if (!attachmentService.SupportsPicking)
        {
            return 0;
        }

        var pickedFiles = await attachmentService.PickFilesAsync();
        if (pickedFiles.Count == 0)
        {
            return 0;
        }

        var targetDirectory = Path.Combine(AttachmentRootPath, area, ownerId.ToString("N"));
        Directory.CreateDirectory(targetDirectory);

        foreach (var pickedFile in pickedFiles)
        {
            if (string.IsNullOrWhiteSpace(pickedFile.SourcePath) || !File.Exists(pickedFile.SourcePath))
            {
                continue;
            }

            var safeFileName = MakeSafeFileName(pickedFile.FileName);
            var storedFileName = $"{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}-{safeFileName}";
            var fullDestinationPath = Path.Combine(targetDirectory, storedFileName);
            File.Copy(pickedFile.SourcePath, fullDestinationPath, overwrite: true);

            attachments.Add(new AttachmentRecord
            {
                OriginalFileName = pickedFile.FileName,
                StoredFileName = storedFileName,
                RelativePath = Path.Combine(area, ownerId.ToString("N"), storedFileName),
                ContentType = pickedFile.ContentType,
                UploadedOn = DateTime.Now
            });
        }

        return pickedFiles.Count;
    }

    public async Task OpenAttachmentAsync(AttachmentRecord attachment)
    {
        if (!attachmentService.SupportsOpening)
        {
            return;
        }

        var fullPath = Path.Combine(AttachmentRootPath, attachment.RelativePath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        await attachmentService.OpenAsync(fullPath);
    }

    public void RemoveAttachment(List<AttachmentRecord> attachments, AttachmentRecord attachment)
    {
        var fullPath = Path.Combine(AttachmentRootPath, attachment.RelativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        attachments.RemoveAll(existing => existing.Id == attachment.Id);
    }

    public async Task<string> ExportJobCostReportAsync(JobRecord job)
    {
        Directory.CreateDirectory(ExportRootPath);
        var fileName = $"{MakeSafeFileName(job.JobNumber)}-{DateTime.Now:yyyyMMddHHmmss}.txt";
        var fullPath = Path.Combine(ExportRootPath, fileName);
        await File.WriteAllTextAsync(fullPath, JobFinancialBuilder.BuildJobCostReport(job, GetClient(job.ClientId)));
        return fullPath;
    }

    public async Task<string> ExportBidProposalAsync(BidRecord bid)
    {
        Directory.CreateDirectory(ExportRootPath);
        var fileName = $"{MakeSafeFileName(bid.BidNumber)}-proposal-{DateTime.Now:yyyyMMddHHmmss}.txt";
        var fullPath = Path.Combine(ExportRootPath, fileName);
        await File.WriteAllTextAsync(fullPath, JobFinancialBuilder.BuildBidProposal(bid, GetClient(bid.ClientId)));
        return fullPath;
    }

    public void SyncJobFinancials(JobRecord job) =>
        SyncLinkedJobBaseline(job);

    public async Task<string> CreateBackupAsync()
    {
        await SaveAsync();
        Directory.CreateDirectory(BackupRootPath);
        var backupPath = Path.Combine(BackupRootPath, $"fyreworks-backup-{DateTime.Now:yyyyMMddHHmmss}.txt");
        File.Copy(DataFilePath, backupPath, overwrite: true);
        return backupPath;
    }

    public bool DeleteBid(Guid bidId)
    {
        var bid = Workspace.Bids.FirstOrDefault(item => item.Id == bidId);
        if (bid is null)
        {
            return false;
        }

        Workspace.Bids.Remove(bid);
        DeleteAttachmentDirectory("bids", bid.Id);
        return true;
    }

    public bool DeleteJob(Guid jobId)
    {
        var job = Workspace.Jobs.FirstOrDefault(item => item.Id == jobId);
        if (job is null)
        {
            return false;
        }

        Workspace.Jobs.Remove(job);
        DeleteAttachmentDirectory("jobs", job.Id);
        return true;
    }

    public Task OpenDataDirectoryAsync() =>
        OpenDirectoryAsync(Path.GetDirectoryName(DataFilePath)!);

    public Task OpenAttachmentDirectoryAsync() =>
        OpenDirectoryAsync(AttachmentRootPath);

    public Task OpenExportDirectoryAsync() =>
        OpenDirectoryAsync(ExportRootPath);

    public Task OpenBackupDirectoryAsync() =>
        OpenDirectoryAsync(BackupRootPath);

    private void ApplyTemplateValuesToBid(BidRecord bid, LaborTemplate template)
    {
        bid.TemplateId = template.Id;
        bid.MarkupPercent = template.DefaultMarkupPercent;
        bid.JourneymanRegularDirectRate = template.JourneymanRegularDirectRate;
        bid.JourneymanRegularBilledRate = template.JourneymanRegularBilledRate;
        bid.JourneymanOvernightDirectRate = template.JourneymanOvernightDirectRate;
        bid.JourneymanOvernightBilledRate = template.JourneymanOvernightBilledRate;
        bid.ApprenticeRegularDirectRate = template.ApprenticeRegularDirectRate;
        bid.ApprenticeRegularBilledRate = template.ApprenticeRegularBilledRate;
        bid.ApprenticeOvernightDirectRate = template.ApprenticeOvernightDirectRate;
        bid.ApprenticeOvernightBilledRate = template.ApprenticeOvernightBilledRate;
        bid.AdminDirectRate = template.AdminDirectRate;
        bid.AdminBilledRate = template.AdminBilledRate;
        bid.EngineeringDirectRate = template.EngineeringDirectRate;
        bid.EngineeringBilledRate = template.EngineeringBilledRate;
        bid.FieldLaborRate = template.JourneymanRegularDirectRate;
        bid.AdminLaborRate = template.AdminDirectRate;
        bid.EngineeringLaborRate = template.EngineeringDirectRate;
    }

    private async Task OpenDirectoryAsync(string fullPath)
    {
        Directory.CreateDirectory(fullPath);
        await workspaceLocationService.OpenDirectoryAsync(fullPath);
    }

    private void DeleteAttachmentDirectory(string area, Guid ownerId)
    {
        var targetDirectory = Path.Combine(AttachmentRootPath, area, ownerId.ToString("N"));
        if (Directory.Exists(targetDirectory))
        {
            Directory.Delete(targetDirectory, recursive: true);
        }
    }

    private int GetNextBidSequence(int year)
    {
        Workspace.Settings.BidNumberCounters ??= [];
        var counter = Workspace.Settings.BidNumberCounters.FirstOrDefault(item => item.Year == year);
        if (counter is null)
        {
            counter = new YearSequenceCounter { Year = year };
            Workspace.Settings.BidNumberCounters.Add(counter);
        }

        var minimumSequence = Workspace.Bids.Count(bid => bid.CreatedOn.Year == year) + 1;
        counter.NextSequence = Math.Max(counter.NextSequence, minimumSequence);

        var current = counter.NextSequence;
        counter.NextSequence++;
        return current;
    }

    private int GetNextJobSequence(int year)
    {
        Workspace.Settings.JobNumberCounters ??= [];
        var counter = Workspace.Settings.JobNumberCounters.FirstOrDefault(item => item.Year == year);
        if (counter is null)
        {
            counter = new YearSequenceCounter { Year = year };
            Workspace.Settings.JobNumberCounters.Add(counter);
        }

        var minimumSequence = Workspace.Jobs.Count(job => job.CreatedOn.Year == year) + 1;
        counter.NextSequence = Math.Max(counter.NextSequence, minimumSequence);

        var current = counter.NextSequence;
        counter.NextSequence++;
        return current;
    }

    private string GenerateBidNumber(LaborTemplate template, DateTime createdOn)
    {
        var year = createdOn.Year;
        var sequence = GetNextBidSequence(year);
        var format = string.IsNullOrWhiteSpace(template.BidNumberFormat) ? "BID-YY-NNNN" : template.BidNumberFormat.Trim();
        var result = format
            .Replace("YYYY", year.ToString("0000", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("YY", (year % 100).ToString("00", CultureInfo.InvariantCulture), StringComparison.Ordinal);

        var firstN = result.IndexOf('N');
        if (firstN >= 0)
        {
            var count = 0;
            for (var index = firstN; index < result.Length && result[index] == 'N'; index++)
            {
                count++;
            }

            result = result.Replace(new string('N', count), sequence.ToString(new string('0', count), CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }
        else
        {
            result = $"{result}-{sequence:0000}";
        }

        return result;
    }

    private string GenerateJobNumber(DateTime createdOn)
    {
        var year = createdOn.Year;
        var sequence = GetNextJobSequence(year);
        return FormatJobNumber(year, sequence);
    }

    private static string BuildJobCostReport(JobRecord job)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Job Cost Export - {job.JobNumber}");
        builder.AppendLine($"Project: {job.ProjectName}");
        builder.AppendLine($"Generated: {DateTime.Now:G}");
        builder.AppendLine();
        builder.AppendLine("Baseline");
        builder.AppendLine($"  Original revenue: {EstimateMath.GetCurrency(job.Baseline.OriginalRevenue)}");
        builder.AppendLine($"  Estimated total cost: {EstimateMath.GetCurrency(job.Baseline.EstimatedTotalCost)}");
        builder.AppendLine($"  Estimated field hours: {job.Baseline.EstimatedFieldHours:N2}");
        builder.AppendLine();
        builder.AppendLine("Actuals");
        builder.AppendLine($"  Labor cost to date: {EstimateMath.GetCurrency(EstimateMath.GetJobActualLaborCost(job))}");
        builder.AppendLine($"  Material cost to date: {EstimateMath.GetCurrency(EstimateMath.GetJobActualMaterialCost(job))}");
        builder.AppendLine($"  Commitment billings: {EstimateMath.GetCurrency(EstimateMath.GetJobBilledCommitments(job))}");
        builder.AppendLine($"  Contract revenue incl. approved COs: {EstimateMath.GetCurrency(EstimateMath.GetJobRevenue(job))}");
        builder.AppendLine($"  Actual cost to date: {EstimateMath.GetCurrency(EstimateMath.GetJobActualCost(job))}");
        builder.AppendLine($"  Exposure including commitments: {EstimateMath.GetCurrency(EstimateMath.GetJobCommittedExposure(job))}");
        builder.AppendLine($"  Profit to date: {EstimateMath.GetCurrency(EstimateMath.GetJobProfit(job))}");
        builder.AppendLine($"  Margin to date: {EstimateMath.GetPercent(EstimateMath.GetJobMarginPercent(job))}");
        builder.AppendLine();
        builder.AppendLine("Schedule of Values");
        foreach (var item in job.ScheduleOfValues)
        {
            builder.AppendLine($"  {item.Description}: scheduled {EstimateMath.GetCurrency(item.ScheduledValue)}, billed {EstimateMath.GetCurrency(item.BilledToDate)}, paid {EstimateMath.GetCurrency(item.PaidToDate)}");
        }

        return builder.ToString();
    }

    private static string GenerateNumber(string prefix, int sequence) =>
        $"{prefix}-{DateTime.Today:yyyyMMdd}-{sequence:000}";

    private static string MakeSafeFileName(string fileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(fileName.Length);
        foreach (var character in fileName)
        {
            builder.Append(invalidCharacters.Contains(character) ? '_' : character);
        }

        return builder.ToString();
    }

    private static T Clone<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, CloneOptions);
        return JsonSerializer.Deserialize<T>(json, CloneOptions)!;
    }

    private static FyreWorksWorkspace CreateDefaultWorkspace()
    {
        var workspace = new FyreWorksWorkspace();
        EnsureDefaults(workspace);
        return workspace;
    }

    private static void EnsureDefaults(FyreWorksWorkspace workspace)
    {
        workspace.Settings ??= new AppSettings();
        workspace.Settings.BidNumberCounters ??= [];
        workspace.Settings.JobNumberCounters ??= [];
        workspace.Clients ??= [];
        workspace.Templates ??= [];
        workspace.Bids ??= [];
        workspace.Jobs ??= [];
        workspace.ServiceAgreements ??= [];

        NormalizeJobNumbers(workspace);

        if (workspace.Templates.Count == 0)
        {
            var starterTemplate = CreateStarterTemplate("Standard Device Labor");
            workspace.Templates.Add(starterTemplate);
            workspace.Settings.DefaultTemplateId = starterTemplate.Id;
        }

        foreach (var template in workspace.Templates)
        {
            template.Rules ??= [];
            if (string.IsNullOrWhiteSpace(template.BidNumberFormat))
            {
                template.BidNumberFormat = "BID-YY-NNNN";
            }

            if (template.DefaultMarkupPercent <= 0m)
            {
                template.DefaultMarkupPercent = workspace.Settings.DefaultMarkupPercent;
            }

            if (template.JourneymanRegularDirectRate <= 0m)
            {
                template.JourneymanRegularDirectRate = workspace.Settings.FieldLaborRate;
            }

            if (template.JourneymanRegularBilledRate <= 0m)
            {
                template.JourneymanRegularBilledRate = template.JourneymanRegularDirectRate;
            }

            if (template.JourneymanOvernightDirectRate <= 0m)
            {
                template.JourneymanOvernightDirectRate = template.JourneymanRegularDirectRate * 1.5m;
            }

            if (template.JourneymanOvernightBilledRate <= 0m)
            {
                template.JourneymanOvernightBilledRate = template.JourneymanRegularBilledRate * 1.5m;
            }

            if (template.ApprenticeRegularDirectRate <= 0m)
            {
                template.ApprenticeRegularDirectRate = Math.Max(1m, template.JourneymanRegularDirectRate * 0.7m);
            }

            if (template.ApprenticeRegularBilledRate <= 0m)
            {
                template.ApprenticeRegularBilledRate = Math.Max(1m, template.JourneymanRegularBilledRate * 0.72m);
            }

            if (template.ApprenticeOvernightDirectRate <= 0m)
            {
                template.ApprenticeOvernightDirectRate = template.ApprenticeRegularDirectRate * 1.5m;
            }

            if (template.ApprenticeOvernightBilledRate <= 0m)
            {
                template.ApprenticeOvernightBilledRate = template.ApprenticeRegularBilledRate * 1.5m;
            }

            if (template.AdminDirectRate <= 0m)
            {
                template.AdminDirectRate = workspace.Settings.AdminLaborRate;
            }

            if (template.AdminBilledRate <= 0m)
            {
                template.AdminBilledRate = template.AdminDirectRate;
            }

            if (template.EngineeringDirectRate <= 0m)
            {
                template.EngineeringDirectRate = workspace.Settings.EngineeringLaborRate;
            }

            if (template.EngineeringBilledRate <= 0m)
            {
                template.EngineeringBilledRate = template.EngineeringDirectRate;
            }
        }

        if (workspace.Settings.DefaultTemplateId is null || workspace.Templates.All(template => template.Id != workspace.Settings.DefaultTemplateId))
        {
            workspace.Settings.DefaultTemplateId = workspace.Templates.FirstOrDefault(template => !template.IsArchived)?.Id ?? workspace.Templates.First().Id;
        }

        var defaultTemplate = workspace.Templates.First(template => template.Id == workspace.Settings.DefaultTemplateId);

        foreach (var bid in workspace.Bids)
        {
            bid.Site ??= new SiteInformation();
            bid.LaborDistribution ??= [];
            bid.AdministrativeTasks ??= [];
            bid.EngineeringTasks ??= [];
            bid.Components ??= [];
            bid.DemoItems ??= [];
            bid.Materials ??= [];
            bid.Attachments ??= [];

            if (string.IsNullOrWhiteSpace(bid.Site.ScopeOfWork) && !string.IsNullOrWhiteSpace(bid.ScopeSummary))
            {
                bid.Site.ScopeOfWork = bid.ScopeSummary;
            }

            if (string.IsNullOrWhiteSpace(bid.Site.OccupancyGroup) && !string.IsNullOrWhiteSpace(bid.Site.OccupancyType))
            {
                bid.Site.OccupancyGroup = bid.Site.OccupancyType;
            }

            if (!string.IsNullOrWhiteSpace(bid.Site.Notes))
            {
                bid.Notes = string.IsNullOrWhiteSpace(bid.Notes)
                    ? bid.Site.Notes
                    : $"{bid.Notes}{Environment.NewLine}{Environment.NewLine}Site notes: {bid.Site.Notes}";
                bid.Site.Notes = string.Empty;
            }

            if (bid.TemplateId is null || workspace.Templates.All(template => template.Id != bid.TemplateId))
            {
                bid.TemplateId = workspace.Settings.DefaultTemplateId;
            }

            var template = workspace.Templates.First(template => template.Id == bid.TemplateId);

            if (bid.MarkupPercent <= 0m)
            {
                bid.MarkupPercent = template.DefaultMarkupPercent > 0m ? template.DefaultMarkupPercent : defaultTemplate.DefaultMarkupPercent;
            }

            if (bid.JourneymanRegularDirectRate <= 0m)
            {
                bid.JourneymanRegularDirectRate = bid.FieldLaborRate > 0m ? bid.FieldLaborRate : template.JourneymanRegularDirectRate;
            }

            if (bid.JourneymanRegularBilledRate <= 0m)
            {
                bid.JourneymanRegularBilledRate = bid.FieldLaborRate > 0m ? bid.FieldLaborRate : template.JourneymanRegularBilledRate;
            }

            if (bid.JourneymanOvernightDirectRate <= 0m)
            {
                bid.JourneymanOvernightDirectRate = template.JourneymanOvernightDirectRate;
            }

            if (bid.JourneymanOvernightBilledRate <= 0m)
            {
                bid.JourneymanOvernightBilledRate = template.JourneymanOvernightBilledRate;
            }

            if (bid.ApprenticeRegularDirectRate <= 0m)
            {
                bid.ApprenticeRegularDirectRate = template.ApprenticeRegularDirectRate;
            }

            if (bid.ApprenticeRegularBilledRate <= 0m)
            {
                bid.ApprenticeRegularBilledRate = template.ApprenticeRegularBilledRate;
            }

            if (bid.ApprenticeOvernightDirectRate <= 0m)
            {
                bid.ApprenticeOvernightDirectRate = template.ApprenticeOvernightDirectRate;
            }

            if (bid.ApprenticeOvernightBilledRate <= 0m)
            {
                bid.ApprenticeOvernightBilledRate = template.ApprenticeOvernightBilledRate;
            }

            if (bid.AdminDirectRate <= 0m)
            {
                bid.AdminDirectRate = bid.AdminLaborRate > 0m ? bid.AdminLaborRate : template.AdminDirectRate;
            }

            if (bid.AdminBilledRate <= 0m)
            {
                bid.AdminBilledRate = bid.AdminLaborRate > 0m ? bid.AdminLaborRate : template.AdminBilledRate;
            }

            if (bid.EngineeringDirectRate <= 0m)
            {
                bid.EngineeringDirectRate = bid.EngineeringLaborRate > 0m ? bid.EngineeringLaborRate : template.EngineeringDirectRate;
            }

            if (bid.EngineeringBilledRate <= 0m)
            {
                bid.EngineeringBilledRate = bid.EngineeringLaborRate > 0m ? bid.EngineeringLaborRate : template.EngineeringBilledRate;
            }

            bid.FieldLaborRate = bid.JourneymanRegularDirectRate;
            bid.AdminLaborRate = bid.AdminDirectRate;
            bid.EngineeringLaborRate = bid.EngineeringDirectRate;

            foreach (var task in bid.AdministrativeTasks.Concat(bid.EngineeringTasks))
            {
                if (task.PricingMode == TaskPricingMode.Fixed && task.SalePrice <= 0m && task.CostPrice > 0m)
                {
                    task.SalePrice = EstimateMath.GetDefaultSaleFromMarkup(task.CostPrice, bid.MarkupPercent);
                }
            }

            foreach (var component in bid.Components)
            {
                if (component.UnitSale <= 0m && component.UnitCost > 0m)
                {
                    component.UnitSale = EstimateMath.GetDefaultSaleFromMarkup(component.UnitCost, bid.MarkupPercent);
                }
            }

            foreach (var demoItem in bid.DemoItems)
            {
                if (string.IsNullOrWhiteSpace(demoItem.Name))
                {
                    demoItem.Name = "Demo Item";
                }
            }

            foreach (var material in bid.Materials)
            {
                if (material.Kind == BidMaterialKind.Unknown)
                {
                    material.Kind = string.Equals(material.Category, "Wire", StringComparison.OrdinalIgnoreCase)
                        ? BidMaterialKind.Wire
                        : BidMaterialKind.Material;
                }

                if (material.UnitSale <= 0m && material.UnitCost > 0m)
                {
                    material.UnitSale = EstimateMath.GetDefaultSaleFromMarkup(material.UnitCost, bid.MarkupPercent);
                }
            }

            EnsureLaborDistributionRows(bid);
            if (EstimateMath.GetBidAllocatedFieldHours(bid) <= 0m && EstimateMath.GetBidCalculatedFieldHours(bid) > 0m)
            {
                RebalanceLaborDistributionToCalculatedHours(bid);
            }
        }

        foreach (var job in workspace.Jobs)
        {
            job.Site ??= new SiteInformation();
            job.Baseline ??= new BaselineEstimate();
            job.Baseline.AdministrativeTasks ??= [];
            job.Baseline.EngineeringTasks ??= [];
            job.Baseline.Components ??= [];
            job.Baseline.DemoItems ??= [];
            job.Baseline.Materials ??= [];
            job.Baseline.LineItems ??= [];
            job.JobDevices ??= [];
            job.Invoices ??= [];
            job.TimeEntries ??= [];
            job.MaterialPurchases ??= [];
            job.ChangeOrders ??= [];
            job.ScheduleOfValues ??= [];
            job.Commitments ??= [];
            job.Attachments ??= [];
            if (job.SourceBidId is not null)
            {
                var bid = workspace.Bids.FirstOrDefault(item => item.Id == job.SourceBidId.Value);
                if (bid is not null)
                {
                    JobFinancialBuilder.RefreshBaselineFromBid(job, bid);
                }
            }

            JobFinancialBuilder.EnsureJobDerivedData(job);
        }

        foreach (var agreement in workspace.ServiceAgreements)
        {
            agreement.Site ??= new SiteInformation();
            agreement.MonitoringPayments ??= [];
            agreement.ServiceCalls ??= [];
            agreement.Quotes ??= [];
            agreement.Attachments ??= [];
            if (agreement.ContractMonths <= 0)
            {
                agreement.ContractMonths = workspace.Settings.DefaultServiceContractMonths;
            }

            if (agreement.MonthlyMonitoringAmount <= 0m)
            {
                agreement.MonthlyMonitoringAmount = workspace.Settings.DefaultMonthlyMonitoringAmount;
            }

            if (agreement.InspectionIntervalMonths <= 0)
            {
                agreement.InspectionIntervalMonths = workspace.Settings.DefaultInspectionIntervalMonths;
            }

            if (agreement.MonitoringPayments.Count == 0)
            {
                var store = new WorkspaceStoreBootstrapper();
                store.RegenerateMonitoringSchedule(agreement);
            }
        }
    }

    private static void NormalizeJobNumbers(FyreWorksWorkspace workspace)
    {
        workspace.Settings.JobNumberCounters ??= [];

        var nextSequenceByYear = new Dictionary<int, int>();
        foreach (var counter in workspace.Settings.JobNumberCounters.Where(counter => counter.Year > 0))
        {
            if (nextSequenceByYear.TryGetValue(counter.Year, out var existing))
            {
                nextSequenceByYear[counter.Year] = Math.Max(existing, Math.Max(1, counter.NextSequence));
            }
            else
            {
                nextSequenceByYear[counter.Year] = Math.Max(1, counter.NextSequence);
            }
        }

        var usedSequencesByYear = new Dictionary<int, HashSet<int>>();
        foreach (var job in workspace.Jobs
                     .OrderBy(job => job.CreatedOn)
                     .ThenBy(job => job.JobNumber, StringComparer.OrdinalIgnoreCase))
        {
            var (year, preferredSequence) = ParseExistingJobNumber(job.JobNumber, job.CreatedOn);
            if (!usedSequencesByYear.TryGetValue(year, out var usedSequences))
            {
                usedSequences = [];
                usedSequencesByYear[year] = usedSequences;
            }

            var nextSequence = nextSequenceByYear.TryGetValue(year, out var trackedNextSequence)
                ? Math.Max(1, trackedNextSequence)
                : 1;

            var assignedSequence = preferredSequence > 0 && !usedSequences.Contains(preferredSequence)
                ? preferredSequence
                : GetNextAvailableSequence(usedSequences, nextSequence);

            usedSequences.Add(assignedSequence);
            nextSequenceByYear[year] = Math.Max(nextSequence, assignedSequence + 1);
            job.JobNumber = FormatJobNumber(year, assignedSequence);
        }

        workspace.Settings.JobNumberCounters = nextSequenceByYear
            .OrderBy(item => item.Key)
            .Select(item => new YearSequenceCounter
            {
                Year = item.Key,
                NextSequence = Math.Max(1, item.Value)
            })
            .ToList();
    }

    private static (int Year, int Sequence) ParseExistingJobNumber(string? jobNumber, DateTime createdOn)
    {
        if (!string.IsNullOrWhiteSpace(jobNumber))
        {
            var trimmed = jobNumber.Trim();
            var standardMatch = StandardJobNumberPattern.Match(trimmed);
            if (standardMatch.Success)
            {
                var shortYear = int.Parse(standardMatch.Groups["year"].Value, CultureInfo.InvariantCulture);
                var sequence = int.Parse(standardMatch.Groups["sequence"].Value, CultureInfo.InvariantCulture);
                return (2000 + shortYear, sequence);
            }

            var legacyMatch = LegacyJobNumberPattern.Match(trimmed);
            if (legacyMatch.Success)
            {
                var fullYear = int.Parse(legacyMatch.Groups["date"].Value[..4], CultureInfo.InvariantCulture);
                var sequence = int.Parse(legacyMatch.Groups["sequence"].Value, CultureInfo.InvariantCulture);
                return (fullYear, sequence);
            }
        }

        return (Math.Max(2000, createdOn.Year), 0);
    }

    private static int GetNextAvailableSequence(HashSet<int> usedSequences, int startingSequence)
    {
        var candidate = Math.Max(1, startingSequence);
        while (usedSequences.Contains(candidate))
        {
            candidate++;
        }

        return candidate;
    }

    private static string FormatJobNumber(int year, int sequence) =>
        $"JOB-{(year % 100):00}-{sequence:0000}";

    private static void EnsureLaborDistributionRows(BidRecord bid)
    {
        var requiredRows = new[]
        {
            (PersonnelType.Journeyman, HourType.Regular),
            (PersonnelType.Journeyman, HourType.Overnight),
            (PersonnelType.Apprentice, HourType.Regular),
            (PersonnelType.Apprentice, HourType.Overnight)
        };

        foreach (var (personnelType, hourType) in requiredRows)
        {
            if (bid.LaborDistribution.All(line => line.PersonnelType != personnelType || line.HourType != hourType))
            {
                bid.LaborDistribution.Add(new BidLaborDistributionLine
                {
                    PersonnelType = personnelType,
                    HourType = hourType
                });
            }
        }

        bid.LaborDistribution = bid.LaborDistribution
            .OrderBy(line => line.PersonnelType)
            .ThenBy(line => line.HourType)
            .ToList();
    }

    private static void RebalanceLaborDistributionToCalculatedHours(BidRecord bid)
    {
        EnsureLaborDistributionRows(bid);
        RebalanceDistributionColumn(bid.LaborDistribution, line => line.InstallHours, (line, value) => line.InstallHours = value, EstimateMath.GetBidInstallHours(bid));
        RebalanceDistributionColumn(bid.LaborDistribution, line => line.DemoHours, (line, value) => line.DemoHours = value, EstimateMath.GetBidDemoHours(bid));
        RebalanceDistributionColumn(bid.LaborDistribution, line => line.TrimHours, (line, value) => line.TrimHours = value, EstimateMath.GetBidTrimHours(bid));
        RebalanceDistributionColumn(bid.LaborDistribution, line => line.TestHours, (line, value) => line.TestHours = value, EstimateMath.GetBidTestHours(bid));
    }

    private void SyncLinkedJobBaseline(JobRecord job)
    {
        if (job.SourceBidId is not null)
        {
            var bid = Workspace.Bids.FirstOrDefault(item => item.Id == job.SourceBidId.Value);
            if (bid is not null)
            {
                JobFinancialBuilder.RefreshBaselineFromBid(job, bid);
            }
        }

        JobFinancialBuilder.EnsureJobDerivedData(job);
    }

    private static void RebalanceDistributionColumn(
        List<BidLaborDistributionLine> lines,
        Func<BidLaborDistributionLine, decimal> selector,
        Action<BidLaborDistributionLine, decimal> assign,
        decimal targetTotal)
    {
        targetTotal = EstimateMath.RoundHours(targetTotal);
        var defaultLine = lines.First(line => line.PersonnelType == PersonnelType.Journeyman && line.HourType == HourType.Regular);

        if (targetTotal <= 0m)
        {
            foreach (var line in lines)
            {
                assign(line, 0m);
            }

            return;
        }

        var currentTotal = lines.Sum(selector);
        if (currentTotal <= 0m)
        {
            foreach (var line in lines)
            {
                assign(line, 0m);
            }

            assign(defaultLine, targetTotal);
            return;
        }

        var scale = targetTotal / currentTotal;
        foreach (var line in lines)
        {
            assign(line, EstimateMath.RoundHours(selector(line) * scale));
        }

        var delta = targetTotal - lines.Sum(selector);
        if (delta > 0m)
        {
            assign(defaultLine, EstimateMath.RoundHours(selector(defaultLine) + delta));
        }
        else if (delta < 0m)
        {
            var remaining = Math.Abs(delta);
            foreach (var line in lines.OrderByDescending(selector))
            {
                if (remaining <= 0m)
                {
                    break;
                }

                var currentValue = selector(line);
                if (currentValue <= 0m)
                {
                    continue;
                }

                var reduction = Math.Min(currentValue, remaining);
                reduction = EstimateMath.RoundHours(reduction);
                if (reduction <= 0m)
                {
                    continue;
                }

                assign(line, currentValue - reduction);
                remaining = EstimateMath.RoundHours(remaining - reduction);
            }
        }
    }

    private static LaborRule? FindRule(LaborTemplate? template, string? locationProfile, string? installType)
    {
        if (template is null)
        {
            return null;
        }

        var normalizedLocation = string.IsNullOrWhiteSpace(locationProfile) ? string.Empty : locationProfile.Trim();
        var normalizedInstallType = string.IsNullOrWhiteSpace(installType) ? string.Empty : installType.Trim();

        return template.Rules.FirstOrDefault(candidate =>
            string.Equals(candidate.LocationProfile.Trim(), normalizedLocation, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.InstallType.Trim(), normalizedInstallType, StringComparison.OrdinalIgnoreCase));
    }

    private static LaborTemplate CreateStarterTemplate(string name) =>
        new()
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Standard Device Labor" : name,
            Notes = "Starter bid profile with numbering, labor rates, markup defaults, and the install time matrix.",
            BidNumberFormat = "BID-YY-NNNN",
            DefaultMarkupPercent = 22m,
            JourneymanRegularDirectRate = 65m,
            JourneymanRegularBilledRate = 90m,
            JourneymanOvernightDirectRate = 97.5m,
            JourneymanOvernightBilledRate = 135m,
            ApprenticeRegularDirectRate = 45m,
            ApprenticeRegularBilledRate = 65m,
            ApprenticeOvernightDirectRate = 67.5m,
            ApprenticeOvernightBilledRate = 97.5m,
            AdminDirectRate = 68m,
            AdminBilledRate = 95m,
            EngineeringDirectRate = 96m,
            EngineeringBilledRate = 130m,
            Rules =
            [
                new LaborRule
                {
                    LocationProfile = "Normal Area",
                    InstallType = "Normal",
                    InstallMinutes = 15m,
                    DemoMinutes = 0m,
                    TrimMinutes = 10m,
                    TestMinutes = 5m,
                    Notes = "Typical ceiling or wall mounted device in a standard environment."
                },
                new LaborRule
                {
                    LocationProfile = "Warehouse",
                    InstallType = "Lift",
                    InstallMinutes = 60m,
                    DemoMinutes = 10m,
                    TrimMinutes = 30m,
                    TestMinutes = 15m,
                    Notes = "Use when lift staging or elevated access is required."
                },
                new LaborRule
                {
                    LocationProfile = "Control Room",
                    InstallType = "Panel",
                    InstallMinutes = 45m,
                    DemoMinutes = 10m,
                    TrimMinutes = 25m,
                    TestMinutes = 12m,
                    Notes = "Panel work, terminations, and coordination time."
                },
                new LaborRule
                {
                    LocationProfile = "Finished Office",
                    InstallType = "Pipe",
                    InstallMinutes = 35m,
                    DemoMinutes = 5m,
                    TrimMinutes = 20m,
                    TestMinutes = 8m,
                    Notes = "Finished spaces with added coordination and cleanup."
                }
            ]
        };

    private sealed class WorkspaceStoreBootstrapper
    {
        public void RegenerateMonitoringSchedule(ServiceAgreement agreement)
        {
            agreement.ContractMonths = Math.Max(1, agreement.ContractMonths);
            agreement.InspectionIntervalMonths = Math.Max(1, agreement.InspectionIntervalMonths);
            agreement.MonitoringPayments =
            [
                ..Enumerable.Range(0, agreement.ContractMonths).Select(monthIndex =>
                    new MonitoringPayment
                    {
                        DueDate = new DateTime(agreement.ContractStart.Year, agreement.ContractStart.Month, 1).AddMonths(monthIndex),
                        Amount = agreement.MonthlyMonitoringAmount,
                        ReceivedOn = new DateTime(agreement.ContractStart.Year, agreement.ContractStart.Month, 1).AddMonths(monthIndex)
                    })
            ];
            agreement.NextInspectionDate = agreement.ContractStart.AddMonths(agreement.InspectionIntervalMonths);
        }
    }
}

public static class EstimateMath
{
    public static decimal RoundCurrency(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal RoundHours(decimal value) =>
        Math.Round(value * 4m, 0, MidpointRounding.AwayFromZero) / 4m;

    public static decimal GetBidInstallHours(BidRecord bid) =>
        RoundHours(bid.Components.Sum(component => component.InstallHours));

    public static decimal GetBidDemoHours(BidRecord bid) =>
        RoundHours(bid.DemoItems.Sum(item => item.TotalHours));

    public static decimal GetBidTrimHours(BidRecord bid) =>
        RoundHours(bid.Components.Sum(component => component.TrimHours));

    public static decimal GetBidTestHours(BidRecord bid) =>
        RoundHours(bid.Components.Sum(component => component.TestHours));

    public static decimal GetBidCalculatedFieldHours(BidRecord bid) =>
        RoundHours(GetBidInstallHours(bid) + GetBidDemoHours(bid) + GetBidTrimHours(bid) + GetBidTestHours(bid));

    public static decimal GetBidAllocatedFieldHours(BidRecord bid) =>
        RoundHours(bid.LaborDistribution.Sum(line => line.TotalHours));

    public static decimal GetBidAllocatedHours(BidRecord bid, PersonnelType personnelType, HourType hourType) =>
        RoundHours(bid.LaborDistribution
            .Where(line => line.PersonnelType == personnelType && line.HourType == hourType)
            .Sum(line => line.TotalHours));

    public static decimal GetBidAdminHours(BidRecord bid) =>
        RoundHours(bid.AdministrativeTasks.Where(task => task.PricingMode == TaskPricingMode.Hourly).Sum(task => task.EstimatedHours));

    public static decimal GetBidEngineeringHours(BidRecord bid) =>
        RoundHours(bid.EngineeringTasks.Where(task => task.PricingMode == TaskPricingMode.Hourly).Sum(task => task.EstimatedHours));

    public static decimal GetDefaultSaleFromMarkup(decimal cost, decimal markupPercent) =>
        RoundCurrency(cost * (1m + (Math.Max(0m, markupPercent) / 100m)));

    public static decimal GetWorkTaskCost(WorkTask task, decimal directRate) =>
        task.PricingMode == TaskPricingMode.Hourly
            ? RoundCurrency(RoundHours(task.EstimatedHours) * directRate)
            : RoundCurrency(task.CostPrice);

    public static decimal GetWorkTaskSale(WorkTask task, decimal directRate, decimal billedRate, decimal markupPercent)
    {
        if (task.PricingMode == TaskPricingMode.Hourly)
        {
            return RoundCurrency(RoundHours(task.EstimatedHours) * billedRate);
        }

        return RoundCurrency(task.SalePrice > 0m
            ? task.SalePrice
            : GetDefaultSaleFromMarkup(task.CostPrice, markupPercent));
    }

    public static decimal GetBidAdministrativeTaskCost(BidRecord bid) =>
        RoundCurrency(bid.AdministrativeTasks.Sum(task => GetWorkTaskCost(task, bid.AdminDirectRate)));

    public static decimal GetBidAdministrativeTaskSale(BidRecord bid) =>
        RoundCurrency(bid.AdministrativeTasks.Sum(task => GetWorkTaskSale(task, bid.AdminDirectRate, bid.AdminBilledRate, bid.MarkupPercent)));

    public static decimal GetBidEngineeringTaskCost(BidRecord bid) =>
        RoundCurrency(bid.EngineeringTasks.Sum(task => GetWorkTaskCost(task, bid.EngineeringDirectRate)));

    public static decimal GetBidEngineeringTaskSale(BidRecord bid) =>
        RoundCurrency(bid.EngineeringTasks.Sum(task => GetWorkTaskSale(task, bid.EngineeringDirectRate, bid.EngineeringBilledRate, bid.MarkupPercent)));

    public static decimal GetBidFieldLaborCost(BidRecord bid) =>
        RoundCurrency(bid.LaborDistribution.Sum(line => GetDistributionCost(bid, line)));

    public static decimal GetBidFieldLaborSale(BidRecord bid) =>
        RoundCurrency(bid.LaborDistribution.Sum(line => GetDistributionSale(bid, line)));

    public static decimal GetBidComponentCost(BidRecord bid) =>
        RoundCurrency(bid.Components.Sum(component => component.TotalMaterialCost));

    public static decimal GetBidComponentSale(BidRecord bid) =>
        RoundCurrency(bid.Components.Sum(component => component.UnitSale > 0m ? component.TotalMaterialSale : GetDefaultSaleFromMarkup(component.TotalMaterialCost, bid.MarkupPercent)));

    public static decimal GetBidWireCost(BidRecord bid) =>
        RoundCurrency(bid.Materials.Where(item => item.Kind == BidMaterialKind.Wire).Sum(item => item.ExtendedCost));

    public static decimal GetBidWireSale(BidRecord bid) =>
        RoundCurrency(bid.Materials.Where(item => item.Kind == BidMaterialKind.Wire).Sum(item => item.UnitSale > 0m ? item.ExtendedSale : GetDefaultSaleFromMarkup(item.ExtendedCost, bid.MarkupPercent)));

    public static decimal GetBidMaterialOnlyCost(BidRecord bid) =>
        RoundCurrency(bid.Materials.Where(item => item.Kind == BidMaterialKind.Material).Sum(item => item.ExtendedCost));

    public static decimal GetBidMaterialOnlySale(BidRecord bid) =>
        RoundCurrency(bid.Materials.Where(item => item.Kind == BidMaterialKind.Material).Sum(item => item.UnitSale > 0m ? item.ExtendedSale : GetDefaultSaleFromMarkup(item.ExtendedCost, bid.MarkupPercent)));

    public static decimal GetBidLaborCost(BidRecord bid) =>
        RoundCurrency(GetBidFieldLaborCost(bid) + GetBidAdministrativeTaskCost(bid) + GetBidEngineeringTaskCost(bid));

    public static decimal GetBidLaborSale(BidRecord bid) =>
        RoundCurrency(GetBidFieldLaborSale(bid) + GetBidAdministrativeTaskSale(bid) + GetBidEngineeringTaskSale(bid));

    public static decimal GetBidMaterialCost(BidRecord bid) =>
        RoundCurrency(GetBidComponentCost(bid) + GetBidWireCost(bid) + GetBidMaterialOnlyCost(bid));

    public static decimal GetBidMaterialSale(BidRecord bid) =>
        RoundCurrency(GetBidComponentSale(bid) + GetBidWireSale(bid) + GetBidMaterialOnlySale(bid));

    public static decimal GetBidEstimatedCost(BidRecord bid) =>
        RoundCurrency(GetBidLaborCost(bid) + GetBidMaterialCost(bid));

    public static decimal GetBidCalculatedSale(BidRecord bid) =>
        RoundCurrency(GetBidLaborSale(bid) + GetBidMaterialSale(bid));

    public static decimal GetBidAdjustedRevenue(BidRecord bid) =>
        RoundCurrency(bid.AcceptedSalePrice > 0m ? bid.AcceptedSalePrice : GetBidCalculatedSale(bid));

    public static decimal GetBidCalculatedProfit(BidRecord bid) =>
        RoundCurrency(GetBidCalculatedSale(bid) - GetBidEstimatedCost(bid));

    public static decimal GetBidCalculatedMarginPercent(BidRecord bid)
    {
        var sale = GetBidCalculatedSale(bid);
        return sale <= 0m ? 0m : GetBidCalculatedProfit(bid) / sale;
    }

    public static decimal GetBidAdjustedProfit(BidRecord bid) =>
        RoundCurrency(GetBidAdjustedRevenue(bid) - GetBidEstimatedCost(bid));

    public static decimal GetBidAdjustedMarginPercent(BidRecord bid)
    {
        var sale = GetBidAdjustedRevenue(bid);
        return sale <= 0m ? 0m : GetBidAdjustedProfit(bid) / sale;
    }

    public static decimal GetBidRevenue(BidRecord bid) =>
        GetBidAdjustedRevenue(bid);

    public static decimal GetBidMargin(BidRecord bid) =>
        GetBidAdjustedProfit(bid);

    public static decimal GetBidMarginPercent(BidRecord bid) =>
        GetBidAdjustedMarginPercent(bid);

    public static decimal GetJobActualLaborHours(JobRecord job) =>
        job.TimeEntries.Sum(entry => entry.Hours);

    public static decimal GetJobActualLaborCost(JobRecord job) =>
        RoundCurrency(job.TimeEntries.Sum(entry => entry.TotalCost));

    public static decimal GetJobActualMaterialCost(JobRecord job) =>
        RoundCurrency(
            JobFinancialMath.GetTrackedBidDeviceActualCost(job) +
            JobFinancialMath.GetTrackedJobDeviceActualCost(job) +
            job.MaterialPurchases.Sum(JobFinancialMath.GetMaterialPurchaseTotal));

    public static decimal GetJobEstimatedCost(JobRecord job) =>
        RoundCurrency(job.Baseline.EstimatedTotalCost + GetJobApprovedChangeOrderCost(job));

    public static decimal GetJobApprovedChangeOrderRevenue(JobRecord job) =>
        RoundCurrency(job.ChangeOrders.Where(changeOrder => changeOrder.Approved).Sum(changeOrder => changeOrder.RevenueAmount));

    public static decimal GetJobApprovedChangeOrderCost(JobRecord job) =>
        RoundCurrency(job.ChangeOrders.Where(changeOrder => changeOrder.Approved).Sum(changeOrder => changeOrder.EstimatedCostImpact));

    public static decimal GetJobRevenue(JobRecord job) =>
        RoundCurrency(job.Baseline.OriginalRevenue + GetJobApprovedChangeOrderRevenue(job));

    public static decimal GetJobBilledCommitments(JobRecord job) =>
        RoundCurrency(job.Commitments.Sum(commitment => commitment.BilledAmount));

    public static decimal GetJobCommittedExposure(JobRecord job) =>
        RoundCurrency(
            GetJobActualLaborCost(job) +
            GetJobActualMaterialCost(job) +
            GetJobApprovedChangeOrderCost(job) +
            job.Commitments.Sum(commitment => Math.Max(commitment.CommittedAmount, commitment.BilledAmount)));

    public static decimal GetJobActualCost(JobRecord job) =>
        RoundCurrency(
            GetJobActualLaborCost(job) +
            GetJobActualMaterialCost(job) +
            GetJobApprovedChangeOrderCost(job) +
            GetJobBilledCommitments(job));

    public static decimal GetJobProfit(JobRecord job) =>
        RoundCurrency(GetJobRevenue(job) - GetJobActualCost(job));

    public static decimal GetJobMarginPercent(JobRecord job)
    {
        var revenue = GetJobRevenue(job);
        return revenue <= 0m ? 0m : GetJobProfit(job) / revenue;
    }

    public static decimal GetJobBilledRevenue(JobRecord job) =>
        RoundCurrency(job.ScheduleOfValues.Sum(item => item.BilledToDate));

    public static decimal GetJobCollectedRevenue(JobRecord job) =>
        RoundCurrency(job.ScheduleOfValues.Sum(item => item.PaidToDate));

    public static decimal GetServiceContractValue(ServiceAgreement agreement) =>
        agreement.MonthlyMonitoringAmount * agreement.ContractMonths;

    public static decimal GetServicePaid(ServiceAgreement agreement) =>
        agreement.MonitoringPayments.Where(payment => payment.IsPaid).Sum(payment => payment.Amount);

    public static decimal GetServiceOutstanding(ServiceAgreement agreement) =>
        GetServiceContractValue(agreement) - GetServicePaid(agreement);

    public static DateTime GetNextBillingDate(ServiceAgreement agreement)
    {
        var nextPayment = agreement.MonitoringPayments
            .Where(payment => !payment.IsPaid)
            .OrderBy(payment => payment.DueDate)
            .FirstOrDefault();

        return nextPayment?.DueDate ?? agreement.ContractStart.AddMonths(agreement.ContractMonths);
    }

    public static decimal GetServiceQuoteRevenue(ServiceQuoteRecord quote) =>
        RoundCurrency(quote.Items.Sum(item => item.TotalPrice));

    public static decimal GetServiceQuoteCost(ServiceQuoteRecord quote) =>
        RoundCurrency(quote.Items.Sum(item => item.TotalCost));

    public static string GetCurrency(decimal value) =>
        RoundCurrency(value).ToString("C2", CultureInfo.CurrentCulture);

    public static string GetHours(decimal value) =>
        RoundHours(value).ToString("N2", CultureInfo.CurrentCulture);

    public static string GetPercent(decimal value) =>
        value.ToString("P1", CultureInfo.CurrentCulture);

    private static decimal GetDistributionCost(BidRecord bid, BidLaborDistributionLine line)
    {
        var directRate = ResolveFieldRate(bid, line.PersonnelType, line.HourType, useBilledRate: false);
        return RoundCurrency(RoundHours(line.TotalHours) * directRate);
    }

    private static decimal GetDistributionSale(BidRecord bid, BidLaborDistributionLine line)
    {
        var billedRate = ResolveFieldRate(bid, line.PersonnelType, line.HourType, useBilledRate: true);
        return RoundCurrency(RoundHours(line.TotalHours) * billedRate);
    }

    private static decimal ResolveFieldRate(BidRecord bid, PersonnelType personnelType, HourType hourType, bool useBilledRate) =>
        (personnelType, hourType, useBilledRate) switch
        {
            (PersonnelType.Journeyman, HourType.Regular, false) => bid.JourneymanRegularDirectRate,
            (PersonnelType.Journeyman, HourType.Regular, true) => bid.JourneymanRegularBilledRate,
            (PersonnelType.Journeyman, HourType.Overnight, false) => bid.JourneymanOvernightDirectRate,
            (PersonnelType.Journeyman, HourType.Overnight, true) => bid.JourneymanOvernightBilledRate,
            (PersonnelType.Apprentice, HourType.Regular, false) => bid.ApprenticeRegularDirectRate,
            (PersonnelType.Apprentice, HourType.Regular, true) => bid.ApprenticeRegularBilledRate,
            (PersonnelType.Apprentice, HourType.Overnight, false) => bid.ApprenticeOvernightDirectRate,
            (PersonnelType.Apprentice, HourType.Overnight, true) => bid.ApprenticeOvernightBilledRate,
            _ => 0m
        };
}
